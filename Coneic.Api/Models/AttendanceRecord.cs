namespace Coneic.Api.Models;

/// <summary>
/// One confirmed check-in: a registrant present at one attendance session.
/// Unique per (RegistrationId, SessionId) — scanning the same person twice
/// in the same session is idempotent, but they can check into other sessions.
/// </summary>
public class AttendanceRecord
{
    public int Id { get; set; }
    public int RegistrationId { get; set; }
    public int SessionId { get; set; }
    public DateTime CheckedInAt { get; set; } = DateTime.Now;

    /// <summary>Email of the admin who scanned/confirmed this check-in (audit trail).</summary>
    public string? CheckedInBy { get; set; }
}
