using Coneic.Api.Data;
using Coneic.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentBatchesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PaymentBatchesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get all batches for a delegation
        [HttpGet("delegation")]
        public async Task<ActionResult<IEnumerable<PaymentBatch>>> GetByDelegation([FromQuery] string name)
        {
            return await _context.PaymentBatches
                .Where(b => b.DelegationName == name)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        // Get all batches (admin)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentBatch>>> GetAll()
        {
            return await _context.PaymentBatches
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PaymentBatch batch)
        {
            batch.CreatedAt = DateTime.Now;
            _context.PaymentBatches.Add(batch);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = batch.Id }, batch);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var batch = await _context.PaymentBatches.FindAsync(id);
            if (batch == null) return NotFound();
            return Ok(batch);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PaymentBatch updated)
        {
            var batch = await _context.PaymentBatches.FindAsync(id);
            if (batch == null) return NotFound();

            batch.ReceiptUrl = updated.ReceiptUrl;
            batch.Description = updated.Description;
            await _context.SaveChangesAsync();
            return Ok(batch);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var batch = await _context.PaymentBatches.FindAsync(id);
            if (batch == null) return NotFound();

            _context.PaymentBatches.Remove(batch);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
