using System.Text.Json;
using Coneic.Api.Models;

namespace Coneic.Api.Data
{
    /// <summary>
    /// In-memory data store seeded from data.json.
    /// Replaces EF Core for Users and Registrations.
    /// Changes are persisted back to disk and survive process restarts,
    /// but the committed data.json is the authoritative seed on each fresh deploy.
    /// </summary>
    public class JsonDataStore
    {
        private readonly string _filePath;
        private readonly List<User> _users;
        private readonly List<Registration> _registrations;
        private int _nextRegistrationId;
        private readonly object _lock = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        public JsonDataStore(IWebHostEnvironment env)
        {
            // Look for data.json next to the executable first, then fall back to content root
            var baseDir = AppContext.BaseDirectory;
            var contentRoot = env.ContentRootPath;

            _filePath = File.Exists(Path.Combine(baseDir, "data.json"))
                ? Path.Combine(baseDir, "data.json")
                : Path.Combine(contentRoot, "data.json");

            if (!File.Exists(_filePath))
                throw new FileNotFoundException($"data.json not found. Searched: {baseDir} and {contentRoot}");

            var json = File.ReadAllText(_filePath);
            var root = JsonSerializer.Deserialize<JsonDataRoot>(json, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize data.json");

            _users = root.Users;
            _registrations = root.Registrations;
            _nextRegistrationId = _registrations.Count > 0
                ? _registrations.Max(r => r.Id) + 1
                : 1;
        }

        // ── Users ──────────────────────────────────────────────────────────────

        public User? FindUser(string email, string password)
            => _users.FirstOrDefault(u =>
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)
                && u.Password == password);

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

        public Registration? GetRegistrationById(int id)
        {
            lock (_lock) { return _registrations.FirstOrDefault(r => r.Id == id); }
        }

        public Registration AddRegistration(Registration reg)
        {
            lock (_lock)
            {
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

        // ── Persistence ────────────────────────────────────────────────────────

        private void Persist()
        {
            try
            {
                var root = new JsonDataRoot { Users = _users, Registrations = _registrations };
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
    }
}
