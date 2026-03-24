using DocuMind.Application.Interfaces;

namespace DocuMind.Infrastructure.TextProcessing;

public class PlainTextExtractor : ITextExtractor
{
    public bool CanHandle(string contentType)
        => contentType is "text/plain";

    public async Task<string> ExtractAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(fileStream);
        var text = await reader.ReadToEndAsync(ct);
        return text.Trim();
    }
}
