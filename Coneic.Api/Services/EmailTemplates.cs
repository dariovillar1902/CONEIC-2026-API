namespace Coneic.Api.Services;

/// <summary>
/// Plantillas HTML para los emails transaccionales del CONEIC 2026.
/// Cada método devuelve (subject, htmlBody).
/// Diseño: tarjeta blanca sobre fondo gris, header azul institucional, logo y acentos dorados.
/// </summary>
internal static class EmailTemplates
{
    // ── Colores corporativos (manual de marca CONEIC) ─────────────────────────
    private const string BgPage    = "#F5F5F5";
    private const string BgCard    = "#ffffff";
    private const string BgHeader  = "#002F51";   // Azul institucional
    private const string ColorGold = "#C2A14A";   // Dorado complementario
    private const string ColorBlue = "#86C2DE";   // Celeste complementario
    private const string ColorText = "#333333";
    private const string ColorMuted = "#555555";

    // ── URLs corporativas ─────────────────────────────────────────────────────
    private const string WebUrl       = "https://coneic2026.com.ar";
    private const string LoginUrl     = "https://coneic2026.com.ar/login";
    private const string InstagramUrl = "https://www.instagram.com/coneicxviii";
    private const string LinkedInUrl  = "https://www.linkedin.com/company/coneic2026";
    private const string LogoUrl      = "https://coneic2026.com.ar/assets/LOGO_H-CONEIC-COLOR-BLANCO.png";

    // ═══════════════════════════════════════════════════════════════════════════
    // WRAPPER HTML: estructura base de la tarjeta
    // ═══════════════════════════════════════════════════════════════════════════
    private static string Wrap(string title, string statusBannerRow, string bodyContent) => $"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{title}</title>
        </head>
        <body style="margin:0;padding:0;background-color:{BgPage};font-family:Arial,Helvetica,sans-serif;">

          <table width="100%" cellpadding="0" cellspacing="0" role="presentation"
                 style="background-color:{BgPage};padding:32px 16px;">
            <tr>
              <td align="center">

                <table width="600" cellpadding="0" cellspacing="0" role="presentation"
                       style="max-width:600px;width:100%;background-color:{BgCard};
                              border-radius:10px;overflow:hidden;
                              box-shadow:0 2px 12px rgba(0,0,0,0.10);">

                  <!-- HEADER -->
                  <tr>
                    <td style="background-color:{BgHeader};padding:32px 40px 28px;text-align:center;">
                      <img src="{LogoUrl}" alt="CONEIC XVIII" height="56"
                           style="height:56px;max-width:260px;object-fit:contain;
                                  display:block;margin:0 auto 16px;" />
                      <p style="margin:0 0 4px;color:{ColorGold};font-size:11px;font-weight:bold;
                                letter-spacing:3px;text-transform:uppercase;">
                        Congreso Nacional de Estudiantes de Ingeniería Civil
                      </p>
                      <p style="margin:6px 0 0;color:{ColorBlue};font-size:12px;letter-spacing:1px;">
                        Ciudad de Buenos Aires · Octubre 2026
                      </p>
                    </td>
                  </tr>

                  {statusBannerRow}

                  <!-- CUERPO -->
                  <tr>
                    <td style="padding:40px 48px;">
                      {bodyContent}
                    </td>
                  </tr>

                  <!-- SEPARADOR -->
                  <tr>
                    <td style="padding:0 48px;">
                      <hr style="border:none;border-top:1px solid #e8e8e8;margin:0;" />
                    </td>
                  </tr>

                  <!-- FIRMA -->
                  <tr>
                    <td style="padding:28px 48px;">
                      <p style="margin:0 0 4px;font-size:14px;color:{BgHeader};font-weight:bold;">Saludos,</p>
                      <p style="margin:0 0 2px;font-size:14px;color:{BgHeader};">Comité Organizador</p>
                      <p style="margin:0;font-size:14px;color:{BgHeader};font-weight:bold;">CONEIC XVIII</p>
                    </td>
                  </tr>

                  <!-- FOOTER -->
                  <tr>
                    <td style="background-color:{BgHeader};padding:24px 40px;text-align:center;">
                      <p style="margin:0 0 6px;color:{ColorGold};font-size:12px;font-weight:bold;">
                        Comité Organizador · CONEIC XVIII
                      </p>
                      <p style="margin:0 0 8px;color:{ColorBlue};font-size:11px;">
                        Ciudad de Buenos Aires, Argentina · 2026
                      </p>
                      <p style="margin:0;font-size:11px;">
                        <a href="{InstagramUrl}" style="color:{ColorGold};text-decoration:none;">Instagram</a>
                        &nbsp;&nbsp;·&nbsp;&nbsp;
                        <a href="{LinkedInUrl}" style="color:{ColorGold};text-decoration:none;">LinkedIn</a>
                        &nbsp;&nbsp;·&nbsp;&nbsp;
                        <a href="{WebUrl}" style="color:{ColorGold};text-decoration:none;">coneic2026.com.ar</a>
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

    // ── Helpers de bloques ────────────────────────────────────────────────────

    /// Banner de estado coloreado (fila de tabla, va entre header y cuerpo).
    private static string StatusBanner(string bgColor, string textColor, string text) => $"""
        <tr>
          <td style="background-color:{bgColor};padding:14px 48px;text-align:center;">
            <p style="margin:0;font-size:14px;font-weight:bold;color:{textColor};
                      letter-spacing:1px;text-transform:uppercase;">{text}</p>
          </td>
        </tr>
        """;

    /// Caja dorada (datos de contacto / próximo paso).
    private static string GoldBox(string content) => $"""
        <table width="100%" cellpadding="0" cellspacing="0" role="presentation"
               style="margin:20px 0;border-left:4px solid {ColorGold};
                      background-color:#F9F7F2;border-radius:0 6px 6px 0;">
          <tr><td style="padding:16px 20px;">{content}</td></tr>
        </table>
        """;

    /// Caja celeste (información/aviso neutro).
    private static string BlueBox(string content) => $"""
        <table width="100%" cellpadding="0" cellspacing="0" role="presentation"
               style="margin:20px 0;border-left:4px solid {ColorBlue};
                      background-color:#EEF7FB;border-radius:0 6px 6px 0;">
          <tr><td style="padding:16px 20px;">{content}</td></tr>
        </table>
        """;

    /// Caja de advertencia amarilla.
    private static string WarningBox(string content) => $"""
        <table width="100%" cellpadding="0" cellspacing="0" role="presentation"
               style="margin:20px 0;border-left:4px solid {ColorGold};
                      background-color:#FDF6E3;border-radius:0 6px 6px 0;">
          <tr><td style="padding:16px 20px;">{content}</td></tr>
        </table>
        """;

    /// Caja de credenciales del portal (con borde dorado).
    private static string CredentialsBox(string userEmail, string password) => $"""
        <table width="100%" cellpadding="0" cellspacing="0" role="presentation"
               style="margin:20px 0;border:1px solid {ColorGold};border-radius:8px;overflow:hidden;">
          <tr>
            <td style="background-color:{BgHeader};padding:12px 20px;">
              <p style="margin:0;font-size:12px;font-weight:bold;color:{ColorGold};
                        letter-spacing:2px;text-transform:uppercase;">Acceso al portal</p>
            </td>
          </tr>
          <tr>
            <td style="padding:20px;">
              <p style="margin:0 0 8px;font-size:14px;color:{ColorText};">
                Además, ya contás con un usuario en nuestra página web:
              </p>
              <table cellpadding="0" cellspacing="0" role="presentation" style="margin:12px 0;">
                <tr>
                  <td style="padding:4px 16px 4px 0;font-size:13px;color:{ColorMuted};
                             font-weight:bold;white-space:nowrap;">Usuario:</td>
                  <td style="padding:4px 0;font-size:13px;color:{BgHeader};font-weight:bold;">
                    {userEmail}
                  </td>
                </tr>
                <tr>
                  <td style="padding:4px 16px 4px 0;font-size:13px;color:{ColorMuted};
                             font-weight:bold;white-space:nowrap;">Contraseña provisoria:</td>
                  <td style="padding:4px 0;font-size:13px;color:{BgHeader};
                             font-weight:bold;font-family:monospace;letter-spacing:1px;">
                    {password}
                  </td>
                </tr>
              </table>
              <p style="margin:4px 0 16px;font-size:13px;color:{ColorMuted};line-height:1.6;">
                Deberás ingresar por primera vez utilizando esa contraseña para luego poder
                cambiarla por una de tu preferencia.
              </p>
              <a href="{LoginUrl}"
                 style="display:inline-block;background-color:{BgHeader};color:#ffffff;
                        font-size:14px;font-weight:bold;padding:12px 28px;border-radius:6px;
                        text-decoration:none;letter-spacing:0.5px;">
                Ingresar al portal →
              </a>
            </td>
          </tr>
        </table>
        """;

    // ═══════════════════════════════════════════════════════════════════════════
    // 1. Pre-inscripción recibida
    // ═══════════════════════════════════════════════════════════════════════════
    public static (string Subject, string Html) RegistrationReceived(
        string toName, string faculty, string delegateName, string delegateEmail, string filialName)
    {
        var subject = "¡Pre-Inscripción recibida! – CONEIC XVIII";

        var body = $"""
            <p style="margin:0 0 20px;font-size:16px;color:{BgHeader};">
              Hola, <strong>{toName}</strong> 👋
            </p>
            <p style="margin:0 0 16px;font-size:15px;line-height:1.75;color:{ColorText};">
              <strong>¡Muchas gracias por pre-inscribirte al CONEIC XVIII!</strong>
            </p>
            <p style="margin:0 0 16px;font-size:15px;line-height:1.75;color:{ColorText};">
              Recibimos correctamente la información enviada a través del formulario de inscripción.
            </p>
            <p style="margin:0 0 16px;font-size:15px;line-height:1.75;color:{ColorText};">
              El siguiente paso será esperar a que tu delegado/a habilite tu inscripción para poder
              continuar con el proceso. Esta validación se realizará durante el transcurso de la
              próxima semana.
            </p>
            <p style="margin:0 0 8px;font-size:15px;line-height:1.75;color:{ColorText};">
              Ante cualquier duda o consulta, podés comunicarte con tu delegado/a para recibir más
              información y resolver cualquier inquietud.
            </p>
            {GoldBox($"""
              <p style="margin:0 0 8px;font-size:13px;font-weight:bold;color:{BgHeader};">
                Próximo paso: contactate directamente con tu delegado/a o vocal asignado/a
              </p>
              <p style="margin:0 0 4px;font-size:13px;color:{ColorMuted};line-height:1.6;">
                Filial: <strong style="color:{ColorText};">{filialName}</strong>
              </p>
              <p style="margin:0 0 4px;font-size:13px;color:{ColorMuted};line-height:1.6;">
                Contacto: <strong style="color:{ColorText};">{delegateName}</strong>
              </p>
              <p style="margin:0;font-size:13px;color:{ColorMuted};line-height:1.6;">
                <a href="mailto:{delegateEmail}" style="color:{BgHeader};font-weight:bold;
                   text-decoration:underline;">{delegateEmail}</a>
              </p>
            """)}
            <p style="margin:0 0 16px;font-size:15px;line-height:1.75;color:{ColorText};">
              Mientras tanto, podés seguir las novedades del congreso a través de nuestras
              redes sociales
              (<a href="{InstagramUrl}" style="color:{BgHeader};">Instagram</a>
              &nbsp;/&nbsp;
              <a href="{LinkedInUrl}" style="color:{BgHeader};">LinkedIn</a>)
              y <a href="{WebUrl}" style="color:{BgHeader};font-weight:bold;">página web oficial</a>.
            </p>
            <p style="margin:0;font-size:15px;line-height:1.75;color:{BgHeader};font-weight:bold;">
              ¡Estamos muy felices de que formes parte de esta edición!
            </p>
            """;

        return (subject, Wrap("Pre-Inscripción recibida — CONEIC XVIII", "", body));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 2. Inscripción habilitada
    // ═══════════════════════════════════════════════════════════════════════════
    public static (string Subject, string Html) RegistrationValidated(
        string toName, string delegateName, string delegateEmail, string filialName)
    {
        var subject = "Inscripción habilitada – CONEIC XVIII";
        var banner  = StatusBanner("#E8F4EC", "#1a6b35", "✅ &nbsp;Inscripción habilitada");

        var body = $"""
            <p style="margin:0 0 20px;font-size:16px;color:{BgHeader};">
              Hola, <strong>{toName}</strong>
            </p>
            <p style="margin:0 0 16px;font-size:15px;line-height:1.75;color:{ColorText};">
              <strong>¡Tu inscripción al CONEIC XVIII fue habilitada correctamente!</strong>
            </p>
            <p style="margin:0 0 16px;font-size:15px;line-height:1.75;color:{ColorText};">
              El próximo paso es comunicarte con tu delegado/a para coordinar el pago y continuar
              con el proceso de inscripción.
            </p>
            <p style="margin:0 0 8px;font-size:15px;line-height:1.75;color:{ColorText};">
              En caso de no comunicarte dentro del plazo indicado, tu lugar podrá ser liberado
              y reasignado a otra persona en lista de espera.
            </p>
            {GoldBox($"""
              <p style="margin:0 0 8px;font-size:13px;font-weight:bold;color:{BgHeader};">
                Próximo paso: contactate directamente con tu delegado/a o vocal asignado/a
              </p>
              <p style="margin:0 0 4px;font-size:13px;color:{ColorMuted};line-height:1.6;">
                Filial: <strong style="color:{ColorText};">{filialName}</strong>
              </p>
              <p style="margin:0 0 4px;font-size:13px;color:{ColorMuted};line-height:1.6;">
                Contacto: <strong style="color:{ColorText};">{delegateName}</strong>
              </p>
              <p style="margin:0;font-size:13px;color:{ColorMuted};line-height:1.6;">
                <a href="mailto:{delegateEmail}" style="color:{BgHeader};font-weight:bold;
                   text-decoration:underline;">{delegateEmail}</a>
              </p>
            """)}
            {BlueBox($"""
              <p style="margin:0;font-size:13px;color:{ColorMuted};line-height:1.7;">
                Te recordamos que, una vez realizado el pago, la acreditación en nuestro sistema
                puede demorar algunos días. Agradecemos tu paciencia durante este proceso.
              </p>
            """)}
            <p style="margin:0;font-size:15px;line-height:1.75;color:{BgHeader};font-weight:bold;">
              ¡Gracias por sumarte a esta nueva edición del congreso!
            </p>
            """;

        return (subject, Wrap("Inscripción habilitada — CONEIC XVIII", banner, body));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 3. Inscripción confirmada (pago completo o 2da cuota — se crea el usuario)
    // ═══════════════════════════════════════════════════════════════════════════
    public static (string Subject, string Html) RegistrationConfirmed(
        string toName, string toEmail, string paymentDetail, string tempPassword, string loginUrl)
    {
        var subject = "Inscripción confirmada – CONEIC XVIII";
        var banner  = StatusBanner("#E8F4EC", "#1a6b35", "🎉 &nbsp;Inscripción confirmada");

        var isSecondInstallment = paymentDetail == "Pagó 2° Cuota";
        var paymentDesc = isSecondInstallment ? "la segunda cuota" : "el pago completo";

        var body = $"""
            <p style="margin:0 0 20px;font-size:16px;color:{BgHeader};">
              Hola, <strong>{toName}</strong>
            </p>
            <p style="margin:0 0 16px;font-size:15px;line-height:1.75;color:{ColorText};">
              <strong>¡Tu inscripción al CONEIC XVIII quedó confirmada correctamente!</strong>
            </p>
            <p style="margin:0 0 16px;font-size:15px;line-height:1.75;color:{ColorText};">
              Recibimos y registramos tu pago correspondiente a <strong>{paymentDesc}</strong>
              en nuestro sistema, por lo que ya formás parte oficialmente de esta nueva edición
              del congreso.
            </p>
            {CredentialsBox(toEmail, tempPassword)}
            <p style="margin:0 0 16px;font-size:15px;line-height:1.75;color:{ColorText};">
              Te invitamos a seguir todas las novedades, anuncios e información importante a través
              de nuestra <a href="{WebUrl}" style="color:{BgHeader};font-weight:bold;">página web</a>
              y redes sociales oficiales
              (<a href="{InstagramUrl}" style="color:{BgHeader};">Instagram</a>
              &nbsp;/&nbsp;
              <a href="{LinkedInUrl}" style="color:{BgHeader};">LinkedIn</a>).
            </p>
            <p style="margin:0;font-size:15px;line-height:1.75;color:{BgHeader};font-weight:bold;">
              ¡Muchas gracias por sumarte al CONEIC XVIII!
            </p>
            """;

        return (subject, Wrap("Inscripción confirmada — CONEIC XVIII", banner, body));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 4. Primera cuota recibida
    // ═══════════════════════════════════════════════════════════════════════════
    public static (string Subject, string Html) FirstPaymentReceived(string toName, string dueDate)
    {
        var subject = "Primera cuota recibida – CONEIC XVIII";
        var banner  = StatusBanner("#FDF6E3", "#8a6500", "💳 &nbsp;Primera cuota recibida");

        var body = $"""
            <p style="margin:0 0 20px;font-size:16px;color:{BgHeader};">
              Hola, <strong>{toName}</strong>
            </p>
            <p style="margin:0 0 16px;font-size:15px;line-height:1.75;color:{ColorText};">
              <strong>¡Recibimos correctamente tu pago correspondiente a la primera cuota de
              la inscripción al CONEIC XVIII!</strong>
            </p>
            <p style="margin:0 0 16px;font-size:15px;line-height:1.75;color:{ColorText};">
              Te recordamos que, para completar el proceso y confirmar tu inscripción, deberá
              abonarse la segunda cuota antes del día <strong>{dueDate}</strong>.
            </p>
            <p style="margin:0 0 8px;font-size:15px;line-height:1.75;color:{ColorText};">
              Hasta que el segundo pago no sea recibido y registrado en nuestro sistema, la
              inscripción continuará figurando como pendiente de confirmación.
            </p>
            {WarningBox($"""
              <p style="margin:0 0 8px;font-size:13px;font-weight:bold;color:#8a6500;">
                ⚠️ &nbsp;Importante
              </p>
              <p style="margin:0 0 8px;font-size:13px;color:{ColorMuted};line-height:1.7;">
                En caso de no recibir el pago de la segunda cuota, tu lugar será liberado y
                re-asignado al siguiente participante en lista de espera.
              </p>
              <p style="margin:0;font-size:13px;color:{ColorMuted};line-height:1.7;">
                Recordá que los importes abonados en concepto de inscripción
                <strong>no son reembolsables</strong> bajo ninguna circunstancia.
                Esto aplica tanto para pagos parciales como para pagos totales.
              </p>
            """)}
            <p style="margin:0 0 16px;font-size:15px;line-height:1.75;color:{ColorText};">
              Ante cualquier duda, podés comunicarte con tu delegado/a.
            </p>
            <p style="margin:0;font-size:15px;line-height:1.75;color:{ColorText};">
              Te invitamos a seguir todas las novedades a través de nuestra
              <a href="{WebUrl}" style="color:{BgHeader};font-weight:bold;">página web</a>
              y redes sociales oficiales.
            </p>
            """;

        return (subject, Wrap("Primera cuota recibida — CONEIC XVIII", banner, body));
    }
}
