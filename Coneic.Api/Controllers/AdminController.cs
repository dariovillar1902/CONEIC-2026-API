using Coneic.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Coneic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IEmailService _email;

    public AdminController(IEmailService email)
    {
        _email = email;
    }

    public record ResendCredentialsRequest(string Email, string Name, string Password, string PaymentDetail = "Pago Único");

    [HttpPost("resend-credentials")]
    public async Task<IActionResult> ResendCredentials([FromBody] ResendCredentialsRequest req)
    {
        await _email.SendRegistrationConfirmedAsync(
            req.Email,
            req.Name,
            req.PaymentDetail,
            req.Password,
            "https://coneic2026.com.ar/login");

        return Ok(new { message = $"Email enviado a {req.Email}" });
    }
}
