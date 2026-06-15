using Coneic.Api.Data;
using Coneic.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public UsersController(ApplicationDbContext db)
        {
            _db = db;
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
            });
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
    }
}
