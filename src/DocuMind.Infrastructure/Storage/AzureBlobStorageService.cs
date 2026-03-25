using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DocuMind.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocuMind.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage implementation for production use.
/// Falls back gracefully if not configured — the DI layer picks
/// LocalFileStorageService when no connection string is present.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var connectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");

        var containerName = configuration["AzureStorage:ContainerName"] ?? "documents";
        _container = new BlobContainerClient(connectionString, containerName);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobName = $"{Guid.NewGuid():N}/{Path.GetFileName(fileName)}";
        var blobClient = _container.GetBlobClient(blobName);

        var headers = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = headers }, ct);

        _logger.LogInformation("File uploaded to Azure Blob: {BlobUri}", blobClient.Uri);
        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
            return;

        var blobClient = new BlobClient(uri);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);

        _logger.LogInformation("File deleted from Azure Blob: {BlobUri}", blobUrl);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            await _container.ExistsAsync(ct);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
