using System.Text.Json;
using Coneic.Api.Models;

namespace Coneic.Api.Data
{
    /// <summary>
    /// In-memory data store seeded from data.json.
    /// Replaces EF Core for Users, Registrations, and PaymentBatches.
    /// Changes are persisted back to disk and survive process restarts,
    /// but the committed data.json is the authoritative seed on each fresh deploy.
    /// </summary>
    public class JsonDataStore
    {
        private readonly string _filePath;
        private readonly List<User> _users;
        private readonly List<Registration> _registrations;
        private readonly List<PaymentBatch> _paymentBatches;
        private int _nextRegistrationId;
        private int _nextBatchId;
        private readonly object _lock = new();

        /// <summary>
        /// Allows reading JSON numbers like 33.0 as int32.
        /// PowerShell's ConvertTo-Json serializes integers as doubles (e.g. 5.0),
        /// so this converter makes the store resilient to that quirk.
        /// </summary>
        private sealed class FlexibleInt32Converter : System.Text.Json.Serialization.JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetInt32(out var i)) return i;
                    if (reader.TryGetDouble(out var d)) return (int)d;
                }
                throw new JsonException($"Cannot read value as Int32 (token: {reader.TokenType})");
            }
            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
                => writer.WriteNumberValue(value);
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new FlexibleInt32Converter() },
        };

        public JsonDataStore(IWebHostEnvironment env)
        {
            // DATA_FILE_PATH env var lets production environments (Azure, Render) keep
            // the live data.json outside the deployment directory so it survives redeploys.
            var overridePath = Environment.GetEnvironmentVariable("DATA_FILE_PATH");

            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                // If the override path doesn't exist yet, seed it from the committed data.json
                if (!File.Exists(overridePath))
                {
                    var seedPath = File.Exists(Path.Combine(AppContext.BaseDirectory, "data.json"))
                        ? Path.Combine(AppContext.BaseDirectory, "data.json")
                        : Path.Combine(env.ContentRootPath, "data.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(overridePath)!);
                    File.Copy(seedPath, overridePath);
                }
                _filePath = overridePath;
            }
            else
            {
                // Local dev: look next to the executable, then content root
                var baseDir = AppContext.BaseDirectory;
                var contentRoot = env.ContentRootPath;
                _filePath = File.Exists(Path.Combine(baseDir, "data.json"))
                    ? Path.Combine(baseDir, "data.json")
                    : Path.Combine(contentRoot, "data.json");
            }

            if (!File.Exists(_filePath))
                throw new FileNotFoundException($"data.json not found. Searched path: {_filePath}");

            var json = File.ReadAllText(_filePath);
            var root = JsonSerializer.Deserialize<JsonDataRoot>(json, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize data.json");

            _users = root.Users;
            _registrations = root.Registrations;
            _paymentBatches = root.PaymentBatches;

            _nextRegistrationId = _registrations.Count > 0
                ? _registrations.Max(r => r.Id) + 1
                : 1;
            _nextBatchId = _paymentBatches.Count > 0
                ? _paymentBatches.Max(b => b.Id) + 1
                : 1;

            // One-time migration: ensure role-specific accounts have the correct role
            ApplyRoleMigrations();
        }

        private void ApplyRoleMigrations()
        {
            bool changed = false;
            var roleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tesoreria@coneic2026.com.ar"] = "tesoreria",
            };
            foreach (var user in _users)
            {
                if (roleMap.TryGetValue(user.Email, out var expectedRole) && user.Role != expectedRole)
                {
                    user.Role = expectedRole;
                    changed = true;
                }
            }
            if (changed) Persist();
        }

        // ── Users ──────────────────────────────────────────────────────────────

        public List<User> GetAllUsers()
        {
            lock (_lock) { return _users.ToList(); }
        }

        public User? FindUser(string email, string password)
            => _users.FirstOrDefault(u =>
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)
                && u.Password == password);

        public User? FindUserByEmail(string email)
            => _users.FirstOrDefault(u =>
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Creates an "assistant" user account for a newly registered attendee.
        /// Returns the new user, or null if the email already has an account.
        /// </summary>
        public User? CreateUserFromRegistration(string email, string password)
        {
            lock (_lock)
            {
                if (_users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)))
                    return null;

                var newUser = new User
                {
                    Id = _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1,
                    Email = email,
                    Password = password,
                    Role = "assistant",
                    MustChangePassword = true,
                };

                _users.Add(newUser);
                Persist();
                return newUser;
            }
        }

        /// <summary>
        /// Changes a user's password. Returns false if current password does not match.
        /// </summary>
        public bool ChangePassword(string email, string currentPassword, string newPassword)
        {
            lock (_lock)
            {
                var user = _users.FirstOrDefault(u =>
                    string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)
                    && u.Password == currentPassword);

                if (user == null) return false;

                user.Password = newPassword;
                user.MustChangePassword = false;
                Persist();
                return true;
            }
        }

        // ── Registrations ──────────────────────────────────────────────────────

        public List<Registration> GetAllRegistrations()
        {
            lock (_lock) { return _registrations.ToList(); }
        }

        public List<Registration> GetRegistrationsByFaculty(string faculty)
        {
            lock (_lock)
            {
                return _registrations
                    .Where(r => string.Equals(r.Faculty, faculty, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        /// <summary>Returns registrations for all faculties in the given list (delegate multi-faculty view).</summary>
        public List<Registration> GetRegistrationsByFaculties(IEnumerable<string> faculties)
        {
            lock (_lock)
            {
                var set = new HashSet<string>(faculties, StringComparer.OrdinalIgnoreCase);
                return _registrations
                    .Where(r => set.Contains(r.Faculty ?? string.Empty))
                    .ToList();
            }
        }

        public Registration? GetRegistrationById(int id)
        {
            lock (_lock) { return _registrations.FirstOrDefault(r => r.Id == id); }
        }

        /// <returns>The created registration, or null if the email already exists.</returns>
        public Registration? AddRegistration(Registration reg)
        {
            lock (_lock)
            {
                var duplicate = _registrations.Any(r =>
                    string.Equals(r.Email, reg.Email, StringComparison.OrdinalIgnoreCase));
                if (duplicate) return null;

                reg.Id = _nextRegistrationId++;
                reg.CreatedAt = DateTime.Now;
                reg.Status = "Pending";
                reg.IsEnabled = false;
                _registrations.Add(reg);
                Persist();
            }
            return reg;
        }

        public bool UpdateStatus(int id, string status)
        {
            lock (_lock)
            {
                var reg = _registrations.FirstOrDefault(r => r.Id == id);
                if (reg == null) return false;
                reg.Status = status;
                Persist();
                return true;
            }
        }

        public bool UpdatePayment(int id, bool isEnabled, string? paymentCondition)
        {
            lock (_lock)
            {
                var reg = _registrations.FirstOrDefault(r => r.Id == id);
                if (reg == null) return false;
                reg.IsEnabled = isEnabled;
                reg.PaymentCondition = paymentCondition;
                Persist();
                return true;
            }
        }

        public bool UpdateAmounts(int id, decimal amountPaid, decimal amountPending)
        {
            lock (_lock)
            {
                var reg = _registrations.FirstOrDefault(r => r.Id == id);
                if (reg == null) return false;
                reg.AmountPaid = amountPaid;
                reg.AmountPending = amountPending;
                Persist();
                return true;
            }
        }

        public bool UpdateObservations(int id, string? observations)
        {
            lock (_lock)
            {
                var reg = _registrations.FirstOrDefault(r => r.Id == id);
                if (reg == null) return false;
                reg.Observations = observations;
                Persist();
                return true;
            }
        }

        public bool UpdateRegistration(int id, Registration updated)
        {
            lock (_lock)
            {
                var reg = _registrations.FirstOrDefault(r => r.Id == id);
                if (reg == null) return false;
                // Update all editable fields (preserve Id, CreatedAt, Status, IsEnabled, PaymentCondition)
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
                Persist();
                return true;
            }
        }

        public bool Delete(int id)
        {
            lock (_lock)
            {
                var reg = _registrations.FirstOrDefault(r => r.Id == id);
                if (reg == null) return false;
                _registrations.Remove(reg);
                Persist();
                return true;
            }
        }

        // ── PaymentBatches ─────────────────────────────────────────────────────

        /// <summary>All batches for a specific delegate (by email).</summary>
        public List<PaymentBatch> GetPaymentBatchesByDelegate(string delegateEmail)
        {
            lock (_lock)
            {
                return _paymentBatches
                    .Where(b => string.Equals(b.DelegateEmail, delegateEmail, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(b => b.CreatedAt)
                    .ToList();
            }
        }

        /// <summary>All batches (admin view).</summary>
        public List<PaymentBatch> GetAllPaymentBatches()
        {
            lock (_lock)
            {
                return _paymentBatches.OrderByDescending(b => b.CreatedAt).ToList();
            }
        }

        public PaymentBatch? GetPaymentBatchById(int id)
        {
            lock (_lock) { return _paymentBatches.FirstOrDefault(b => b.Id == id); }
        }

        public PaymentBatch AddPaymentBatch(PaymentBatch batch)
        {
            lock (_lock)
            {
                batch.Id = _nextBatchId++;
                batch.CreatedAt = DateTime.Now;
                _paymentBatches.Add(batch);
                Persist();
            }
            return batch;
        }

        public PaymentBatch? UpdatePaymentBatch(int id, PaymentBatch updated)
        {
            lock (_lock)
            {
                var existing = _paymentBatches.FirstOrDefault(b => b.Id == id);
                if (existing == null) return null;

                existing.ReceiptUrl = updated.ReceiptUrl;
                existing.Description = updated.Description;
                existing.Assignments = updated.Assignments;
                Persist();
                return existing;
            }
        }

        public bool DeletePaymentBatch(int id)
        {
            lock (_lock)
            {
                var batch = _paymentBatches.FirstOrDefault(b => b.Id == id);
                if (batch == null) return false;
                _paymentBatches.Remove(batch);
                Persist();
                return true;
            }
        }

        public PaymentBatch? ValidateBatch(int id)
        {
            lock (_lock)
            {
                var batch = _paymentBatches.FirstOrDefault(b => b.Id == id);
                if (batch == null) return null;
                batch.IsValidated = true;
                batch.ValidatedAt = DateTime.Now;
                Persist();
                return batch;
            }
        }

        // ── Persistence ────────────────────────────────────────────────────────

        private void Persist()
        {
            try
            {
                var root = new JsonDataRoot
                {
                    Users = _users,
                    Registrations = _registrations,
                    PaymentBatches = _paymentBatches,
                };
                var json = JsonSerializer.Serialize(root, JsonOptions);
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Swallow write errors — data is still valid in memory.
                // On ephemeral hosts (Render free tier) writes may not survive redeploy anyway.
            }
        }
    }

    public class JsonDataRoot
    {
        public List<User> Users { get; set; } = new();
        public List<Registration> Registrations { get; set; } = new();
        public List<PaymentBatch> PaymentBatches { get; set; } = new();
    }
}
