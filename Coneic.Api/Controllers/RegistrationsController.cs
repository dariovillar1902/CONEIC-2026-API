using ClosedXML.Excel;
using Coneic.Api.Data;
using Coneic.Api.Models;
using Coneic.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _email;
        private readonly IBlobStorageService _blob;

        private const string LoginUrl = "https://coneic2026.com.ar/login";

        public RegistrationsController(
            ApplicationDbContext db,
            IEmailService email, IBlobStorageService blob)
        {
            _db    = db;
            _email = email;
            _blob  = blob;
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static string GetPaymentDeadline(string? stageName) => stageName switch
        {
            "1ª Etapa" or "Primera Etapa" => "8 de julio de 2026",
            "2ª Etapa" or "Segunda Etapa" => "12 de agosto de 2026",
            "3ª Etapa" or "Tercera Etapa" => "16 de septiembre de 2026",
            _ => "la fecha indicada por tu delegado/a"
        };

        // ── Create ──────────────────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Registration registration)
        {
            if (!string.IsNullOrWhiteSpace(registration.Dni)
                && _db.Registrations.Any(r => r.Dni == registration.Dni))
                return Conflict(new { message = "Ya existe una inscripción con ese DNI." });

            if (_db.Registrations.AsEnumerable()
                .Any(r => r.Email.Equals(registration.Email, StringComparison.OrdinalIgnoreCase)))
                return Conflict(new { message = "Ya existe una inscripción con ese email." });

            registration.CreatedAt = DateTime.Now;
            registration.Status = "Pending";
            registration.IsEnabled = false;
            _db.Registrations.Add(registration);
            _db.SaveChanges();

            var delegation = DelegateDirectory.Lookup(registration.Faculty);

            await _email.SendRegistrationReceivedAsync(new RegistrationEmailData(
                ToEmail:    registration.Email,
                ToName:     $"{registration.Name} {registration.Lastname}",
                Faculty:    registration.Faculty ?? "",
                Delegation: delegation
            ));

            return CreatedAtAction(nameof(GetById), new { id = registration.Id }, new { registration });
        }

        private static string GeneratePassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var rng = Random.Shared;
            return new string(Enumerable.Range(0, 10).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }

        // ── Read ────────────────────────────────────────────────────────────────

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var reg = _db.Registrations.Find(id);
            if (reg == null) return NotFound();
            return Ok(reg);
        }

        [HttpGet]
        public ActionResult<IEnumerable<Registration>> GetAll()
            => Ok(_db.Registrations.ToList());

        [HttpGet("delegation")]
        public ActionResult<IEnumerable<Registration>> GetByDelegation([FromQuery] string name)
        {
            var regs = _db.Registrations.AsEnumerable()
                .Where(r => string.Equals(r.Faculty, name, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Ok(regs);
        }

        [HttpGet("delegate")]
        public ActionResult<IEnumerable<Registration>> GetByDelegate([FromQuery] string email)
        {
            var user = _db.Users.AsEnumerable()
                .FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user == null) return NotFound(new { message = "Delegate not found." });

            var faculties = user.ManagedFaculties.Count > 0
                ? user.ManagedFaculties
                : (user.DelegationName != null ? new List<string> { user.DelegationName } : new List<string>());

            var regs = _db.Registrations.AsEnumerable()
                .Where(r => faculties.Contains(r.Faculty ?? "", StringComparer.OrdinalIgnoreCase))
                .ToList();
            return Ok(regs);
        }

        // ── Update ──────────────────────────────────────────────────────────────

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var reg = _db.Registrations.Find(id);
            if (reg == null) return NotFound();

            reg.Status = status;
            _db.SaveChanges();

            if (status == "Paid")
            {
                var generatedPassword = GeneratePassword();
                CreateUserFromRegistration(reg.Email, generatedPassword);

                await _email.SendRegistrationConfirmedAsync(
                    toEmail:       reg.Email,
                    toName:        $"{reg.Name} {reg.Lastname}",
                    paymentDetail: reg.PaymentCondition ?? "Pago completo",
                    tempPassword:  generatedPassword,
                    loginUrl:      LoginUrl);

                return Ok(new { registration = reg, generatedPassword });
            }

            return Ok(reg);
        }

        [HttpPatch("{id}/payment")]
        public async Task<IActionResult> UpdatePayment(int id, [FromBody] UpdatePaymentDto dto)
        {
            var reg = _db.Registrations.Find(id);
            if (reg == null) return NotFound();

            var wasEnabled = reg.IsEnabled;
            reg.IsEnabled = dto.IsEnabled;
            reg.PaymentCondition = dto.PaymentCondition;
            _db.SaveChanges();

            if (dto.IsEnabled && !wasEnabled)
            {
                var delegation = DelegateDirectory.Lookup(reg.Faculty);
                var deadline   = GetPaymentDeadline(reg.StageName);

                await _email.SendRegistrationValidatedAsync(
                    toEmail:         reg.Email,
                    toName:          $"{reg.Name} {reg.Lastname}",
                    delegation:      delegation,
                    paymentDeadline: deadline);
            }

            return Ok(reg);
        }

        [HttpPatch("{id}/amounts")]
        public IActionResult UpdateAmounts(int id, [FromBody] UpdateAmountsDto dto)
        {
            var reg = _db.Registrations.Find(id);
            if (reg == null) return NotFound();

            reg.AmountPaid = dto.AmountPaid;
            reg.AmountPending = dto.AmountPending;
            _db.SaveChanges();
            return Ok(reg);
        }

        [HttpPatch("{id}/observations")]
        public IActionResult UpdateObservations(int id, [FromBody] string? observations)
        {
            var reg = _db.Registrations.Find(id);
            if (reg == null) return NotFound();

            reg.Observations = observations;
            _db.SaveChanges();
            return Ok(reg);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Registration updated)
        {
            var reg = _db.Registrations.Find(id);
            if (reg == null) return NotFound();

            reg.Name                  = updated.Name;
            reg.Lastname              = updated.Lastname;
            reg.Dni                   = updated.Dni;
            reg.Phone                 = updated.Phone;
            reg.Email                 = updated.Email;
            reg.Faculty               = updated.Faculty;
            reg.BloodType             = updated.BloodType;
            reg.MedicalConditions     = updated.MedicalConditions;
            reg.EmergencyContactName  = updated.EmergencyContactName;
            reg.EmergencyContactPhone = updated.EmergencyContactPhone;
            reg.StageName             = updated.StageName;
            reg.Price                 = updated.Price;
            reg.ParticipatedInJoreic  = updated.ParticipatedInJoreic;
            reg.PaymentMethod         = updated.PaymentMethod;
            reg.AmountPaid            = updated.AmountPaid;
            reg.AmountPending         = updated.AmountPending;
            reg.Observations          = updated.Observations;
            reg.DietaryRestrictions   = updated.DietaryRestrictions;
            _db.SaveChanges();
            return Ok(reg);
        }

        // ── Delegation directory lookup ─────────────────────────────────────────

        [HttpGet("directory")]
        public IActionResult GetDirectory([FromQuery] string? faculty)
        {
            var info = DelegateDirectory.Lookup(faculty ?? "");
            return Ok(info);
        }

        // ── Tesorería: confirm payment and send credentials email ───────────────

        [HttpPost("{id}/confirm-payment")]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var reg = _db.Registrations.Find(id);
            if (reg == null) return NotFound();
            if (reg.Status == "Paid")
                return BadRequest(new { message = "Este pago ya fue confirmado." });

            reg.Status = "Paid";
            _db.SaveChanges();

            var tempPassword = GeneratePassword();
            CreateUserFromRegistration(reg.Email, tempPassword);

            await _email.SendRegistrationConfirmedAsync(
                toEmail:       reg.Email,
                toName:        $"{reg.Name} {reg.Lastname}",
                paymentDetail: reg.PaymentCondition ?? "Pago completo",
                tempPassword:  tempPassword,
                loginUrl:      LoginUrl);

            return Ok(new { registration = reg, generatedPassword = tempPassword });
        }

        private void CreateUserFromRegistration(string email, string password)
        {
            var exists = _db.Users.AsEnumerable()
                .Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (exists) return;

            var maxId = _db.Users.Any() ? _db.Users.Max(u => u.Id) : 0;
            _db.Users.Add(new Models.User
            {
                Id = maxId + 1,
                Email = email,
                Password = password,
                Role = "assistant",
                MustChangePassword = true,
            });
            _db.SaveChanges();
        }

        // ── File upload → Azure Blob Storage ───────────────────────────────────
        //
        // Parámetros query opcionales:
        //   type        = "certificate" | "comprobante"  (default: comprobante)
        //   dni         = DNI del alumno       (requerido si type=certificate)
        //   apellido    = Apellido del alumno  (requerido si type=certificate)
        //   nombre      = Nombre del alumno    (requerido si type=certificate)
        //   faculty     = Facultad del alumno  (requerido si type=certificate)
        //   delegateEmail = Email del delegado (requerido si type=comprobante)

        [HttpPost("upload")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
        public async Task<IActionResult> UploadFile(
            IFormFile file,
            [FromQuery] string? type = null,
            [FromQuery] string? dni = null,
            [FromQuery] string? apellido = null,
            [FromQuery] string? nombre = null,
            [FromQuery] string? faculty = null,
            [FromQuery] string? delegateEmail = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No se recibió ningún archivo." });

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "application/pdf" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new { message = "Solo se permiten imágenes (JPG, PNG) y PDFs." });

            var ext = Path.GetExtension(file.FileName).ToLower();
            using var stream = file.OpenReadStream();

            string url;

            if (type == "certificate" &&
                !string.IsNullOrWhiteSpace(dni) &&
                !string.IsNullOrWhiteSpace(apellido) &&
                !string.IsNullOrWhiteSpace(nombre) &&
                !string.IsNullOrWhiteSpace(faculty))
            {
                // Certificado de alumno → container "certificados" con nombre descriptivo
                url = await _blob.UploadCertificateAsync(
                    stream, file.ContentType, ext, dni, apellido, nombre, faculty);
            }
            else if (!string.IsNullOrWhiteSpace(delegateEmail))
            {
                // Comprobante de pago → container "comprobantes" por delegado
                url = await _blob.UploadComprobanteAsync(
                    stream, file.ContentType, ext, delegateEmail);
            }
            else
            {
                // Upload genérico (compatibilidad hacia atrás)
                url = await _blob.UploadGenericAsync(stream, file.ContentType, ext);
            }

            return Ok(new { url });
        }

        // ── Delete ──────────────────────────────────────────────────────────────

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var reg = _db.Registrations.Find(id);
            if (reg == null) return NotFound();
            _db.Registrations.Remove(reg);
            _db.SaveChanges();
            return NoContent();
        }

        // ── Excel exports ───────────────────────────────────────────────────────

        [HttpGet("export")]
        public IActionResult ExportAll()
        {
            var registrations = _db.Registrations.ToList();
            var fileBytes = BuildExcel(registrations);
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "inscripciones.xlsx");
        }

        [HttpGet("export/delegation")]
        public IActionResult ExportByDelegation([FromQuery] string name)
        {
            var registrations = _db.Registrations.AsEnumerable()
                .Where(r => string.Equals(r.Faculty, name, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var fileBytes = BuildExcel(registrations);
            var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"inscripciones_{safeName}.xlsx");
        }

        [HttpGet("export/delegate")]
        public IActionResult ExportByDelegate([FromQuery] string email)
        {
            var user = _db.Users.AsEnumerable()
                .FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user == null) return NotFound();

            var faculties = user.ManagedFaculties.Count > 0
                ? user.ManagedFaculties
                : (user.DelegationName != null ? new List<string> { user.DelegationName } : new List<string>());

            var registrations = _db.Registrations.AsEnumerable()
                .Where(r => faculties.Contains(r.Faculty ?? "", StringComparer.OrdinalIgnoreCase))
                .ToList();
            var fileBytes = BuildExcel(registrations);
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"inscripciones_delegado.xlsx");
        }

        private static byte[] BuildExcel(IEnumerable<Registration> registrations)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Inscripciones");

            var headers = new[]
            {
                "ID", "Apellido", "Nombre", "DNI", "Teléfono", "Email", "Delegación",
                "Grupo Sanguíneo", "Afecciones", "Restricciones Alimentarias", "Contacto Emergencia", "Tel. Emergencia",
                "Etapa", "Precio", "Habilitado", "Condición de Pago",
                "Monto Pagado", "Monto Pendiente", "Observaciones", "Fecha Inscripción"
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
                ws.Cell(row, 10).Value = r.DietaryRestrictions ?? "";
                ws.Cell(row, 11).Value = r.EmergencyContactName;
                ws.Cell(row, 12).Value = r.EmergencyContactPhone;
                ws.Cell(row, 13).Value = r.StageName;
                ws.Cell(row, 14).Value = (double)r.Price;
                ws.Cell(row, 15).Value = r.IsEnabled ? "Sí" : "No";
                ws.Cell(row, 16).Value = r.PaymentCondition ?? "Sin asignar";
                ws.Cell(row, 17).Value = (double)r.AmountPaid;
                ws.Cell(row, 18).Value = (double)r.AmountPending;
                ws.Cell(row, 19).Value = r.Observations ?? "";
                ws.Cell(row, 20).Value = r.CreatedAt.ToString("dd/MM/yyyy HH:mm");
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

    public class UpdateAmountsDto
    {
        public decimal AmountPaid { get; set; }
        public decimal AmountPending { get; set; }
    }
}
