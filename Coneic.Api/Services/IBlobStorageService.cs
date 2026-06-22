namespace Coneic.Api.Services;

public interface IBlobStorageService
{
    /// <summary>
    /// Sube un certificado de alumno regular.
    /// Nombre de blob: certificados/{facultad-slug}/{dni}-{apellido}-{nombre}.{ext}
    /// </summary>
    Task<string> UploadCertificateAsync(
        Stream fileStream,
        string contentType,
        string extension,
        string dni,
        string apellido,
        string nombre,
        string faculty);

    /// <summary>
    /// Sube un comprobante de pago de delegado.
    /// Nombre de blob: comprobantes/{delegate-slug}/{fecha}-{guid}.{ext}
    /// </summary>
    Task<string> UploadComprobanteAsync(
        Stream fileStream,
        string contentType,
        string extension,
        string delegateEmail);

    /// <summary>Upload genérico (legado) — devuelve la URL pública del blob.</summary>
    Task<string> UploadGenericAsync(Stream fileStream, string contentType, string extension);

    /// <summary>Elimina un blob por su URL pública. No falla si no existe.</summary>
    Task DeleteByUrlAsync(string blobUrl);
}
