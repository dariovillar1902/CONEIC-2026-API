using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.RegularExpressions;

namespace Coneic.Api.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;
    private readonly ILogger<BlobStorageService> _logger;

    private readonly string _containerCertificados;
    private readonly string _containerComprobantes;

    public BlobStorageService(IConfiguration config, ILogger<BlobStorageService> logger)
    {
        var connectionString = config["AZURE_STORAGE_CONNECTION_STRING"]
            ?? throw new InvalidOperationException("AZURE_STORAGE_CONNECTION_STRING no está configurado.");
        _client = new BlobServiceClient(connectionString);
        _logger = logger;
        _containerCertificados = config["BLOB_CONTAINER_CERTIFICADOS"] ?? "certificados";
        _containerComprobantes = config["BLOB_CONTAINER_COMPROBANTES"] ?? "comprobantes";
    }

    public async Task<string> UploadCertificateAsync(
        Stream fileStream, string contentType, string extension,
        string dni, string apellido, string nombre, string faculty)
    {
        // ej: certificados/utn-frba/2026-06/12345678-garcia-juan.pdf
        var month     = DateTime.Now.ToString("yyyy-MM");
        var facultySlug = Slugify(faculty);
        var fileName  = $"{Slugify(dni)}-{Slugify(apellido)}-{Slugify(nombre)}{extension}";
        var blobName  = $"{facultySlug}/{month}/{fileName}";

        return await UploadAsync(_containerCertificados, blobName, fileStream, contentType);
    }

    public async Task<string> UploadComprobanteAsync(
        Stream fileStream, string contentType, string extension, string delegateEmail)
    {
        // ej: comprobantes/delegacion-utn-frba/2026-06/abc12345.jpg
        var month        = DateTime.Now.ToString("yyyy-MM");
        var delegateSlug = Slugify(delegateEmail.Split('@')[0]);
        var blobName     = $"{delegateSlug}/{month}/{Guid.NewGuid():N}{extension}";

        return await UploadAsync(_containerComprobantes, blobName, fileStream, contentType);
    }

    public async Task<string> UploadGenericAsync(Stream fileStream, string contentType, string extension)
    {
        var blobName = $"misc/{DateTime.Now:yyyy-MM}/{Guid.NewGuid():N}{extension}";
        return await UploadAsync(_containerComprobantes, blobName, fileStream, contentType);
    }

    // ── Interno ───────────────────────────────────────────────────────────────

    private async Task<string> UploadAsync(
        string container, string blobName, Stream fileStream, string contentType)
    {
        var containerClient = _client.GetBlobContainerClient(container);
        var blobClient      = containerClient.GetBlobClient(blobName);

        var headers = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = headers });

        _logger.LogInformation("Blob subido: {Container}/{BlobName}", container, blobName);
        return blobClient.Uri.ToString();
    }

    public async Task DeleteByUrlAsync(string blobUrl)
    {
        if (string.IsNullOrWhiteSpace(blobUrl)) return;
        var uri = new Uri(blobUrl);
        // path = /{container}/{blobName...}
        var segments = uri.AbsolutePath.TrimStart('/').Split('/', 2);
        if (segments.Length < 2) return;
        var container = segments[0];
        var blobName  = segments[1];
        var blobClient = _client.GetBlobContainerClient(container).GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
        _logger.LogInformation("Blob eliminado: {Container}/{BlobName}", container, blobName);
    }

    /// <summary>Convierte texto a slug ASCII para nombres de archivo seguros.</summary>
    private static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "unknown";

        var normalized = input.Normalize(System.Text.NormalizationForm.FormD);
        var ascii = new System.Text.StringBuilder();
        foreach (var c in normalized)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                ascii.Append(c);
        }

        return Regex.Replace(ascii.ToString().ToLower(), @"[^a-z0-9]+", "-").Trim('-');
    }
}
