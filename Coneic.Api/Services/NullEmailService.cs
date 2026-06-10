namespace Coneic.Api.Services;

/// <summary>
/// Implementación no-op de IEmailService para entorno de desarrollo local.
/// Solo loggea los emails en lugar de enviarlos.
/// Se registra automáticamente cuando ACS_CONNECTION_STRING no está configurado.
/// </summary>
public class NullEmailService : IEmailService
{
    private readonly ILogger<NullEmailService> _logger;

    public NullEmailService(ILogger<NullEmailService> logger) => _logger = logger;

    public Task SendRegistrationReceivedAsync(RegistrationEmailData data)
    {
        _logger.LogInformation(
            "[DEV EMAIL] RegistrationReceived → {Email} ({Name}) | Delegación: {Delegation}",
            data.ToEmail, data.ToName, data.Delegation?.DelegationName ?? "—");
        return Task.CompletedTask;
    }

    public Task SendRegistrationValidatedAsync(
        string toEmail, string toName, DelegationInfo? delegation, string paymentDeadline)
    {
        _logger.LogInformation(
            "[DEV EMAIL] RegistrationValidated → {Email} ({Name}) | Delegación: {Delegation} | Vence: {Deadline}",
            toEmail, toName, delegation?.DelegationName ?? "—", paymentDeadline);
        return Task.CompletedTask;
    }

    public Task SendRegistrationConfirmedAsync(
        string toEmail, string toName, string paymentDetail, string tempPassword, string loginUrl)
    {
        _logger.LogInformation(
            "[DEV EMAIL] RegistrationConfirmed → {Email} ({Name}) | Password: {Password}",
            toEmail, toName, tempPassword);
        return Task.CompletedTask;
    }

    public Task SendFirstPaymentReceivedAsync(string toEmail, string toName, string dueDate)
    {
        _logger.LogInformation("[DEV EMAIL] FirstPaymentReceived → {Email} ({Name})", toEmail, toName);
        return Task.CompletedTask;
    }
}
