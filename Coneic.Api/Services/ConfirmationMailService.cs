using Coneic.Api.Data;
using Coneic.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Services;

/// <summary>
/// Envía, en segundo plano y con ritmo controlado, el mail de "Tus actividades
/// académicas elegidas" a cada persona que ya tiene confirmadas todas las
/// actividades que le corresponden (Taller + Simultánea + Solidaria; solo
/// Solidaria para el Desafío de Barreras).
///
/// Antes el mail salía dentro del pedido de confirmar y, cuando el servicio de
/// correo respondía 429 (límite de envíos por minuto), el error se tragaba y la
/// persona nunca recibía nada (la noche del 7/10 fallaron ~1.450 envíos). Ahora
/// se lleva un registro propio (tabla SentConfirmationEmails): lo que no sale
/// se reintenta en el próximo ciclo, y cubre también a quienes reciben su
/// asignación automática. Pausa de emergencia: AppSettings
/// "ConfirmationMailsPaused" = "1".
/// </summary>
public class ConfirmationMailService : BackgroundService
{
    private const int PerCycle = 12;                       // ~12 mails por minuto
    private static readonly TimeSpan CycleEvery = TimeSpan.FromSeconds(60);
    private const int MaxFailuresPerEmail = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmailService _email;
    private readonly ILogger<ConfirmationMailService> _logger;
    private readonly Dictionary<string, int> _failures = new();

    public ConfirmationMailService(IServiceScopeFactory scopeFactory, IEmailService email, ILogger<ConfirmationMailService> logger)
    {
        _scopeFactory = scopeFactory;
        _email = email;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunCycleAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Ciclo de mails de confirmación falló"); }
            try { await Task.Delay(CycleEvery, stoppingToken); } catch (OperationCanceledException) { }
        }
    }

    private sealed record Pending(string Email, string Name, List<MailActivityRow> Rows, bool Barreras);

    private async Task RunCycleAsync(CancellationToken ct)
    {
        List<Pending> pending;

        // Lectura dentro de la cola de escrituras: no compite con elegir/confirmar.
        await HotCache.WriteGate.WaitAsync(ct);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS SentConfirmationEmails (Email TEXT PRIMARY KEY, SentAt TEXT NOT NULL)", ct);

            var paused = await db.AppSettings.AsNoTracking().AnyAsync(s => s.Key == "ConfirmationMailsPaused" && s.Value == "1", ct);
            if (paused) return;

            pending = await BuildPendingAsync(db, ct);
        }
        finally { HotCache.WriteGate.Release(); }

        foreach (var p in pending.Take(PerCycle))
        {
            if (ct.IsCancellationRequested) break;
            if (_failures.GetValueOrDefault(p.Email) >= MaxFailuresPerEmail) continue;

            var ok = await _email.TrySendAcademicActivitiesDetailedAsync(p.Email, p.Name, p.Rows, p.Barreras);
            if (!ok)
            {
                _failures[p.Email] = _failures.GetValueOrDefault(p.Email) + 1;
                continue;
            }

            await HotCache.WriteGate.WaitAsync(ct);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT OR IGNORE INTO SentConfirmationEmails (Email, SentAt) VALUES ({p.Email}, {DateTime.UtcNow:O})", ct);
            }
            finally { HotCache.WriteGate.Release(); }

            await Task.Delay(TimeSpan.FromMilliseconds(1500), ct);
        }

        if (pending.Count > 0)
            _logger.LogInformation("Mails de confirmación: {Pending} pendientes al iniciar el ciclo", pending.Count);
    }

    private static async Task<List<Pending>> BuildPendingAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var blockIds = new[] { 2, 3, 4 };
        var sent = (await db.Database.SqlQueryRaw<string>("SELECT Email AS Value FROM SentConfirmationEmails").ToListAsync(ct))
            .Select(e => e.ToLowerInvariant()).ToHashSet();

        var acts = await db.SelectableActivities.AsNoTracking().Where(a => blockIds.Contains(a.BlockId)).ToDictionaryAsync(a => a.Id, ct);
        var sels = await db.ActivitySelections.AsNoTracking().Where(s => blockIds.Contains(s.BlockId)).ToListAsync(ct);
        var regs = (await db.Registrations.AsNoTracking().ToListAsync(ct))
            .GroupBy(r => r.Email.ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First());

        var result = new List<(DateTime Done, Pending P)>();
        foreach (var g in sels.GroupBy(s => s.UserEmail.ToLowerInvariant()))
        {
            if (sent.Contains(g.Key) || !regs.TryGetValue(g.Key, out var reg)) continue;

            var barreras = reg.InterestedInMaccaferri;
            var need = barreras ? new[] { 4 } : new[] { 2, 3, 4 };
            var byBlock = g.ToDictionary(s => s.BlockId);
            if (!need.All(b => byBlock.TryGetValue(b, out var s) && s.IsConfirmed)) continue;

            var rows = new List<MailActivityRow>();
            if (barreras)
            {
                rows.Add(new MailActivityRow("Taller y Charla Simultánea", "Desafío de Barreras", "Maccaferri", null,
                    "Sede: UTN BA - Campus · Hora de inicio: 09:00 hs"));
            }
            else
            {
                rows.Add(Row("Taller", acts[byBlock[2].ActivityId]));
                rows.Add(Row("Charla Simultánea", acts[byBlock[3].ActivityId]));
            }
            rows.Add(Row("Actividad de Compromiso Social y Medio Ambiente", acts[byBlock[4].ActivityId]));

            var done = need.Max(b => byBlock[b].ConfirmedAt ?? DateTime.MinValue);
            result.Add((done, new Pending(reg.Email, $"{reg.Name} {reg.Lastname}".Trim(), rows, barreras)));
        }
        return result.OrderBy(r => r.Done).Select(r => r.P).ToList();
    }

    private static MailActivityRow Row(string label, SelectableActivity a)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(a.Venue)) parts.Add($"Sede: {a.Venue}");
        parts.Add(string.IsNullOrWhiteSpace(a.StartTime) ? "Hora de inicio: a confirmar" : $"Hora de inicio: {a.StartTime} hs");
        if (!string.IsNullOrWhiteSpace(a.MeetingPoint)) parts.Add($"Punto de encuentro: {a.MeetingPoint}");
        return new MailActivityRow(label, a.Code, a.Title, a.Speaker, string.Join(" · ", parts));
    }
}
