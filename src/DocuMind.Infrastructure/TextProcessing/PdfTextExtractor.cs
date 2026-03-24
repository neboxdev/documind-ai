using System.Text;
using DocuMind.Application.Interfaces;
using UglyToad.PdfPig;

namespace DocuMind.Infrastructure.TextProcessing;

public class PdfTextExtractor : ITextExtractor
{
    public bool CanHandle(string contentType)
        => contentType is "application/pdf";

    public Task<string> ExtractAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var document = PdfDocument.Open(fileStream);
        var sb = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            var text = page.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
                sb.AppendLine(); // blank line between pages
            }
        }

        return Task.FromResult(sb.ToString().Trim());
    }
}
