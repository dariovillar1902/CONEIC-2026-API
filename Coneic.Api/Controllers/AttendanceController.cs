using Coneic.Api.Data;
using Coneic.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AttendanceController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // Accepts either the Registration.Id or the Dni (digits only) as the
    // scanned/typed code, so the QR and the manual fallback are interchangeable.
    private async Task<Registration?> FindByCodeAsync(string code)
    {
        var trimmed = (code ?? string.Empty).Trim();
        if (trimmed.Length == 0) return null;

        if (int.TryParse(trimmed, out var id))
        {
            var byId = await _db.Registrations.FindAsync(id);
            if (byId != null) return byId;
        }

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        return await _db.Registrations
            .FirstOrDefaultAsync(r => r.Dni == digits || r.Dni == trimmed);
    }

    private record RegistrantInfo(int Id, string Name, string Lastname, string Faculty, string Dni, bool IsEnabled, string Status);

    private static RegistrantInfo ToInfo(Registration r) =>
        new(r.Id, r.Name, r.Lastname, r.Faculty, r.Dni, r.IsEnabled, r.Status);

    // ── Sessions (instancias de toma de asistencia) ────────────────────────────

    public record CreateSessionRequest(string Name);

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var sessions = await _db.AttendanceSessions
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.CreatedAt,
                CheckedInCount = _db.AttendanceRecords.Count(r => r.SessionId == s.Id),
            })
            .ToListAsync();

        return Ok(sessions);
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest req)
    {
        var name = (req.Name ?? string.Empty).Trim();
        if (name.Length == 0) return BadRequest(new { message = "El nombre de la instancia es requerido." });

        var session = new AttendanceSession { Name = name };
        _db.AttendanceSessions.Add(session);
        await _db.SaveChangesAsync();
        return Ok(session);
    }

    // ── Lookup (previsualizar antes de confirmar) ───────────────────────────────

    [HttpGet("lookup/{code}")]
    public async Task<IActionResult> Lookup(string code, [FromQuery] int sessionId)
    {
        var reg = await FindByCodeAsync(code);
        if (reg == null)
            return NotFound(new { found = false });

        var alreadyIn = await _db.AttendanceRecords
            .AnyAsync(r => r.RegistrationId == reg.Id && r.SessionId == sessionId);

        return Ok(new { found = true, alreadyCheckedIn = alreadyIn, registrant = ToInfo(reg) });
    }

    // ── Check-in (confirmar) ─────────────────────────────────────────────────

    public record CheckInRequest(string Code, int SessionId, string? CheckedInBy);

    [HttpPost("checkin")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest req)
    {
        var sessionExists = await _db.AttendanceSessions.AnyAsync(s => s.Id == req.SessionId);
        if (!sessionExists)
            return BadRequest(new { message = "La instancia de asistencia indicada no existe." });

        var reg = await FindByCodeAsync(req.Code);
        if (reg == null)
            return NotFound(new { found = false });

        var existing = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.RegistrationId == reg.Id && r.SessionId == req.SessionId);

        if (existing != null)
            return Ok(new { found = true, alreadyCheckedIn = true, registrant = ToInfo(reg), checkedInAt = existing.CheckedInAt });

        var record = new AttendanceRecord
        {
            RegistrationId = reg.Id,
            SessionId = req.SessionId,
            CheckedInBy = req.CheckedInBy,
        };
        _db.AttendanceRecords.Add(record);
        await _db.SaveChangesAsync();

        return Ok(new { found = true, alreadyCheckedIn = false, registrant = ToInfo(reg), checkedInAt = record.CheckedInAt });
    }

    // ── Historial ────────────────────────────────────────────────────────────

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] int? sessionId)
    {
        var query = _db.AttendanceRecords.AsQueryable();
        if (sessionId.HasValue) query = query.Where(r => r.SessionId == sessionId.Value);

        var records = await query
            .OrderByDescending(r => r.CheckedInAt)
            .Join(_db.Registrations, r => r.RegistrationId, reg => reg.Id, (r, reg) => new
            {
                r.Id,
                r.SessionId,
                r.CheckedInAt,
                r.CheckedInBy,
                Registrant = new { reg.Id, reg.Name, reg.Lastname, reg.Faculty, reg.Dni },
            })
            .Take(500)
            .ToListAsync();

        return Ok(records);
    }

    // ── Buscar (¿ya se registró este número/DNI?) ───────────────────────────────

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var reg = await FindByCodeAsync(query);
        if (reg == null)
            return NotFound(new { found = false });

        var records = await _db.AttendanceRecords
            .Where(r => r.RegistrationId == reg.Id)
            .Join(_db.AttendanceSessions, r => r.SessionId, s => s.Id, (r, s) => new
            {
                SessionId = s.Id,
                SessionName = s.Name,
                r.CheckedInAt,
            })
            .OrderByDescending(r => r.CheckedInAt)
            .ToListAsync();

        return Ok(new { found = true, registrant = ToInfo(reg), sessions = records });
    }

    // ── Módulo de asistencia (planillas en papel) ────────────────────────────────

    private Task<bool> IsStaffAsync(string? email) =>
        string.IsNullOrWhiteSpace(email)
            ? Task.FromResult(false)
            : _db.Users.AnyAsync(u => u.Email.ToLower() == email.Trim().ToLower()
                                      && (u.Role == "admin" || u.Role == "tesoreria"));

    /// <summary>Toda la grilla persona × instancia en una sola llamada (para las vistas por persona y por evento).</summary>
    [HttpGet("matrix")]
    public async Task<IActionResult> Matrix([FromQuery] string requesterEmail)
    {
        if (!await IsStaffAsync(requesterEmail)) return StatusCode(403, new { message = "Solo administradores." });

        var sessions = await _db.AttendanceSessions.AsNoTracking().OrderBy(s => s.Id)
            .Select(s => new { s.Id, s.Name }).ToListAsync();
        var records = await _db.AttendanceRecords.AsNoTracking()
            .Select(r => new { r.RegistrationId, r.SessionId, r.CheckedInAt }).ToListAsync();
        var bySession = records.GroupBy(r => r.RegistrationId).ToDictionary(g => g.Key, g => g.ToList());

        var people = (await _db.Registrations.AsNoTracking()
                .Where(r => r.Status == "Paid")
                .Select(r => new { r.Id, r.Name, r.Lastname, r.Faculty, r.Dni })
                .ToListAsync())
            .Select(r => new
            {
                r.Id, r.Name, r.Lastname, r.Faculty, r.Dni,
                Sessions = bySession.TryGetValue(r.Id, out var list) ? list.Select(x => x.SessionId).ToList() : new List<int>(),
            })
            .OrderBy(r => r.Lastname).ThenBy(r => r.Name)
            .ToList();

        return Ok(new { sessions, people });
    }

    public record BulkCheckInRequest(int SessionId, List<string> Codes, string? CheckedInBy);

    /// <summary>Carga una planilla: lista de números (ID de inscripción o DNI). Idempotente por persona e instancia.</summary>
    [HttpPost("bulk-checkin")]
    public async Task<IActionResult> BulkCheckIn([FromBody] BulkCheckInRequest req)
    {
        if (!await IsStaffAsync(req.CheckedInBy)) return StatusCode(403, new { message = "Solo administradores." });
        if (!await _db.AttendanceSessions.AnyAsync(s => s.Id == req.SessionId))
            return BadRequest(new { message = "La instancia de asistencia indicada no existe." });

        var already = (await _db.AttendanceRecords.Where(r => r.SessionId == req.SessionId)
            .Select(r => r.RegistrationId).ToListAsync()).ToHashSet();
        var added = new List<object>();
        var duplicates = 0;
        var notFound = new List<string>();

        foreach (var code in (req.Codes ?? new()).Select(c => (c ?? "").Trim()).Where(c => c.Length > 0).Distinct())
        {
            var reg = await FindByCodeAsync(code);
            if (reg == null) { notFound.Add(code); continue; }
            if (!already.Add(reg.Id)) { duplicates++; continue; }
            _db.AttendanceRecords.Add(new AttendanceRecord
            {
                RegistrationId = reg.Id, SessionId = req.SessionId, CheckedInBy = req.CheckedInBy,
            });
            added.Add(new { code, reg.Id, reg.Name, reg.Lastname, reg.Faculty, reg.Status });
        }
        await _db.SaveChangesAsync();
        return Ok(new { added, duplicates, notFound });
    }

    /// <summary>Quita una asistencia cargada por error.</summary>
    [HttpDelete("checkin")]
    public async Task<IActionResult> RemoveCheckIn([FromQuery] int registrationId, [FromQuery] int sessionId, [FromQuery] string requesterEmail)
    {
        if (!await IsStaffAsync(requesterEmail)) return StatusCode(403, new { message = "Solo administradores." });
        var rec = await _db.AttendanceRecords.FirstOrDefaultAsync(r => r.RegistrationId == registrationId && r.SessionId == sessionId);
        if (rec == null) return NotFound();
        _db.AttendanceRecords.Remove(rec);
        await _db.SaveChangesAsync();
        return Ok(new { removed = true });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats([FromQuery] int? sessionId)
    {
        var total = await _db.Registrations.CountAsync();
        var checkedIn = sessionId.HasValue
            ? await _db.AttendanceRecords.CountAsync(r => r.SessionId == sessionId.Value)
            : await _db.AttendanceRecords.Select(r => r.RegistrationId).Distinct().CountAsync();
        return Ok(new { total, checkedIn });
    }
}
