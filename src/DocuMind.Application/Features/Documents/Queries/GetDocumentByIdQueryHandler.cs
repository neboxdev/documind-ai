using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using MediatR;

namespace DocuMind.Application.Features.Documents.Queries;

public class GetDocumentByIdQueryHandler : IRequestHandler<GetDocumentByIdQuery, DocumentOutDTO>
{
    private readonly IDocumentRepository _documents;

    public GetDocumentByIdQueryHandler(IDocumentRepository documents)
    {
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
    }

    public async Task<DocumentOutDTO> Handle(GetDocumentByIdQuery request, CancellationToken ct)
    {
        var doc = await _documents.GetByIdWithChunksAsync(request.Id, ct)
            ?? throw new KeyNotFoundException($"Document with ID '{request.Id}' was not found.");

        return new DocumentOutDTO(
            doc.Id, doc.FileName, doc.ContentType, doc.Size,
            doc.Status, doc.UploadedAt, doc.Chunks.Count);
    }
}
