using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using MediatR;

namespace DocuMind.Application.Features.Conversations.Queries;

public class GetConversationsQueryHandler
    : IRequestHandler<GetConversationsQuery, ConversationOutDTO[]>
{
    private readonly IConversationRepository _conversations;
    private readonly IDocumentRepository _documents;

    public GetConversationsQueryHandler(
        IConversationRepository conversations,
        IDocumentRepository documents)
    {
        _conversations = conversations ?? throw new ArgumentNullException(nameof(conversations));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
    }

    public async Task<ConversationOutDTO[]> Handle(GetConversationsQuery request, CancellationToken ct)
    {
        // Verify document exists
        _ = await _documents.GetByIdAsync(request.DocumentId, ct)
            ?? throw new KeyNotFoundException($"Document with ID '{request.DocumentId}' was not found.");

        var conversations = await _conversations.GetByDocumentIdAsync(request.DocumentId, ct);

        return conversations
            .Select(c => new ConversationOutDTO(
                c.Id, c.DocumentId, c.Title, c.Provider, c.ModelId, c.CreatedAt))
            .ToArray();
    }
}
