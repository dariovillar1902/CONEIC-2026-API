using Coneic.Api.Data;
using Coneic.Api.Models;
using Coneic.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentBatchesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _email;

        private const string LoginUrl = "https://coneic2026.com.ar/login";

        private static string GetSecondInstallmentDeadline(string? stageName) => stageName switch
        {
            "1ª Etapa" or "Primera Etapa" => "16 de agosto de 2026",
            "2ª Etapa" or "Segunda Etapa" => "20 de septiembre de 2026",
            _ => "la fecha indicada por tu delegado/a"
        };

        public PaymentBatchesController(ApplicationDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        [HttpGet("delegate")]
        public ActionResult<IEnumerable<PaymentBatch>> GetByDelegate([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Se requiere el email del delegado." });

            var batches = _db.PaymentBatches.AsEnumerable()
                .Where(b => b.DelegateEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            return Ok(batches);
        }

        [HttpGet]
        public ActionResult<IEnumerable<PaymentBatch>> GetAll()
        {
            var batches = _db.PaymentBatches
                .OrderByDescending(b => b.CreatedAt)
                .ToList();
            return Ok(batches);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var batch = _db.PaymentBatches.Find(id);
            if (batch == null) return NotFound();
            return Ok(batch);
        }

        [HttpPost]
        public IActionResult Create([FromBody] PaymentBatch batch)
        {
            if (string.IsNullOrWhiteSpace(batch.DelegateEmail))
                return BadRequest(new { message = "DelegateEmail es requerido." });

            batch.CreatedAt = DateTime.Now;
            _db.PaymentBatches.Add(batch);
            _db.SaveChanges();

            foreach (var assignment in batch.Assignments)
            {
                var reg = _db.Registrations.Find(assignment.RegistrationId);
                if (reg == null) continue;
                reg.PaymentCondition = assignment.PaymentType;
                _db.SaveChanges();
            }

            return CreatedAtAction(nameof(GetById), new { id = batch.Id }, batch);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] PaymentBatch updated)
        {
            var existing = _db.PaymentBatches.Find(id);
            if (existing == null) return NotFound();

            existing.ReceiptUrl = updated.ReceiptUrl;
            existing.Description = updated.Description;
            existing.Assignments = updated.Assignments;
            _db.SaveChanges();

            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var batch = _db.PaymentBatches.Find(id);
            if (batch == null) return NotFound();

            foreach (var assignment in batch.Assignments)
            {
                var reg = _db.Registrations.Find(assignment.RegistrationId);
                if (reg == null || reg.Status == "Paid") continue;
                reg.PaymentCondition = string.Empty;
            }

            _db.PaymentBatches.Remove(batch);
            _db.SaveChanges();
            return NoContent();
        }

        [HttpPost("{id}/validate")]
        public async Task<IActionResult> Validate(int id)
        {
            var batch = _db.PaymentBatches.Find(id);
            if (batch == null) return NotFound();
            if (batch.IsValidated)
                return BadRequest(new { message = "Este comprobante ya fue validado." });

            batch.IsValidated = true;
            batch.ValidatedAt = DateTime.Now;
            _db.SaveChanges();

            foreach (var assignment in batch.Assignments)
            {
                var reg = _db.Registrations.Find(assignment.RegistrationId);
                if (reg == null) continue;

                var fullName = $"{reg.Name} {reg.Lastname}";

                if (assignment.PaymentType == "Pagó 1° Cuota")
                {
                    var deadline = GetSecondInstallmentDeadline(reg.StageName);
                    await _email.SendFirstPaymentReceivedAsync(reg.Email, fullName, deadline);
                }
                else if (assignment.PaymentType is "Pagó Completo" or "Pagó 2° Cuota")
                {
                    if (reg.Status != "Paid")
                    {
                        reg.Status = "Paid";
                        var tempPassword = GeneratePassword();
                        CreateUserFromRegistration(reg.Email, tempPassword);
                        _db.SaveChanges();

                        await _email.SendRegistrationConfirmedAsync(
                            toEmail:       reg.Email,
                            toName:        fullName,
                            paymentDetail: assignment.PaymentType,
                            tempPassword:  tempPassword,
                            loginUrl:      LoginUrl,
                            amount:        reg.Price,
                            stageName:     reg.StageName ?? "");
                    }
                }
            }

            return Ok(batch);
        }

        private void CreateUserFromRegistration(string email, string password)
        {
            var exists = _db.Users.AsEnumerable()
                .Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (exists) return;

            var maxId = _db.Users.Any() ? _db.Users.Max(u => u.Id) : 0;
            _db.Users.Add(new User
            {
                Id = maxId + 1,
                Email = email,
                Password = password,
                Role = "assistant",
                MustChangePassword = true,
            });
        }

        private static string GeneratePassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            return new string(Enumerable.Range(0, 10).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
        }
    }
}
