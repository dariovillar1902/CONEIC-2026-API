using ClosedXML.Excel;
using Coneic.Api.Data;
using Coneic.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Coneic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationsController : ControllerBase
    {
        private readonly JsonDataStore _store;

        public RegistrationsController(JsonDataStore store)
        {
            _store = store;
        }

        [HttpPost]
        public IActionResult Create([FromBody] Registration registration)
        {
            var created = _store.AddRegistration(registration);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var reg = _store.GetRegistrationById(id);
            if (reg == null) return NotFound();
            return Ok(reg);
        }

        // For Admin
        [HttpGet]
        public ActionResult<IEnumerable<Registration>> GetAll()
        {
            return Ok(_store.GetAllRegistrations());
        }

        // For Delegates
        [HttpGet("delegation")]
        public ActionResult<IEnumerable<Registration>> GetByDelegation([FromQuery] string name)
        {
            return Ok(_store.GetRegistrationsByFaculty(name));
        }

        [HttpPut("{id}/status")]
        public IActionResult UpdateStatus(int id, [FromBody] string status)
        {
            if (!_store.UpdateStatus(id, status))
                return NotFound();
            return Ok(_store.GetRegistrationById(id));
        }

        [HttpPatch("{id}/payment")]
        public IActionResult UpdatePayment(int id, [FromBody] UpdatePaymentDto dto)
        {
            if (!_store.UpdatePayment(id, dto.IsEnabled, dto.PaymentCondition))
                return NotFound();
            return Ok(_store.GetRegistrationById(id));
        }

        // Export all registrations to Excel (admin)
        [HttpGet("export")]
        public IActionResult ExportAll()
        {
            var registrations = _store.GetAllRegistrations();
            var fileBytes = BuildExcel(registrations, "Todas las Inscripciones");
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "inscripciones.xlsx");
        }

        // Export registrations by delegation to Excel (delegate)
        [HttpGet("export/delegation")]
        public IActionResult ExportByDelegation([FromQuery] string name)
        {
            var registrations = _store.GetRegistrationsByFaculty(name);
            var fileBytes = BuildExcel(registrations, name);
            var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"inscripciones_{safeName}.xlsx");
        }

        private static byte[] BuildExcel(IEnumerable<Registration> registrations, string sheetTitle)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Inscripciones");

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
