using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DocuMindDbContext _db;

    public DocumentRepository(DocuMindDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<Document?> GetByIdWithChunksAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Documents
            .Include(d => d.Chunks.OrderBy(c => c.ChunkIndex))
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<Document[]> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Documents
            .OrderByDescending(d => d.UploadedAt)
            .ToArrayAsync(ct);
    }

    public async Task<(Document[] Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Documents
            .Include(d => d.Chunks)
            .OrderByDescending(d => d.UploadedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Document document, CancellationToken ct = default)
    {
        await _db.Documents.AddAsync(document, ct);
    }

    public void Remove(Document document)
    {
        _db.Documents.Remove(document);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
