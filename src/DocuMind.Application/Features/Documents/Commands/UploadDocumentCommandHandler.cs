using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocuMind.Application.Features.Documents.Commands;

public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    private readonly IDocumentRepository _documents;
    private readonly IEnumerable<ITextExtractor> _extractors;
    private readonly ITextChunker _chunker;
    private readonly ILogger<UploadDocumentCommandHandler> _logger;

    public UploadDocumentCommandHandler(
        IDocumentRepository documents,
        IEnumerable<ITextExtractor> extractors,
        ITextChunker chunker,
        ILogger<UploadDocumentCommandHandler> logger)
    {
        _documents = documents;
        _extractors = extractors;
        _chunker = chunker;
        _logger = logger;
    }

    public async Task<DocumentDto> Handle(UploadDocumentCommand request, CancellationToken ct)
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = request.FileName,
            ContentType = request.ContentType,
            Size = request.Size,
            Status = DocumentStatus.Processing
        };

        try
        {
            var extractor = _extractors.FirstOrDefault(e => e.CanHandle(request.ContentType))
                ?? throw new InvalidOperationException(
                    $"No text extractor available for content type '{request.ContentType}'");

            var text = await extractor.ExtractAsync(request.FileStream, ct);

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("No text extracted from {FileName}", request.FileName);
                document.Status = DocumentStatus.Failed;
                await _documents.AddAsync(document, ct);
                await _documents.SaveChangesAsync(ct);
                return ToDto(document, 0);
            }

            var chunks = _chunker.ChunkText(text);

            for (var i = 0; i < chunks.Count; i++)
            {
                document.Chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    Content = chunks[i],
                    ChunkIndex = i
                });
            }

            document.Status = DocumentStatus.Processed;

            // Single save: inserts document + all chunks in one transaction
            await _documents.AddAsync(document, ct);
            await _documents.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Processed {FileName}: {ChunkCount} chunks created",
                request.FileName, chunks.Count);

            return ToDto(document, chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {FileName}", request.FileName);

            document.Status = DocumentStatus.Failed;
            document.Chunks.Clear();
            await _documents.AddAsync(document, ct);
            await _documents.SaveChangesAsync(ct);

            return ToDto(document, 0);
        }
    }

    private static DocumentDto ToDto(Document doc, int chunkCount)
        => new(doc.Id, doc.FileName, doc.ContentType, doc.Size,
               doc.Status, doc.UploadedAt, chunkCount);
}
