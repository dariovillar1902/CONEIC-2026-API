namespace Coneic.Api.Services;

/// <summary>
/// Plantillas HTML para los emails transaccionales del CONEIC 2026.
/// Cada método devuelve (subject, htmlBody).
/// </summary>
internal static class EmailTemplates
{
    private const string LogoUrl = "https://coneic2026.com.ar/assets/LOGO_H-CONEIC-COLOR-BLANCO.png";
    private const string WebUrl  = "https://coneic2026.com.ar";

    // ── Colores corporativos ───────────────────────────────────────────────────
    private const string ColorDark      = "#0a0a0a";
    private const string ColorGold      = "#b8973e";
    private const string ColorGoldLight = "#d4af5a";
    private const string ColorText      = "#e5e7eb";
    private const string ColorMuted     = "#9ca3af";

    // ── Wrapper HTML compartido ────────────────────────────────────────────────
    private static string Wrap(string title, string bodyContent) => $"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{title}</title>
        </head>
        <body style="margin:0;padding:0;background:{ColorDark};font-family:'Segoe UI',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:{ColorDark};padding:32px 0;">
            <tr>
              <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;">

                  <!-- Header -->
                  <tr>
                    <td style="background:#111111;border-radius:12px 12px 0 0;padding:32px;text-align:center;
                                border-bottom:3px solid {ColorGold};">
                      <img src="{LogoUrl}" alt="CONEIC 2026" height="60"
                           style="height:60px;max-width:280px;object-fit:contain;" />
                      <p style="margin:12px 0 0;color:{ColorMuted};font-size:13px;letter-spacing:2px;
                                text-transform:uppercase;">Congreso Nacional de Estudiantes de Ingeniería Civil</p>
                    </td>
                  </tr>

                  <!-- Body -->
                  <tr>
                    <td style="background:#111111;padding:40px 40px 32px;border-radius:0 0 12px 12px;">
                      {bodyContent}
                    </td>
                  </tr>

                  <!-- Footer -->
                  <tr>
                    <td style="padding:24px 0;text-align:center;">
                      <p style="margin:0;color:{ColorMuted};font-size:12px;">
                        CONEIC 2026 · Ciudad de Buenos Aires<br />
                        <a href="{WebUrl}" style="color:{ColorGold};text-decoration:none;">{WebUrl}</a>
                      </p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string H1(string text) =>
        $"<h1 style='margin:0 0 8px;color:{ColorGold};font-size:26px;font-weight:700;'>{text}</h1>";

    private static string P(string text) =>
        $"<p style='margin:0 0 16px;color:{ColorText};font-size:15px;line-height:1.6;'>{text}</p>";

    private static string InfoBox(string content) =>
        $"""
        <table width="100%" cellpadding="16" cellspacing="0"
               style="background:#1a1a1a;border-left:4px solid {ColorGold};border-radius:0 8px 8px 0;margin:20px 0;">
          <tr><td style="color:{ColorText};font-size:14px;line-height:1.7;">{content}</td></tr>
        </table>
        """;

    private static string WarningBox(string content) =>
        $"""
        <table width="100%" cellpadding="16" cellspacing="0"
               style="background:#1a1200;border-left:4px solid #f59e0b;border-radius:0 8px 8px 0;margin:20px 0;">
          <tr><td style="color:#fde68a;font-size:14px;line-height:1.7;">{content}</td></tr>
        </table>
        """;

    private static string Button(string url, string label) =>
        $"""
        <p style="text-align:center;margin:28px 0 8px;">
          <a href="{url}"
             style="display:inline-block;background:{ColorGold};color:#000000;font-weight:700;
                    font-size:15px;padding:14px 36px;border-radius:8px;text-decoration:none;
                    letter-spacing:0.5px;">
            {label}
          </a>
        </p>
        """;

    private static string Field(string label, string value) =>
        $"<span style='color:{ColorMuted};'>{label}:</span> <strong style='color:{ColorText};'>{value}</strong><br/>";

    // ═══════════════════════════════════════════════════════════════════════════
    // 1. Pre-inscripción recibida
    // ═══════════════════════════════════════════════════════════════════════════
    public static (string Subject, string Html) RegistrationReceived(
        string toName, string faculty, string delegateName, string filialName)
    {
        var subject = "¡Pre-Inscripción recibida! - CONEIC XVIII";
        var body = $"""
            {H1("¡Pre-Inscripción recibida!")}
            {P($"Hola <strong>{toName}</strong>,")}
            {P($"Recibimos tu pre-inscripción al <strong>CONEIC XVIII</strong>. Tu solicitud fue registrada correctamente.")}
            {InfoBox($"""
                {Field("Delegación", faculty)}
                {Field("Delegado/a", delegateName)}
                {Field("Filial", filialName)}
                """)}
            {WarningBox($"""
                <strong>Próximo paso:</strong><br/>
                Tu inscripción aún no está confirmada. Una vez que el/la delegado/a (<strong>{delegateName}</strong>)
                habilite tu inscripción, recibirás un nuevo email con las instrucciones para continuar.
                Ante cualquier consulta, comunicáte directamente con tu delegado/a.
                """)}
            {Button(WebUrl, "Ver el sitio del CONEIC")}
            """;

        return (subject, Wrap("Pre-Inscripción recibida — CONEIC XVIII", body));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 2. Inscripción habilitada (delegado habilitó al alumno)
    // ═══════════════════════════════════════════════════════════════════════════
    public static (string Subject, string Html) RegistrationValidated(string toName)
    {
        var subject = "Inscripción habilitada - CONEIC XVIII";
        var body = $"""
            {H1("¡Tu inscripción fue habilitada!")}
            {P($"Hola <strong>{toName}</strong>,")}
            {P($"¡Buenas noticias! El/la delegado/a de tu facultad habilitó tu inscripción al <strong>CONEIC XVIII</strong>.")}
            {InfoBox("""
                Ya podés proceder con el pago de la inscripción según las instrucciones que te
                indique tu delegado/a. Comunicate con él/ella para coordinar la forma de pago
                y conocer la fecha límite para hacerlo.
                Una vez confirmado el pago, recibirás un email con tu acceso a la plataforma del congreso.
                """)}
            {Button(WebUrl, "Ver el sitio del CONEIC")}
            """;

        return (subject, Wrap("Inscripción habilitada — CONEIC XVIII", body));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 3. Inscripción confirmada (pago completo o 2da cuota — se crea el usuario)
    // ═══════════════════════════════════════════════════════════════════════════
    public static (string Subject, string Html) RegistrationConfirmed(
        string toName, string paymentDetail, string tempPassword, string loginUrl)
    {
        var subject = "Inscripción confirmada - CONEIC XVIII";

        // Diferenciar el mensaje según si es pago completo o 2da cuota
        var isSecondInstallment = paymentDetail == "Pagó 2° Cuota";
        var introText = isSecondInstallment
            ? $"Hola <strong>{toName}</strong>, recibimos el pago de la <strong>segunda cuota</strong> de tu inscripción al <strong>CONEIC XVIII</strong>. ¡Tu inscripción quedó completamente confirmada!"
            : $"Hola <strong>{toName}</strong>, recibimos el pago de tu inscripción al <strong>CONEIC XVIII</strong>. ¡Tu inscripción quedó confirmada!";

        var body = $"""
            {H1("¡Inscripción confirmada!")}
            {P(introText)}
            {InfoBox($"""
                {Field("Estado de pago", paymentDetail)}
                <br/>
                Tu lugar en el CONEIC XVIII está reservado. ¡Nos vemos en Buenos Aires en octubre!
                """)}
            {WarningBox($"""
                <strong>🔐 Acceso al portal del congreso</strong><br/>
                Se creó un usuario para vos con las siguientes credenciales:<br/><br/>
                Usuario: <strong>{toName}</strong><br/>
                Contraseña provisoria: <strong style="font-size:18px;letter-spacing:2px;">{tempPassword}</strong><br/>
                <br/>
                Ingresá con tu email y esta contraseña. Te recomendamos cambiarla en tu primer acceso.
                """)}
            {Button(loginUrl, "Ingresar al portal →")}
            """;

        return (subject, Wrap("Inscripción confirmada — CONEIC XVIII", body));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 4. Primera cuota recibida
    // ═══════════════════════════════════════════════════════════════════════════
    public static (string Subject, string Html) FirstPaymentReceived(string toName, string dueDate)
    {
        var subject = "Primera cuota recibida - CONEIC XVIII";
        var body = $"""
            {H1("Primera cuota recibida")}
            {P($"Hola <strong>{toName}</strong>,")}
            {P($"Recibimos el pago de la <strong>primera cuota</strong> de tu inscripción al <strong>CONEIC XVIII</strong>.")}
            {InfoBox($"""
                {Field("Vencimiento segunda cuota", dueDate)}
                <br/>
                Recordá abonar la segunda cuota antes de la fecha de vencimiento para completar
                y confirmar tu inscripción. <strong>Si no se recibe el pago en esa fecha, tu lugar
                podría ser liberado.</strong><br/><br/>
                Ante cualquier consulta, comunicáte con tu delegado/a.
                """)}
            {Button(WebUrl, "Ver el sitio del CONEIC")}
            """;

        return (subject, Wrap("Primera cuota recibida — CONEIC XVIII", body));
    }
}
