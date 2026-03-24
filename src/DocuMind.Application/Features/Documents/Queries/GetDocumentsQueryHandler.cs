using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using MediatR;

namespace DocuMind.Application.Features.Documents.Queries;

public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQuery, PagedResultOutDTO<DocumentOutDTO>>
{
    private readonly IDocumentRepository _documents;

    public GetDocumentsQueryHandler(IDocumentRepository documents)
    {
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
    }

    public async Task<PagedResultOutDTO<DocumentOutDTO>> Handle(GetDocumentsQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await _documents.GetPagedAsync(request.Page, request.PageSize, ct);

        var dtos = items
            .Select(d => new DocumentOutDTO(
                d.Id, d.FileName, d.ContentType, d.Size,
                d.Status, d.UploadedAt, d.Chunks.Count))
            .ToArray();

        return new PagedResultOutDTO<DocumentOutDTO>(dtos, totalCount, request.Page, request.PageSize);
    }
}
