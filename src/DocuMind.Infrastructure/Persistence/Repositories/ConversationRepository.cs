using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly DocuMindDbContext _db;

    public ConversationRepository(DocuMindDbContext db)
    {
        _db = db;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Conversation?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<Conversation>> GetByDocumentIdAsync(Guid documentId, CancellationToken ct = default)
    {
        return await _db.Conversations
            .Where(c => c.DocumentId == documentId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Conversation conversation, CancellationToken ct = default)
    {
        await _db.Conversations.AddAsync(conversation, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
