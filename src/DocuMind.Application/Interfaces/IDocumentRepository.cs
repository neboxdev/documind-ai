using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Document?> GetByIdWithChunksAsync(Guid id, CancellationToken ct = default);
    Task<List<Document>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Document document, CancellationToken ct = default);
    void Remove(Document document);
    Task SaveChangesAsync(CancellationToken ct = default);
}
