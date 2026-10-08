using Coneic.Api.Controllers;
using Coneic.Api.Data;
using Coneic.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Services;

/// <summary>
/// Al cerrar la elección (jueves 08/10 23:00 ART) asigna automáticamente, una
/// sola vez, lo que quedó sin elegir (ver AutoAssignRunner). La corrida queda
/// registrada en AppSettings ("AutoAssignDone_20261008") para que no se repita
/// aunque la app se reinicie. Los mails de confirmación de quienes reciben
/// asignación salen después por ConfirmationMailService.
/// </summary>
public class AutoAssignScheduler : BackgroundService
{
    private const string DoneKey = "AutoAssignDone_20261008";
    private static readonly TimeSpan GraceAfterClose = TimeSpan.FromSeconds(45);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoAssignScheduler> _logger;

    public AutoAssignScheduler(IServiceScopeFactory scopeFactory, ILogger<AutoAssignScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (DateTime.UtcNow >= ActivitySelectionController.SelectionWindowClosesAt + GraceAfterClose)
                {
                    if (await TryRunAsync(stoppingToken)) return;
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Asignación automática falló, se reintenta"); }

            try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); } catch (OperationCanceledException) { }
        }
    }

    private async Task<bool> TryRunAsync(CancellationToken ct)
    {
        await HotCache.WriteGate.WaitAsync(ct);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (await db.AppSettings.AnyAsync(s => s.Key == DoneKey, ct)) return true;

            var result = await AutoAssignRunner.RunAsync(db, dryRun: false);
            db.AppSettings.Add(new AppSetting
            {
                Key = DoneKey,
                Value = $"{DateTime.UtcNow:O} talleres={result.Talleres} simultaneas={result.Simultaneas} solidarias={result.Solidarias} borradores={result.ConfirmedDrafts} otraFamilia={result.MixedFamily} sinLugar={result.Failures.Count}",
            });
            await db.SaveChangesAsync(ct);
            ActivitySelectionController.InvalidateAll();

            _logger.LogWarning(
                "[AUTO-ASSIGN 23:00] talleres={T} simultaneas={S} solidarias={So} borradores={D} otraFamilia={M} sinLugar={F}",
                result.Talleres, result.Simultaneas, result.Solidarias, result.ConfirmedDrafts, result.MixedFamily, result.Failures.Count);
            foreach (var f in result.Failures) _logger.LogWarning("[AUTO-ASSIGN] sin lugar: {F}", f);
            return true;
        }
        finally { HotCache.WriteGate.Release(); }
    }
}
