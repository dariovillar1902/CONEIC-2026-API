using ClosedXML.Excel;
using Coneic.Api.Data;
using Coneic.Api.Models;
using Coneic.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Coneic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationsController : ControllerBase
    {
        private readonly JsonDataStore _store;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _email;
        private readonly IBlobStorageService _blob;

        private const string LoginUrl = "https://coneic2026.com.ar/login";

        public RegistrationsController(
            JsonDataStore store, IWebHostEnvironment env,
            IEmailService email, IBlobStorageService blob)
        {
            _store = store;
            _env   = env;
            _email = email;
            _blob  = blob;
        }

        // ── Create ──────────────────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Registration registration)
        {
            var created = _store.AddRegistration(registration);
            if (created == null)
                return Conflict(new { message = "Ya existe una inscripción con ese email." });

            // Buscar el delegado de la facultad para incluirlo en el email
            var delegateUser = _store.GetAllUsers()
                .FirstOrDefault(u => u.Role == "delegate" &&
                                     (u.ManagedFaculties.Contains(created.Faculty ?? "") ||
                                      u.DelegationName == created.Faculty));
            var delegateName  = delegateUser?.DelegationName ?? "el/la delegado/a de tu facultad";
            var delegateEmail = delegateUser?.Email ?? "";
            var filialName    = delegateUser?.Filial ?? created.Faculty ?? "";

            await _email.SendRegistrationReceivedAsync(new RegistrationEmailData(
                ToEmail:       created.Email,
                ToName:        $"{created.Name} {created.Lastname}",
                Faculty:       created.Faculty ?? "",
                DelegateName:  delegateName,
                DelegateEmail: delegateEmail,
                FilialName:    filialName,
                WebUrl:        "https://coneic2026.com.ar"
            ));

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, new { registration = created });
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
            var reg = _store.GetRegistrationById(id);
            if (reg == null) return NotFound();
            return Ok(reg);
        }

        [HttpGet]
        public ActionResult<IEnumerable<Registration>> GetAll()
            => Ok(_store.GetAllRegistrations());

        [HttpGet("delegation")]
        public ActionResult<IEnumerable<Registration>> GetByDelegation([FromQuery] string name)
            => Ok(_store.GetRegistrationsByFaculty(name));

        /// <summary>Returns all registrations for a delegate's managed faculties (looked up by email).</summary>
        [HttpGet("delegate")]
        public ActionResult<IEnumerable<Registration>> GetByDelegate([FromQuery] string email)
        {
            var user = _store.FindUserByEmail(email);
            if (user == null) return NotFound(new { message = "Delegate not found." });

            var faculties = user.ManagedFaculties.Count > 0
                ? user.ManagedFaculties
                : (user.DelegationName != null ? new List<string> { user.DelegationName } : new List<string>());

            return Ok(_store.GetRegistrationsByFaculties(faculties));
        }

        // ── Update ──────────────────────────────────────────────────────────────

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            if (!_store.UpdateStatus(id, status)) return NotFound();
            var reg = _store.GetRegistrationById(id);

            if (status == "Paid" && reg != null)
            {
                var generatedPassword = GeneratePassword();
                _store.CreateUserFromRegistration(reg.Email, generatedPassword);

                // Email de inscripción confirmada con contraseña temporal
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
            // Capturar estado anterior para saber si cambió a habilitado
            var before = _store.GetRegistrationById(id);
            var wasEnabled = before?.IsEnabled ?? false;

            if (!_store.UpdatePayment(id, dto.IsEnabled, dto.PaymentCondition)) return NotFound();

            var reg = _store.GetRegistrationById(id);

            // Enviar email solo cuando se habilita por primera vez
            if (dto.IsEnabled && !wasEnabled && reg != null)
            {
                var delegateUser = _store.GetAllUsers()
                    .FirstOrDefault(u => u.Role == "delegate" &&
                                         (u.ManagedFaculties.Contains(reg.Faculty ?? "") ||
                                          u.DelegationName == reg.Faculty));

                await _email.SendRegistrationValidatedAsync(
                    toEmail:       reg.Email,
                    toName:        $"{reg.Name} {reg.Lastname}",
                    delegateName:  delegateUser?.DelegationName ?? "el/la delegado/a de tu facultad",
                    delegateEmail: delegateUser?.Email ?? "",
                    filialName:    delegateUser?.Filial ?? reg.Faculty ?? "");
            }

            return Ok(reg);
        }

        [HttpPatch("{id}/amounts")]
        public IActionResult UpdateAmounts(int id, [FromBody] UpdateAmountsDto dto)
        {
            if (!_store.UpdateAmounts(id, dto.AmountPaid, dto.AmountPending)) return NotFound();
            return Ok(_store.GetRegistrationById(id));
        }

        [HttpPatch("{id}/observations")]
        public IActionResult UpdateObservations(int id, [FromBody] string? observations)
        {
            if (!_store.UpdateObservations(id, observations)) return NotFound();
            return Ok(_store.GetRegistrationById(id));
        }

        /// <summary>Full registration update (all editable fields). Used by admin and delegate edit modals.</summary>
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Registration updated)
        {
            if (!_store.UpdateRegistration(id, updated)) return NotFound();
            return Ok(_store.GetRegistrationById(id));
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
            if (!_store.Delete(id)) return NotFound();
            return NoContent();
        }

        // ── Excel exports ───────────────────────────────────────────────────────

        [HttpGet("export")]
        public IActionResult ExportAll()
        {
            var registrations = _store.GetAllRegistrations();
            var fileBytes = BuildExcel(registrations);
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "inscripciones.xlsx");
        }

        [HttpGet("export/delegation")]
        public IActionResult ExportByDelegation([FromQuery] string name)
        {
            var registrations = _store.GetRegistrationsByFaculty(name);
            var fileBytes = BuildExcel(registrations);
            var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"inscripciones_{safeName}.xlsx");
        }

        [HttpGet("export/delegate")]
        public IActionResult ExportByDelegate([FromQuery] string email)
        {
            var user = _store.FindUserByEmail(email);
            if (user == null) return NotFound();

            var faculties = user.ManagedFaculties.Count > 0
                ? user.ManagedFaculties
                : (user.DelegationName != null ? new List<string> { user.DelegationName } : new List<string>());

            var registrations = _store.GetRegistrationsByFaculties(faculties);
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
                "Grupo Sanguíneo", "Afecciones", "Contacto Emergencia", "Tel. Emergencia",
                "Etapa", "Precio", "Estado", "Habilitado", "Condición de Pago",
                "Monto Pagado", "Monto Pendiente", "Método de Pago", "Observaciones", "Fecha Inscripción"
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
                ws.Cell(row, 20).Value = r.Observations ?? "";
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

    public class UpdateAmountsDto
    {
        public decimal AmountPaid { get; set; }
        public decimal AmountPending { get; set; }
    }
}
