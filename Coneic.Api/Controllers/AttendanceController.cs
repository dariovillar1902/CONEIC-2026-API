using Coneic.Api.Data;
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

    public record CheckInRequest(string Code);

    public record CheckInResponse(
        bool Found,
        bool AlreadyCheckedIn,
        int? Id,
        string? Name,
        string? Lastname,
        string? Faculty,
        string? Dni,
        bool IsEnabled,
        string? Status,
        DateTime? CheckedInAt);

    // Accepts either the Registration.Id or the Dni (digits only) as the
    // scanned/typed code, so the QR and the manual fallback are interchangeable.
    private async Task<Models.Registration?> FindByCodeAsync(string code)
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

    [HttpPost("checkin")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest req)
    {
        var reg = await FindByCodeAsync(req.Code);
        if (reg == null)
            return NotFound(new CheckInResponse(false, false, null, null, null, null, null, false, null, null));

        var alreadyCheckedIn = reg.CheckedIn;
        if (!alreadyCheckedIn)
        {
            reg.CheckedIn = true;
            reg.CheckedInAt = DateTime.Now;
            await _db.SaveChangesAsync();
        }

        return Ok(new CheckInResponse(
            true,
            alreadyCheckedIn,
            reg.Id,
            reg.Name,
            reg.Lastname,
            reg.Faculty,
            reg.Dni,
            reg.IsEnabled,
            reg.Status,
            reg.CheckedInAt));
    }

    // Preview without marking check-in, so the scanner UI can show who it is
    // before confirming (and to test codes without side effects).
    [HttpGet("lookup/{code}")]
    public async Task<IActionResult> Lookup(string code)
    {
        var reg = await FindByCodeAsync(code);
        if (reg == null)
            return NotFound(new CheckInResponse(false, false, null, null, null, null, null, false, null, null));

        return Ok(new CheckInResponse(
            true,
            reg.CheckedIn,
            reg.Id,
            reg.Name,
            reg.Lastname,
            reg.Faculty,
            reg.Dni,
            reg.IsEnabled,
            reg.Status,
            reg.CheckedInAt));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var total = await _db.Registrations.CountAsync();
        var checkedIn = await _db.Registrations.CountAsync(r => r.CheckedIn);
        return Ok(new { total, checkedIn });
    }
}
