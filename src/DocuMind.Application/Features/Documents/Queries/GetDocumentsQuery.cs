using DocuMind.Application.DTOs;
using MediatR;

namespace DocuMind.Application.Features.Documents.Queries;

public record GetDocumentsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResultOutDTO<DocumentOutDTO>>;

public class PagedResultOutDTO<T>
{
    public PagedResultOutDTO(T[] items, int totalCount, int page, int pageSize)
    {
        this.Items = items ?? throw new ArgumentNullException(nameof(items));
        this.TotalCount = totalCount;
        this.Page = page;
        this.PageSize = pageSize;
    }

    public T[] Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
