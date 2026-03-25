namespace DocuMind.Application.Interfaces;

public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file and returns the URL/path where it was stored.
    /// </summary>
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Deletes a previously uploaded file by its URL/path.
    /// </summary>
    Task DeleteAsync(string blobUrl, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the storage backend is reachable.
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}
