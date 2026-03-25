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

    /// <summary>
    /// Simple keyword-based relevance search: splits the query into words,
    /// scores each chunk by how many query words appear in it, and returns
    /// the top N. This is v1 — a proper implementation would use embeddings.
    /// </summary>
    public async Task<DocumentChunk[]> FindRelevantChunksAsync(
        Guid documentId, string query, int maxChunks = 5, CancellationToken ct = default)
    {
        var chunks = await _db.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ToListAsync(ct);

        if (chunks.Count == 0)
            return [];

        var keywords = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 2) // skip tiny words like "a", "is", "of"
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .ToArray();

        if (keywords.Length == 0)
            return chunks.OrderBy(c => c.ChunkIndex).Take(maxChunks).ToArray();

        // Score each chunk by keyword hit count
        var scored = chunks
            .Select(c => new
            {
                Chunk = c,
                Score = keywords.Count(k => c.Content.Contains(k, StringComparison.OrdinalIgnoreCase))
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Chunk.ChunkIndex)
            .Take(maxChunks)
            .Select(x => x.Chunk)
            .ToArray();

        return scored;
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
