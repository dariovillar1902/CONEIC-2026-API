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
    Task SendRegistrationValidatedAsync(string toEmail, string toName,
        string delegateName, string delegateEmail, string filialName);

    /// <summary>Envía el email de inscripción confirmada (pago completo).</summary>
    Task SendRegistrationConfirmedAsync(string toEmail, string toName, string paymentDetail, string tempPassword, string loginUrl);

    /// <summary>Envía el email de primera cuota recibida.</summary>
    Task SendFirstPaymentReceivedAsync(string toEmail, string toName, string dueDate);
}

public record RegistrationEmailData(
    string ToEmail,
    string ToName,
    string Faculty,
    string DelegateName,
    string DelegateEmail,
    string FilialName,
    string WebUrl
);
