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
    };

    private const string EppPdfUrl =
        "https://coneic2026storage.blob.core.windows.net/comprobantes/misc/2026-09/eleccion-de-actividades.pdf";

    public ActivitySelectionController(ApplicationDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    // ── Listado de bloques + opciones + cupos + tu elección actual (draft o confirmada) ──

    [HttpGet("blocks")]
    public async Task<IActionResult> GetBlocks([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Falta el email." });

        var mySelections = await _db.ActivitySelections
            .Where(s => s.UserEmail.ToLower() == email.ToLower())
            .ToListAsync();

        var blocks = await _db.ActivityBlocks.Where(b => b.IsActive).OrderBy(b => b.Id).ToListAsync();
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
}
