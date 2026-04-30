using Coneic.Api.Data;
using Coneic.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Coneic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly JsonDataStore _store;

        public UsersController(JsonDataStore store)
        {
            _store = store;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _store.FindUser(request.Email, request.Password);

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                role = user.Role,
                delegationName = user.DelegationName
            });
        }
    }
}
