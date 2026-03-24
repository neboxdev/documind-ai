namespace DocuMind.Application.Interfaces;

public interface ITextExtractor
{
    bool CanHandle(string contentType);
    Task<string> ExtractAsync(Stream fileStream, CancellationToken ct = default);
}
