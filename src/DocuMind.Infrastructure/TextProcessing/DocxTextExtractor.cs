using System.Text;
using DocuMind.Application.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocuMind.Infrastructure.TextProcessing;

public class DocxTextExtractor : ITextExtractor
{
    private static readonly HashSet<string> DocxContentTypes =
    [
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/msword"
    ];

    public bool CanHandle(string contentType)
        => DocxContentTypes.Contains(contentType);

    public Task<string> ExtractAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var doc = WordprocessingDocument.Open(fileStream, false);
        var body = doc.MainDocumentPart?.Document?.Body;

        if (body == null)
            return Task.FromResult(string.Empty);

        var sb = new StringBuilder();

        foreach (var paragraph in body.Elements<Paragraph>())
        {
            var text = paragraph.InnerText;
            if (!string.IsNullOrWhiteSpace(text))
                sb.AppendLine(text);
        }

        return Task.FromResult(sb.ToString().Trim());
    }
}
