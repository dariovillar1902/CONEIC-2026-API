using Coneic.Api.Data;
using Coneic.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Controllers;

[ApiController]
[Route("api/manual-comments")]
public class ManualCommentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ManualCommentsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.ManualComments.OrderByDescending(c => c.CreatedAt).ToListAsync());

    public record CreateCommentRequest(string AuthorEmail, string AuthorName, string SectionId, string Content);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content) || req.Content.Length > 1000)
            return BadRequest(new { message = "Contenido inválido." });

        if (string.IsNullOrWhiteSpace(req.AuthorEmail))
            return BadRequest(new { message = "Email requerido." });

        if (string.IsNullOrWhiteSpace(req.SectionId))
            return BadRequest(new { message = "Sección requerida." });

        var comment = new ManualComment
        {
            AuthorEmail = req.AuthorEmail.Trim(),
            AuthorName = string.IsNullOrWhiteSpace(req.AuthorName) ? req.AuthorEmail.Split('@')[0] : req.AuthorName.Trim(),
            SectionId = req.SectionId.Trim(),
            Content = req.Content.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        _db.ManualComments.Add(comment);
        await _db.SaveChangesAsync();
        return Ok(comment);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] string requesterEmail)
    {
        if (string.IsNullOrWhiteSpace(requesterEmail))
            return BadRequest(new { message = "Email requerido." });

        var comment = await _db.ManualComments.FindAsync(id);
        if (comment == null) return NotFound();

        var isAuthor = string.Equals(comment.AuthorEmail, requesterEmail, StringComparison.OrdinalIgnoreCase);
        var isAdmin = await _db.Users.AnyAsync(u =>
            u.Email.ToLower() == requesterEmail.ToLower() && u.Role == "admin");

        if (!isAuthor && !isAdmin)
            return StatusCode(403, new { message = "No tenés permiso para borrar este comentario." });

        _db.ManualComments.Remove(comment);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
