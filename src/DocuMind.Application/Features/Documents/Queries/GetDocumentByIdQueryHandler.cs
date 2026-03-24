using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using MediatR;

namespace DocuMind.Application.Features.Documents.Queries;

public class GetDocumentByIdQueryHandler : IRequestHandler<GetDocumentByIdQuery, DocumentDto>
{
    private readonly IDocumentRepository _documents;

    public GetDocumentByIdQueryHandler(IDocumentRepository documents)
    {
        _documents = documents;
    }

    public async Task<DocumentDto> Handle(GetDocumentByIdQuery request, CancellationToken ct)
    {
        var doc = await _documents.GetByIdWithChunksAsync(request.Id, ct)
            ?? throw new KeyNotFoundException($"Document with ID '{request.Id}' was not found.");

        return new DocumentDto(
            doc.Id, doc.FileName, doc.ContentType, doc.Size,
            doc.Status, doc.UploadedAt, doc.Chunks.Count);
    }
}
