using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using MediatR;

namespace DocuMind.Application.Features.Documents.Queries;

public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQuery, PagedResult<DocumentDto>>
{
    private readonly IDocumentRepository _documents;

    public GetDocumentsQueryHandler(IDocumentRepository documents)
    {
        _documents = documents;
    }

    public async Task<PagedResult<DocumentDto>> Handle(GetDocumentsQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await _documents.GetPagedAsync(request.Page, request.PageSize, ct);

        var dtos = items
            .Select(d => new DocumentDto(
                d.Id, d.FileName, d.ContentType, d.Size,
                d.Status, d.UploadedAt, d.Chunks.Count))
            .ToList();

        return new PagedResult<DocumentDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
