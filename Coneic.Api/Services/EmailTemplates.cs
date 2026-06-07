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
                        CONEIC 2026 · San Rafael, Mendoza<br />
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
        var subject = "✅ Pre-inscripción recibida — CONEIC 2026";
        var body = $"""
            {H1("¡Tu pre-inscripción fue recibida!")}
            {P($"Hola <strong>{toName}</strong>, registramos tu pre-inscripción al CONEIC 2026. A continuación están los detalles.")}
            {InfoBox($"""
                {Field("Delegación", faculty)}
                {Field("Delegado/a", delegateName)}
                {Field("Filial", filialName)}
                """)}
            {WarningBox($"""
                <strong>⚠️ Importante — próximo paso</strong><br/>
                Tu inscripción aún no está confirmada. El/la delegado/a de tu facultad
                (<strong>{delegateName}</strong>) va a habilitarte una vez que abra la inscripción
                formal para tu delegación. Vas a recibir otro email cuando eso ocurra.
                """)}
            {P($"Ante cualquier consulta, contactá directamente a tu delegado/a.")}
            {Button(WebUrl, "Ver el sitio del CONEIC")}
            """;

        return (subject, Wrap("Pre-inscripción CONEIC 2026", body));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 2. Inscripción habilitada (delegado habilitó al alumno)
    // ═══════════════════════════════════════════════════════════════════════════
    public static (string Subject, string Html) RegistrationValidated(string toName)
    {
        var subject = "🎉 ¡Tu inscripción al CONEIC 2026 fue habilitada!";
        var body = $"""
            {H1("¡Tu inscripción fue habilitada!")}
            {P($"Hola <strong>{toName}</strong>, el/la delegado/a de tu facultad habilitó tu inscripción al CONEIC 2026.")}
            {InfoBox("""
                Esto significa que ya podés proceder con el pago de la inscripción según las
                instrucciones que te comparta tu delegado/a. Una vez que el pago sea confirmado,
                recibirás un email con tu acceso a la plataforma.
                """)}
            {Button(WebUrl, "Ver el sitio del CONEIC")}
            """;

        return (subject, Wrap("Inscripción habilitada — CONEIC 2026", body));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 3. Inscripción confirmada (pago completo — se crea el usuario)
    // ═══════════════════════════════════════════════════════════════════════════
    public static (string Subject, string Html) RegistrationConfirmed(
        string toName, string paymentDetail, string tempPassword, string loginUrl)
    {
        var subject = "🎊 ¡Inscripción al CONEIC 2026 confirmada!";
        var body = $"""
            {H1("¡Inscripción confirmada!")}
            {P($"Hola <strong>{toName}</strong>, tu pago fue registrado y tu inscripción al CONEIC 2026 está confirmada. ¡Nos vemos en San Rafael!")}
            {InfoBox($"""
                {Field("Condición de pago", paymentDetail)}
                """)}
            {WarningBox($"""
                <strong>🔐 Tus credenciales de acceso</strong><br/>
                Contraseña temporal: <strong style="font-size:18px;letter-spacing:2px;">{tempPassword}</strong><br/>
                <br/>
                Ingresá con tu email y cambiá la contraseña en tu primer acceso.
                """)}
            {Button(loginUrl, "Ingresar a la plataforma")}
            """;

        return (subject, Wrap("Inscripción confirmada — CONEIC 2026", body));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 4. Primera cuota recibida
    // ═══════════════════════════════════════════════════════════════════════════
    public static (string Subject, string Html) FirstPaymentReceived(string toName, string dueDate)
    {
        var subject = "💰 Primera cuota recibida — CONEIC 2026";
        var body = $"""
            {H1("Primera cuota recibida")}
            {P($"Hola <strong>{toName}</strong>, registramos el pago de tu primera cuota para el CONEIC 2026.")}
            {InfoBox($"""
                {Field("Vencimiento segunda cuota", dueDate)}
                <br/>
                Recordá abonar la segunda cuota antes del vencimiento para completar tu inscripción.
                Ante cualquier consulta, contactá a tu delegado/a.
                """)}
            {Button(WebUrl, "Ver el sitio del CONEIC")}
            """;

        return (subject, Wrap("Primera cuota — CONEIC 2026", body));
    }
}
