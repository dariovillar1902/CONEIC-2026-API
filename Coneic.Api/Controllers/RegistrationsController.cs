using ClosedXML.Excel;
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

        // Update enabled flag and payment condition (delegate action)
        [HttpPatch("{id}/payment")]
        public async Task<IActionResult> UpdatePayment(int id, [FromBody] UpdatePaymentDto dto)
        {
            var reg = await _context.Registrations.FindAsync(id);
            if (reg == null) return NotFound();

            reg.IsEnabled = dto.IsEnabled;
            reg.PaymentCondition = dto.PaymentCondition;
            await _context.SaveChangesAsync();
            return Ok(reg);
        }

        // Export all registrations to Excel (admin)
        [HttpGet("export")]
        public async Task<IActionResult> ExportAll()
        {
            var registrations = await _context.Registrations.ToListAsync();
            var fileBytes = BuildExcel(registrations, "Todas las Inscripciones");
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "inscripciones.xlsx");
        }

        // Export registrations by delegation to Excel (delegate)
        [HttpGet("export/delegation")]
        public async Task<IActionResult> ExportByDelegation([FromQuery] string name)
        {
            var registrations = await _context.Registrations
                .Where(r => r.Faculty == name)
                .ToListAsync();

            var fileBytes = BuildExcel(registrations, name);
            var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"inscripciones_{safeName}.xlsx");
        }

        private static byte[] BuildExcel(IEnumerable<Registration> registrations, string sheetTitle)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Inscripciones");

            // Header row
            var headers = new[]
            {
                "ID", "Apellido", "Nombre", "DNI", "Teléfono", "Email", "Delegación",
                "Grupo Sanguíneo", "Afecciones", "Contacto Emergencia", "Tel. Emergencia",
                "Etapa", "Precio", "Estado", "Habilitado", "Condición de Pago",
                "Monto Pagado", "Monto Pendiente", "Método de Pago", "Comprobante", "Fecha Inscripción"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#C00000");
                cell.Style.Font.FontColor = XLColor.White;
            }

            // Data rows
            int row = 2;
            foreach (var r in registrations)
            {
                ws.Cell(row, 1).Value = r.Id;
                ws.Cell(row, 2).Value = r.Lastname;
                ws.Cell(row, 3).Value = r.Name;
                ws.Cell(row, 4).Value = r.Dni;
                ws.Cell(row, 5).Value = r.Phone;
                ws.Cell(row, 6).Value = r.Email;
                ws.Cell(row, 7).Value = r.Faculty;
                ws.Cell(row, 8).Value = r.BloodType ?? "";
                ws.Cell(row, 9).Value = r.MedicalConditions ?? "";
                ws.Cell(row, 10).Value = r.EmergencyContactName;
                ws.Cell(row, 11).Value = r.EmergencyContactPhone;
                ws.Cell(row, 12).Value = r.StageName;
                ws.Cell(row, 13).Value = (double)r.Price;
                ws.Cell(row, 14).Value = r.Status;
                ws.Cell(row, 15).Value = r.IsEnabled ? "Sí" : "No";
                ws.Cell(row, 16).Value = r.PaymentCondition ?? "Sin asignar";
                ws.Cell(row, 17).Value = (double)r.AmountPaid;
                ws.Cell(row, 18).Value = (double)r.AmountPending;
                ws.Cell(row, 19).Value = r.PaymentMethod ?? "";
                ws.Cell(row, 20).Value = r.PaymentReceiptUrl ?? "";
                ws.Cell(row, 21).Value = r.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }

    public class UpdatePaymentDto
    {
        public bool IsEnabled { get; set; }
        public string? PaymentCondition { get; set; }
    }
}
