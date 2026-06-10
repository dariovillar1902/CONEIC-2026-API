using Coneic.Api.Data;
using Coneic.Api.Models;
using Coneic.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Coneic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentBatchesController : ControllerBase
    {
        private readonly JsonDataStore _store;
        private readonly IEmailService _email;

        public PaymentBatchesController(JsonDataStore store, IEmailService email)
        {
            _store = store;
            _email = email;
        }

        /// <summary>Get all batches for the logged-in delegate (by email query param).</summary>
        [HttpGet("delegate")]
        public ActionResult<IEnumerable<PaymentBatch>> GetByDelegate([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Se requiere el email del delegado." });

            return Ok(_store.GetPaymentBatchesByDelegate(email));
        }

        /// <summary>Get all batches — admin use.</summary>
        [HttpGet]
        public ActionResult<IEnumerable<PaymentBatch>> GetAll()
            => Ok(_store.GetAllPaymentBatches());

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var batch = _store.GetPaymentBatchById(id);
            if (batch == null) return NotFound();
            return Ok(batch);
        }

        private const string LoginUrl = "https://coneic2026.com.ar/login";

        private static string GeneratePassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            return new string(Enumerable.Range(0, 10).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
        }

        [HttpPost]
        public IActionResult Create([FromBody] PaymentBatch batch)
        {
            if (string.IsNullOrWhiteSpace(batch.DelegateEmail))
                return BadRequest(new { message = "DelegateEmail es requerido." });

            var created = _store.AddPaymentBatch(batch);

            // Actualizar paymentCondition en cada registro asignado.
            // La confirmación de pago (email + creación de usuario) la gestiona tesorería
            // desde su panel usando POST /api/registrations/{id}/confirm-payment.
            foreach (var assignment in created.Assignments)
            {
                var reg = _store.GetRegistrationById(assignment.RegistrationId);
                if (reg == null) continue;
                _store.UpdatePayment(assignment.RegistrationId, reg.IsEnabled, assignment.PaymentType);
            }

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] PaymentBatch updated)
        {
            var result = _store.UpdatePaymentBatch(id, updated);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            if (!_store.DeletePaymentBatch(id)) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Tesorería validates receipt: marks batch as validated and sends emails
        /// based on payment type per assignment ("Pagó Completo", "Pagó 1° Cuota", "Pagó 2° Cuota").
        /// </summary>
        [HttpPost("{id}/validate")]
        public async Task<IActionResult> Validate(int id)
        {
            var batch = _store.GetPaymentBatchById(id);
            if (batch == null) return NotFound();
            if (batch.IsValidated)
                return BadRequest(new { message = "Este comprobante ya fue validado." });

            var validated = _store.ValidateBatch(id);
            if (validated == null) return NotFound();

            foreach (var assignment in validated.Assignments)
            {
                var reg = _store.GetRegistrationById(assignment.RegistrationId);
                if (reg == null) continue;

                var fullName = $"{reg.Name} {reg.Lastname}";

                if (assignment.PaymentType == "Pagó 1° Cuota")
                {
                    await _email.SendFirstPaymentReceivedAsync(reg.Email, fullName, "a confirmar");
                }
                else if (assignment.PaymentType is "Pagó Completo" or "Pagó 2° Cuota")
                {
                    if (reg.Status != "Paid")
                    {
                        _store.UpdateStatus(assignment.RegistrationId, "Paid");
                        var tempPassword = GeneratePassword();
                        _store.CreateUserFromRegistration(reg.Email, tempPassword);
                        await _email.SendRegistrationConfirmedAsync(
                            toEmail: reg.Email,
                            toName: fullName,
                            paymentDetail: assignment.PaymentType,
                            tempPassword: tempPassword,
                            loginUrl: LoginUrl);
                    }
                }
            }

            return Ok(validated);
        }
    }
}
