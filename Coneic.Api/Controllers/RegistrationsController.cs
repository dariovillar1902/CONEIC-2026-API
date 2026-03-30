using Coneic.Api.Data;
using Coneic.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RegistrationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Registration registration)
        {
            registration.CreatedAt = DateTime.Now;
            registration.Status = "Pending";
            
            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetById), new { id = registration.Id }, registration);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var reg = await _context.Registrations.FindAsync(id);
            if (reg == null) return NotFound();
            return Ok(reg);
        }

        // For Admin
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Registration>>> GetAll()
        {
            return await _context.Registrations.ToListAsync();
        }

        // For Delegates
        [HttpGet("delegation")]
        public async Task<ActionResult<IEnumerable<Registration>>> GetByDelegation([FromQuery] string name)
        {
            return await _context.Registrations
                .Where(r => r.Faculty == name)
                .ToListAsync();
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var reg = await _context.Registrations.FindAsync(id);
            if (reg == null) return NotFound();

            reg.Status = status;
            await _context.SaveChangesAsync();
            return Ok(reg);
        }
    }
}
