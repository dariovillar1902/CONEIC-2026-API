using Coneic.Api.Data;
using Coneic.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivitySelectionController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ActivitySelectionController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ── Listado de bloques + opciones + cupos + tu elección actual ─────────────

    [HttpGet("blocks")]
    public async Task<IActionResult> GetBlocks([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Falta el email." });

        var mySelections = await _db.ActivitySelections
            .Where(s => s.UserEmail.ToLower() == email.ToLower())
            .ToListAsync();

        var blocks = await _db.ActivityBlocks.OrderBy(b => b.Id).ToListAsync();
        var activities = await _db.SelectableActivities.OrderBy(a => a.Code).ToListAsync();

        var takenCounts = await _db.ActivitySelections
            .GroupBy(s => s.ActivityId)
            .Select(g => new { ActivityId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ActivityId, x => x.Count);

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
                a.Capacity,
                Taken = takenCounts.GetValueOrDefault(a.Id, 0),
            }),
        });

        return Ok(result);
    }

    // ── Elegir / cambiar de opción dentro de un bloque ──────────────────────────

    public record SelectRequest(string Email, int ActivityId);

    [HttpPost("select")]
    public async Task<IActionResult> Select([FromBody] SelectRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { message = "Falta el email." });

        var activity = await _db.SelectableActivities.FindAsync(req.ActivityId);
        if (activity == null)
            return NotFound(new { message = "La actividad indicada no existe." });

        var existing = await _db.ActivitySelections
            .FirstOrDefaultAsync(s => s.UserEmail.ToLower() == req.Email.ToLower() && s.BlockId == activity.BlockId);

        // Si ya tenías elegida esta misma actividad, no hay nada que hacer.
        if (existing != null && existing.ActivityId == activity.Id)
            return Ok(new { message = "Ya tenías esta actividad seleccionada." });

        // Chequeo de cupo (no cuenta tu propio cupo anterior si estás cambiando de opción).
        var taken = await _db.ActivitySelections.CountAsync(s => s.ActivityId == activity.Id);
        if (taken >= activity.Capacity)
            return BadRequest(new { message = "No quedan cupos disponibles para esta actividad." });

        if (existing != null)
        {
            _db.ActivitySelections.Remove(existing);
        }

        _db.ActivitySelections.Add(new ActivitySelection
        {
            UserEmail = req.Email,
            BlockId = activity.BlockId,
            ActivityId = activity.Id,
        });

        await _db.SaveChangesAsync();
        return Ok(new { message = "Selección guardada.", activityId = activity.Id, blockId = activity.BlockId });
    }

    // ── Quitar tu elección en un bloque ──────────────────────────────────────

    [HttpDelete("select")]
    public async Task<IActionResult> Unselect([FromQuery] string email, [FromQuery] int blockId)
    {
        var existing = await _db.ActivitySelections
            .FirstOrDefaultAsync(s => s.UserEmail.ToLower() == email.ToLower() && s.BlockId == blockId);

        if (existing == null) return NoContent();

        _db.ActivitySelections.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
