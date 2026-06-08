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
        public async Task<IActionResult> Create([FromBody] PaymentBatch batch)
        {
            if (string.IsNullOrWhiteSpace(batch.DelegateEmail))
                return BadRequest(new { message = "DelegateEmail es requerido." });

            var created = _store.AddPaymentBatch(batch);

            // Procesar cada asignación: actualizar paymentCondition y enviar emails
            foreach (var assignment in created.Assignments)
            {
                var reg = _store.GetRegistrationById(assignment.RegistrationId);
                if (reg == null) continue;

                // Actualizar paymentCondition en el registro (preserva isEnabled actual)
                _store.UpdatePayment(assignment.RegistrationId, reg.IsEnabled, assignment.PaymentType);

                switch (assignment.PaymentType)
                {
                    case "Pagó Completo":
                    case "Pagó 2° Cuota":
                    {
                        // Crear usuario en portal y enviar email de confirmación
                        var tempPassword = GeneratePassword();
                        _store.CreateUserFromRegistration(reg.Email, tempPassword);
                        await _email.SendRegistrationConfirmedAsync(
                            toEmail:       reg.Email,
                            toName:        $"{reg.Name} {reg.Lastname}",
                            paymentDetail: assignment.PaymentType,
                            tempPassword:  tempPassword,
                            loginUrl:      LoginUrl);
                        break;
                    }
                    case "Pagó 1° Cuota":
                        await _email.SendFirstPaymentReceivedAsync(
                            toEmail: reg.Email,
                            toName:  $"{reg.Name} {reg.Lastname}",
                            dueDate: "a confirmar con tu delegado/a");
                        break;
                }
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
    }
}
