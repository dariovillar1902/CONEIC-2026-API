using Coneic.Api.Data;
using Coneic.Api.Models;
using Coneic.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _email;

        // Evita que se pueda re-disparar el mail de reseteo para la misma
        // cuenta más de una vez cada 2 minutos (spam-click, o abuso).
        private static readonly TimeSpan ResetCooldown = TimeSpan.FromMinutes(2);

        public UsersController(ApplicationDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var email = request.Email.ToLower();
            var user = _db.Users.AsEnumerable()
                .FirstOrDefault(u =>
                    u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)
                    && u.Password == request.Password);

            if (user == null)
                return Unauthorized(new { message = "Credenciales inválidas" });

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                role = user.Role,
                delegationName = user.DelegationName,
                filial = user.Filial,
                managedFaculties = user.ManagedFaculties,
                mustChangePassword = user.MustChangePassword,
                quota = user.Quota,
            });
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var user = _db.Users.Find(id);
            if (user == null) return NotFound();
            _db.Users.Remove(user);
            _db.SaveChanges();
            return Ok(new { message = $"Usuario {user.Email} eliminado." });
        }

        [HttpDelete("by-role/{role}")]
        public IActionResult DeleteByRole(string role)
        {
            var users = _db.Users.Where(u => u.Role == role).ToList();
            _db.Users.RemoveRange(users);
            _db.SaveChanges();
            return Ok(new { deleted = users.Count, emails = users.Select(u => u.Email) });
        }

        [HttpPost("change-password")]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var user = _db.Users.AsEnumerable()
                .FirstOrDefault(u =>
                    u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)
                    && u.Password == request.CurrentPassword);

            if (user == null)
                return Unauthorized(new { message = "Contraseña actual incorrecta." });

            user.Password = request.NewPassword;
            user.MustChangePassword = false;
            _db.SaveChanges();

            return Ok(new { message = "Contraseña actualizada correctamente." });
        }

        // ── Olvidé mi contraseña: autogenera una nueva y la envía por mail ────────
        //
        // Devuelve siempre el mismo mensaje genérico exista o no la cuenta, para no
        // revelar por este medio qué emails están registrados en el sistema.
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            const string genericMessage =
                "Si el email ingresado tiene una cuenta, te enviamos una contraseña nueva.";

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null)
                return Ok(new { message = genericMessage });

            if (user.LastPasswordResetRequestAt.HasValue
                && DateTime.UtcNow - user.LastPasswordResetRequestAt.Value < ResetCooldown)
            {
                // Ya se envió un mail hace poco — no generamos uno nuevo (invalidaría
                // el anterior sin necesidad) ni reenviamos para evitar spam-click.
                return Ok(new { message = genericMessage });
            }

            var newPassword = GeneratePassword();
            user.Password = newPassword;
            user.MustChangePassword = true;
            user.LastPasswordResetRequestAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var registration = await _db.Registrations
                .Where(r => r.Email.ToLower() == user.Email.ToLower())
                .Select(r => new { r.Name, r.Lastname })
                .FirstOrDefaultAsync();
            var toName = registration != null ? $"{registration.Name} {registration.Lastname}" : user.Email;

            try
            {
                await _email.SendPasswordResetAsync(user.Email, toName, newPassword);
            }
            catch
            {
                // No exponer detalles de envío al cliente — la contraseña ya quedó
                // actualizada en la base; un reintento generaría una nueva igual.
            }

            return Ok(new { message = genericMessage });
        }

        private static string GeneratePassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var rng = Random.Shared;
            return new string(Enumerable.Range(0, 10).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }
    }
}
