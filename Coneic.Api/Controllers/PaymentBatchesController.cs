using Coneic.Api.Data;
using Coneic.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Coneic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentBatchesController : ControllerBase
    {
        private readonly JsonDataStore _store;

        public PaymentBatchesController(JsonDataStore store)
        {
            _store = store;
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

        [HttpPost]
        public IActionResult Create([FromBody] PaymentBatch batch)
        {
            if (string.IsNullOrWhiteSpace(batch.DelegateEmail))
                return BadRequest(new { message = "DelegateEmail es requerido." });

            var created = _store.AddPaymentBatch(batch);
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
