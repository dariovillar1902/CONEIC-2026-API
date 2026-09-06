using Coneic.Api.Data;
using Coneic.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Controllers;

[ApiController]
[Route("api/app-settings")]
public class AppSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AppSettingsController(ApplicationDbContext db) => _db = db;

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        var setting = await _db.AppSettings.FindAsync(key);
        return Ok(new { key, value = setting?.Value });
    }

    public record UpdateSettingRequest(string Value, string RequesterEmail);

    [HttpPut("{key}")]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateSettingRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RequesterEmail))
            return BadRequest(new { message = "Email requerido." });

        var isAdmin = await _db.Users.AnyAsync(u =>
            u.Email.ToLower() == req.RequesterEmail.ToLower() && u.Role == "admin");
        if (!isAdmin)
            return StatusCode(403, new { message = "No tenés permiso para cambiar esta configuración." });

        var setting = await _db.AppSettings.FindAsync(key);
        if (setting == null)
        {
            setting = new AppSetting { Key = key, Value = req.Value };
            _db.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = req.Value;
        }

        await _db.SaveChangesAsync();
        return Ok(new { key, value = setting.Value });
    }
}
