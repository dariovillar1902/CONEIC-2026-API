using Coneic.Api.Data;
using Coneic.Api.Models;
using Coneic.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivitySelectionController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<ActivitySelectionController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    // Única cuenta habilitada para reasignar manualmente la visita de otra
    // persona (excepción del directorio, fuera del flujo normal). No se
    // deriva del rol 'admin' genérico a propósito: es un caso puntual, no un
    // permiso de todos los admins.
    private const string DirectorioOverrideEmail = "directorio@coneic2026.com.ar";

    // Recorte temporal ("por ahora") mientras se prueba la feature con el
    // equipo: solo estas cuentas admin reciben el mail de confirmación, y a
    // una casilla personal real (son cuentas institucionales compartidas).
    // Sacar este mapeo cuando se habilite para todo el mundo.
    private static readonly Dictionary<string, (string Name, string Email)[]> PilotRecipients = new()
    {
        ["web@coneic2026.com.ar"] = new[] { ("Darío", "dario_villar2001@hotmail.com") },
        ["prensa@coneic2026.com.ar"] = new[] { ("Carol", "carollombardino97@gmail.com") },
        ["directorio@coneic2026.com.ar"] = new[]
        {
            ("Sofi", "spizzamus@frba.utn.edu.ar"),
            ("Cande", "candepoggi@frba.utn.edu.ar"),
        },
        ["comision-directiva@coneic2026.com.ar"] = new[] { ("Darío", "dvillar@frba.utn.edu.ar") },
    };

    // Actualizado 2026-09-09: el PDF viejo en blob storage seguía teniendo
    // AESA (visita que se sacó del listado). Ahora apunta directo al Drive
    // con la propuesta definitiva vigente.
    private const string EppPdfUrl =
        "https://drive.google.com/file/d/1YXycLwBieByabfsSTqBqJVfU3ifJ13-F/view?usp=drive_link";

    public ActivitySelectionController(
        ApplicationDbContext db, IEmailService email, ILogger<ActivitySelectionController> logger, IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _email = email;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    // ── Listado de bloques + opciones + cupos + tu elección actual (draft o confirmada) ──

    [HttpGet("blocks")]
    public async Task<IActionResult> GetBlocks([FromQuery] string email, [FromQuery] bool includeInactive = false)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Falta el email." });

        var mySelections = await _db.ActivitySelections
            .Where(s => s.UserEmail.ToLower() == email.ToLower())
            .ToListAsync();

        // includeInactive=true es para el catálogo del panel de admin (para
        // poder asignar/reasignar Visita Técnica, que quedó IsActive=false
        // una vez cerrada su ventana de elección, pero sigue siendo una
        // actividad real a la que hay que poder anotar gente a mano). El
        // flujo normal del asistente (sin este parámetro) sigue viendo solo
        // los bloques activos, sin cambios.
        var blocksQuery = _db.ActivityBlocks.AsQueryable();
        if (!includeInactive) blocksQuery = blocksQuery.Where(b => b.IsActive);
        var blocks = await blocksQuery.OrderBy(b => b.Id).ToListAsync();
        var activities = await _db.SelectableActivities.OrderBy(a => a.Code).ToListAsync();

        var result = blocks.Select(b => new
        {
            b.Id,
            b.Category,
            b.Name,
            b.Note,
            b.MaxSelections,
            YourSelectionActivityId = mySelections.FirstOrDefault(s => s.BlockId == b.Id)?.ActivityId,
            Options = activities.Where(a => a.BlockId == b.Id).Select(a => new
            {
                a.Id,
                a.Code,
                a.Title,
                a.Speaker,
                a.Description,
                a.ImageUrl,
                a.Capacity,
                Taken = a.TakenCount,
            }),
        });

        return Ok(result);
    }

    // ── Tu estado general: ¿ya confirmaste definitivamente? ──────────────────

    [HttpGet("status")]
    public async Task<IActionResult> Status([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Falta el email." });

        var mine = await (
            from s in _db.ActivitySelections
            join a in _db.SelectableActivities on s.ActivityId equals a.Id
            where s.UserEmail.ToLower() == email.ToLower()
            select new
            {
                s.BlockId,
                s.IsConfirmed,
                s.ConfirmedAt,
                ActivityId = a.Id,
                ActivityCode = a.Code,
                ActivityTitle = a.Title,
            }).ToListAsync();

        return Ok(new
        {
            isConfirmed = mine.Any(m => m.IsConfirmed),
            confirmedAt = mine.Where(m => m.ConfirmedAt.HasValue).Select(m => m.ConfirmedAt).FirstOrDefault(),
            selections = mine,
        });
    }

    // ── Elegir / cambiar de opción dentro de un bloque (draft, reversible) ──────

    public record SelectRequest(string Email, int ActivityId);

    [HttpPost("select")]
    public async Task<IActionResult> Select([FromBody] SelectRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { message = "Falta el email." });

        var activity = await _db.SelectableActivities.FindAsync(req.ActivityId);
        if (activity == null)
            return NotFound(new { message = "La actividad indicada no existe." });

        var alreadyConfirmed = await _db.ActivitySelections
            .AnyAsync(s => s.UserEmail.ToLower() == req.Email.ToLower() && s.IsConfirmed);
        if (alreadyConfirmed)
            return BadRequest(new { message = "Ya confirmaste tu selección definitiva — no se puede modificar." });

        var existing = await _db.ActivitySelections
            .FirstOrDefaultAsync(s => s.UserEmail.ToLower() == req.Email.ToLower() && s.BlockId == activity.BlockId);

        if (existing != null && existing.ActivityId == activity.Id)
            return Ok(new { message = "Ya tenías esta actividad seleccionada.", activityId = activity.Id, blockId = activity.BlockId });

        using var tx = await _db.Database.BeginTransactionAsync();

        // Reserva atómica: un único UPDATE que solo avanza el contador si
        // todavía hay cupo. Bajo carga concurrente, SQLite serializa estas
        // transacciones — nunca dos personas "ganan" el mismo último cupo.
        var reserved = await _db.SelectableActivities
            .Where(a => a.Id == activity.Id && a.TakenCount < a.Capacity)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.TakenCount, a => a.TakenCount + 1));

        if (reserved == 0)
        {
            await tx.RollbackAsync();
            return Conflict(new { message = "Se acaba de completar el cupo de esta actividad. Elegí otra opción." });
        }

        if (existing != null)
        {
            await _db.SelectableActivities
                .Where(a => a.Id == existing.ActivityId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.TakenCount, a => a.TakenCount - 1));
            _db.ActivitySelections.Remove(existing);
            await _db.SaveChangesAsync();
        }

        _db.ActivitySelections.Add(new ActivitySelection
        {
            UserEmail = req.Email,
            BlockId = activity.BlockId,
            ActivityId = activity.Id,
        });
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        return Ok(new { message = "Selección guardada.", activityId = activity.Id, blockId = activity.BlockId });
    }

    // ── Quitar tu elección (draft) en un bloque ──────────────────────────────

    [HttpDelete("select")]
    public async Task<IActionResult> Unselect([FromQuery] string email, [FromQuery] int blockId)
    {
        var existing = await _db.ActivitySelections
            .FirstOrDefaultAsync(s => s.UserEmail.ToLower() == email.ToLower() && s.BlockId == blockId);

        if (existing == null) return NoContent();
        if (existing.IsConfirmed)
            return BadRequest(new { message = "Ya confirmaste tu selección definitiva — no se puede modificar." });

        using var tx = await _db.Database.BeginTransactionAsync();
        await _db.SelectableActivities
            .Where(a => a.Id == existing.ActivityId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.TakenCount, a => a.TakenCount - 1));
        _db.ActivitySelections.Remove(existing);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return NoContent();
    }

    // ── Listado completo de selecciones (admin, sin restricción) ────────────
    //
    // Filtros opcionales: activityCode (código de la actividad, ej "4.01"),
    // faculty (facultad/delegación exacta), sortBy (date_desc | date_asc,
    // default date_desc = más reciente primero).
    [HttpGet("all")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? activityCode,
        [FromQuery] string? faculty,
        [FromQuery] string? sortBy)
    {
        var query = BuildSelectionQuery(_db, activityCode, faculty);
        var results = await ApplySort(query, sortBy).ToListAsync();
        return Ok(results);
    }

    // ── Listado de selecciones restringido a las facultades del delegado ────
    //
    // Mismos filtros que /all, pero solo devuelve gente de las facultades que
    // el delegado gestiona (ManagedFaculties, o DelegationName si está vacío
    // — mismo patrón que RegistrationsController.GetByDelegate).
    [HttpGet("delegate")]
    public async Task<IActionResult> GetByDelegate(
        [FromQuery] string email,
        [FromQuery] string? activityCode,
        [FromQuery] string? faculty,
        [FromQuery] string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Falta el email." });

        var delegateUser = _db.Users.AsEnumerable()
            .FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (delegateUser == null)
            return NotFound(new { message = "Delegado no encontrado." });

        var managedFaculties = delegateUser.ManagedFaculties.Count > 0
            ? delegateUser.ManagedFaculties
            : (delegateUser.DelegationName != null ? new List<string> { delegateUser.DelegationName } : new List<string>());

        if (managedFaculties.Count == 0)
            return Ok(Array.Empty<object>());

        var query = BuildSelectionQuery(_db, activityCode, faculty)
            .Where(x => managedFaculties.Contains(x.Faculty ?? "", StringComparer.OrdinalIgnoreCase));

        var results = await ApplySort(query, sortBy).ToListAsync();
        return Ok(results);
    }

    private static IQueryable<SelectionListItem> BuildSelectionQuery(
        ApplicationDbContext db, string? activityCode, string? faculty)
    {
        var query =
            from s in db.ActivitySelections
            join a in db.SelectableActivities on s.ActivityId equals a.Id
            join r in db.Registrations on s.UserEmail.ToLower() equals r.Email.ToLower() into regJoin
            from r in regJoin.DefaultIfEmpty()
            select new SelectionListItem
            {
                RegistrationId = r != null ? r.Id : (int?)null,
                Name = r != null ? r.Name : null,
                Lastname = r != null ? r.Lastname : null,
                Email = s.UserEmail,
                Faculty = r != null ? r.Faculty : null,
                BlockId = s.BlockId,
                ActivityId = a.Id,
                ActivityCode = a.Code,
                ActivityTitle = a.Title,
                IsConfirmed = s.IsConfirmed,
                SelectedAt = s.SelectedAt,
                ConfirmedAt = s.ConfirmedAt,
            };

        if (!string.IsNullOrWhiteSpace(activityCode))
            query = query.Where(x => x.ActivityCode == activityCode);

        if (!string.IsNullOrWhiteSpace(faculty))
            query = query.Where(x => x.Faculty != null && x.Faculty.ToLower() == faculty.ToLower());

        return query;
    }

    private static IQueryable<SelectionListItem> ApplySort(IQueryable<SelectionListItem> query, string? sortBy) =>
        sortBy switch
        {
            "date_asc" => query.OrderBy(x => x.SelectedAt),
            _ => query.OrderByDescending(x => x.SelectedAt), // default: más reciente primero
        };

    private class SelectionListItem
    {
        public int? RegistrationId { get; set; }
        public string? Name { get; set; }
        public string? Lastname { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Faculty { get; set; }
        public int BlockId { get; set; }
        public int ActivityId { get; set; }
        public string ActivityCode { get; set; } = string.Empty;
        public string ActivityTitle { get; set; } = string.Empty;
        public bool IsConfirmed { get; set; }
        public DateTime SelectedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }

    // ── Confirmación definitiva (irreversible) ───────────────────────────────

    public record ConfirmRequest(string Email);

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { message = "Falta el email." });

        var mySelections = await _db.ActivitySelections
            .Where(s => s.UserEmail.ToLower() == req.Email.ToLower())
            .ToListAsync();

        if (mySelections.Any(s => s.IsConfirmed))
            return BadRequest(new { message = "Ya habías confirmado tu selección definitiva." });

        var allBlockIds = await _db.ActivityBlocks.Where(b => b.IsActive).Select(b => b.Id).ToListAsync();
        var missing = allBlockIds.Except(mySelections.Select(s => s.BlockId)).ToList();
        if (missing.Count > 0)
            return BadRequest(new { message = "Todavía te falta elegir una actividad en algún bloque.", missingBlockIds = missing });

        var now = DateTime.Now;
        foreach (var s in mySelections)
        {
            s.IsConfirmed = true;
            s.ConfirmedAt = now;
        }
        await _db.SaveChangesAsync();

        await SendPilotConfirmationEmailAsync(req.Email, mySelections);

        return Ok(new { message = "Selección confirmada.", confirmedAt = now });
    }

    // Envía el mail de "visita técnica elegida" solo si la cuenta que confirmó
    // está en la lista piloto (ver PilotRecipients). No falla la confirmación
    // si el envío tiene algún problema — la selección ya quedó guardada.
    private async Task SendPilotConfirmationEmailAsync(string userEmail, List<ActivitySelection> selections)
    {
        if (!PilotRecipients.TryGetValue(userEmail.ToLower(), out var recipients)) return;

        var visita = await (
            from s in _db.ActivitySelections.Where(x => selections.Select(sel => sel.Id).Contains(x.Id))
            join a in _db.SelectableActivities on s.ActivityId equals a.Id
            where a.BlockId == 1 // bloque "Visita Técnica"
            select new { a.Code, a.Title }
        ).FirstOrDefaultAsync();

        if (visita == null) return;

        foreach (var (name, email) in recipients)
        {
            try
            {
                await _email.SendActivitySelectionConfirmedAsync(email, name, visita.Code, visita.Title, EppPdfUrl);
            }
            catch
            {
                // no interrumpir la confirmación por un fallo de envío puntual
            }
        }
    }

    // ── Reasignación manual (excepción del directorio) ───────────────────────
    //
    // Mueve a `TargetEmail` a otra actividad del mismo bloque que la que ya
    // tenía elegida (o le crea una si no tenía). A diferencia de /select:
    //   - No le saca el cupo a nadie: si la actividad destino ya está llena,
    //     se le suma 1 a su Capacity en vez de rechazar el cambio.
    //   - No dispara el mail de confirmación — es una excepción administrativa,
    //     no una elección nueva del asistente.
    //   - Solo la cuenta de directorio puede usarlo (ver DirectorioOverrideEmail).
    public record AdminOverrideRequest(string AdminEmail, string TargetEmail, int ActivityId);

    [HttpPost("admin-override")]
    public async Task<IActionResult> AdminOverride([FromBody] AdminOverrideRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.AdminEmail) ||
            !req.AdminEmail.Equals(DirectorioOverrideEmail, StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { message = "Esta acción está restringida a la cuenta de Directorio." });
        }

        if (string.IsNullOrWhiteSpace(req.TargetEmail))
            return BadRequest(new { message = "Falta el email de la persona a reasignar." });

        var newActivity = await _db.SelectableActivities.FindAsync(req.ActivityId);
        if (newActivity == null)
            return NotFound(new { message = "La actividad indicada no existe." });

        using var tx = await _db.Database.BeginTransactionAsync();

        var existing = await _db.ActivitySelections
            .FirstOrDefaultAsync(s => s.UserEmail.ToLower() == req.TargetEmail.ToLower() && s.BlockId == newActivity.BlockId);

        if (existing != null && existing.ActivityId == newActivity.Id)
        {
            await tx.RollbackAsync();
            return Ok(new { message = "Ya estaba anotado/a en esta actividad.", activityId = newActivity.Id });
        }

        // Si hay cupo libre, se reserva como siempre. Si no, se agranda la
        // capacidad en 1 para hacerle lugar sin desplazar a nadie más.
        var hasRoom = await _db.SelectableActivities.AnyAsync(a => a.Id == newActivity.Id && a.TakenCount < a.Capacity);
        if (hasRoom)
        {
            await _db.SelectableActivities.Where(a => a.Id == newActivity.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.TakenCount, a => a.TakenCount + 1));
        }
        else
        {
            await _db.SelectableActivities.Where(a => a.Id == newActivity.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Capacity, a => a.Capacity + 1)
                    .SetProperty(a => a.TakenCount, a => a.TakenCount + 1));
        }

        if (existing != null)
        {
            await _db.SelectableActivities.Where(a => a.Id == existing.ActivityId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.TakenCount, a => a.TakenCount - 1));

            existing.ActivityId = newActivity.Id;
            existing.SelectedAt = DateTime.Now;
            // Una reasignación de directorio es definitiva por definición —
            // si la selección original había quedado en borrador (la persona
            // nunca llegó a confirmar), no debe seguir en borrador después
            // de esto.
            existing.IsConfirmed = true;
            existing.ConfirmedAt ??= DateTime.Now;
            await _db.SaveChangesAsync();
        }
        else
        {
            _db.ActivitySelections.Add(new ActivitySelection
            {
                UserEmail = req.TargetEmail,
                BlockId = newActivity.BlockId,
                ActivityId = newActivity.Id,
                IsConfirmed = true,
                ConfirmedAt = DateTime.Now,
            });
            await _db.SaveChangesAsync();
        }

        await tx.CommitAsync();

        // Sin mail — acción administrativa excepcional, no una elección del
        // asistente. Queda igual registrada en los logs del servidor.
        _logger.LogWarning(
            "[ADMIN OVERRIDE] {Admin} reasignó a {Target} a {Code} - {Title} (bloque {BlockId})",
            req.AdminEmail, req.TargetEmail, newActivity.Code, newActivity.Title, newActivity.BlockId);

        return Ok(new { message = "Reasignación realizada.", activityId = newActivity.Id, activityCode = newActivity.Code });
    }

    // ── Reenvío masivo del mail de confirmación (backfill puntual) ───────────
    //
    // El mail de "visita técnica confirmada" quedó restringido a
    // PilotRecipients desde que se armó la feature — ningún asistente real lo
    // recibió nunca. Este endpoint lo manda una vez a todo el mundo que ya
    // tiene su visita técnica (bloque 1) confirmada, sin tocar esa restricción
    // (que sigue rigiendo para confirmaciones futuras, hasta que se saque
    // explícitamente). Solo directorio puede dispararlo.
    public record ResendConfirmationsRequest(string AdminEmail, List<string>? ExcludeEmails);

    [HttpPost("resend-confirmations")]
    public async Task<IActionResult> ResendConfirmations([FromBody] ResendConfirmationsRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.AdminEmail) ||
            !req.AdminEmail.Equals(DirectorioOverrideEmail, StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { message = "Esta acción está restringida a la cuenta de Directorio." });
        }

        var excludeSet = new HashSet<string>((req.ExcludeEmails ?? new()).Select(e => e.ToLower()));

        var pending = await (
            from s in _db.ActivitySelections
            join a in _db.SelectableActivities on s.ActivityId equals a.Id
            where s.BlockId == 1 && s.IsConfirmed
            select new { s.UserEmail, a.Code, a.Title }
        ).ToListAsync();
        var toSendCount = pending.Count(p => !excludeSet.Contains(p.UserEmail.ToLower()));

        // Corre desacoplado del request HTTP: 700+ mails tardan más que
        // cualquier timeout razonable de reverse proxy (ya nos pasó una vez —
        // el envío sincrónico se cortó a mitad de camino cuando el proxy
        // cerró la conexión). Usa su propio scope/DbContext porque el de
        // este controller se dispone en cuanto el request HTTP termina.
        _ = Task.Run(() => RunBulkResendAsync(req.AdminEmail, excludeSet));

        return Accepted(new { message = "Reenvío iniciado en segundo plano.", toSend = toSendCount, excluded = excludeSet.Count });
    }

    private async Task RunBulkResendAsync(string adminEmail, HashSet<string> excludeEmails)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var confirmed = await (
            from s in db.ActivitySelections
            join a in db.SelectableActivities on s.ActivityId equals a.Id
            where s.BlockId == 1 && s.IsConfirmed
            select new { s.UserEmail, a.Code, a.Title }
        ).ToListAsync();

        var toSend = confirmed.Where(c => !excludeEmails.Contains(c.UserEmail.ToLower())).ToList();

        var emails = toSend.Select(c => c.UserEmail.ToLower()).ToList();
        var regsByEmail = await db.Registrations
            .Where(r => emails.Contains(r.Email.ToLower()))
            .ToDictionaryAsync(r => r.Email.ToLower(), r => $"{r.Name} {r.Lastname}");

        _logger.LogWarning(
            "[BULK RESEND STARTED] {Admin} — {Count} a enviar ({Excluded} ya enviados antes, excluidos).",
            adminEmail, toSend.Count, excludeEmails.Count);

        foreach (var c in toSend)
        {
            var name = regsByEmail.TryGetValue(c.UserEmail.ToLower(), out var n) ? n : c.UserEmail;
            // SendActivitySelectionConfirmedAsync ya loguea éxito/error puntual
            // y no propaga excepciones.
            await _email.SendActivitySelectionConfirmedAsync(c.UserEmail, name, c.Code, c.Title, EppPdfUrl);
            await Task.Delay(150);
        }

        _logger.LogWarning("[BULK RESEND DONE] {Admin} — terminó de recorrer {Count} personas.", adminEmail, toSend.Count);
    }
}
