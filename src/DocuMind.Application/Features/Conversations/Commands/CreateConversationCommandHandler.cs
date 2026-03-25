using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using MediatR;

namespace DocuMind.Application.Features.Conversations.Commands;

public class CreateConversationCommandHandler
    : IRequestHandler<CreateConversationCommand, ConversationOutDTO>
{
    private readonly IConversationRepository _conversations;
    private readonly IDocumentRepository _documents;
    private readonly IAIProviderFactory _providerFactory;

    public CreateConversationCommandHandler(
        IConversationRepository conversations,
        IDocumentRepository documents,
        IAIProviderFactory providerFactory)
    {
        _conversations = conversations ?? throw new ArgumentNullException(nameof(conversations));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
    }

    public async Task<ConversationOutDTO> Handle(CreateConversationCommand request, CancellationToken ct)
    {
        // Verify the document exists
        var document = await _documents.GetByIdAsync(request.DocumentId, ct)
            ?? throw new KeyNotFoundException($"Document with ID '{request.DocumentId}' was not found.");

        // Resolve provider: use the one specified, or fall back to the configured default.
        // We don't validate the API key here — that happens when a message is actually sent.
        // This allows creating conversations even if the key is added later.
        var providerType = request.Provider ?? _providerFactory.GetDefaultProvider();
        var modelId = _providerFactory.GetModelIdForProvider(providerType);

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            DocumentId = request.DocumentId,
            Title = request.Title,
            Provider = providerType,
            ModelId = modelId
        };

        await _conversations.AddAsync(conversation, ct);
        await _conversations.SaveChangesAsync(ct);

        return new ConversationOutDTO(
            conversation.Id, conversation.DocumentId, conversation.Title,
            conversation.Provider, conversation.ModelId, conversation.CreatedAt);
    }
}
