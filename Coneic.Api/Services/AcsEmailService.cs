using Azure;
using Azure.Communication.Email;

namespace Coneic.Api.Services;

/// <summary>
/// Implementación de IEmailService usando Azure Communication Services.
/// Requiere la variable de entorno ACS_CONNECTION_STRING.
/// </summary>
public class AcsEmailService : IEmailService
{
    private readonly EmailClient _client;
    private readonly ILogger<AcsEmailService> _logger;

    private const string SenderAddress = "noreply@mail.coneic2026.com.ar";
    private const string SenderName    = "CONEIC 2026";
    private const string LoginUrl      = "https://coneic2026.com.ar/login";

    public AcsEmailService(IConfiguration config, ILogger<AcsEmailService> logger)
    {
        var connectionString = config["ACS_CONNECTION_STRING"]
            ?? throw new InvalidOperationException("ACS_CONNECTION_STRING no está configurado.");
        _client = new EmailClient(connectionString);
        _logger = logger;
    }

    public async Task SendRegistrationReceivedAsync(RegistrationEmailData data)
    {
        var (subject, html) = EmailTemplates.RegistrationReceived(
            data.ToName, data.Faculty, data.Delegation);
        await SendAsync(data.ToEmail, data.ToName, subject, html);
    }

    public async Task SendRegistrationValidatedAsync(
        string toEmail, string toName, DelegationInfo? delegation, string paymentDeadline)
    {
        var (subject, html) = EmailTemplates.RegistrationValidated(toName, delegation, paymentDeadline);
        await SendAsync(toEmail, toName, subject, html);
    }

    public async Task SendRegistrationConfirmedAsync(
        string toEmail, string toName, string paymentDetail, string tempPassword, string loginUrl,
        decimal amount = 0, string stageName = "")
    {
        var (subject, html) = EmailTemplates.RegistrationConfirmed(
            toName, toEmail, paymentDetail, tempPassword, loginUrl, amount, stageName);
        await SendAsync(toEmail, toName, subject, html);
    }

    public async Task SendFirstPaymentReceivedAsync(string toEmail, string toName, string dueDate)
    {
        var (subject, html) = EmailTemplates.FirstPaymentReceived(toName, dueDate);
        await SendAsync(toEmail, toName, subject, html);
    }

    // ── Método interno ─────────────────────────────────────────────────────────

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var message = new EmailMessage(
                senderAddress: SenderAddress,
                recipients: new EmailRecipients(new[]
                {
                    new EmailAddress(toEmail, toName)
                }),
                content: new EmailContent(subject)
                {
                    Html = htmlBody,
                    PlainText = "Abrí este email en un cliente que soporte HTML para verlo correctamente."
                });

            message.Headers.Add("From", $"{SenderName} <{SenderAddress}>");

            var operation = await _client.SendAsync(WaitUntil.Started, message);
            _logger.LogInformation(
                "Email enviado a {Email} | MessageId: {MessageId} | Asunto: {Subject}",
                toEmail, operation.Id, subject);
        }
        catch (Exception ex)
        {
            // Loggear pero no propagar: el email falla silenciosamente
            // para no bloquear la operación principal (registro, pago, etc.)
            _logger.LogError(ex, "Error enviando email a {Email} | Asunto: {Subject}", toEmail, subject);
        }
    }
}
