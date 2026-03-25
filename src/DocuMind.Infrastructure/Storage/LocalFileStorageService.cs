using DocuMind.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocuMind.Infrastructure.Storage;

/// <summary>
/// Development fallback: stores uploaded files on the local filesystem.
/// In production, this would be replaced by AzureBlobStorageService.
/// </summary>
public class LocalFileStorageService : IBlobStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(ILogger<LocalFileStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        // Prefix with a GUID to avoid name collisions
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_storagePath, safeName);

        using var output = File.Create(filePath);
        await fileStream.CopyToAsync(output, ct);

        _logger.LogInformation("File stored locally: {Path}", filePath);
        return filePath;
    }

    public Task DeleteAsync(string blobUrl, CancellationToken ct = default)
    {
        if (File.Exists(blobUrl))
        {
            File.Delete(blobUrl);
            _logger.LogInformation("File deleted: {Path}", blobUrl);
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        var healthy = Directory.Exists(_storagePath);
        return Task.FromResult(healthy);
    }
}
