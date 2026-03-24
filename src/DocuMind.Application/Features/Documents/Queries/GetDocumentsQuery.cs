using DocuMind.Application.DTOs;
using MediatR;

namespace DocuMind.Application.Features.Documents.Queries;

public record GetDocumentsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<DocumentDto>>;

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
