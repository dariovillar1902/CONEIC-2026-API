namespace Coneic.Api.Services;

/// <summary>
/// Abstraction for sending transactional emails.
/// Implementations: AcsEmailService (production), NullEmailService (dev/test).
/// </summary>
public interface IEmailService
{
    /// <summary>Envía el email de pre-inscripción al alumno que se registró.</summary>
    Task SendRegistrationReceivedAsync(RegistrationEmailData data);

    /// <summary>Envía el email de inscripción habilitada (delegado habilitó al alumno).</summary>
    Task SendRegistrationValidatedAsync(string toEmail, string toName, DelegationInfo? delegation, string paymentDeadline);

    /// <summary>Envía el email de inscripción confirmada (pago completo).</summary>
    Task SendRegistrationConfirmedAsync(string toEmail, string toName, string paymentDetail, string tempPassword, string loginUrl, decimal amount = 0, string stageName = "");

    /// <summary>Envía el email de primera cuota recibida.</summary>
    Task SendFirstPaymentReceivedAsync(string toEmail, string toName, string dueDate);

    /// <summary>Envía el email de visita técnica confirmada (elección definitiva de actividades).</summary>
    Task SendActivitySelectionConfirmedAsync(string toEmail, string toName, string activityCode, string activityTitle, string pdfUrl);
}

public record RegistrationEmailData(
    string ToEmail,
    string ToName,
    string Faculty,
    DelegationInfo? Delegation
);
