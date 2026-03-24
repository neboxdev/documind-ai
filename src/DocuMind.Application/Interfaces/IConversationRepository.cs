using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Conversation?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default);
    Task<Conversation[]> GetByDocumentIdAsync(Guid documentId, CancellationToken ct = default);
    Task AddAsync(Conversation conversation, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
