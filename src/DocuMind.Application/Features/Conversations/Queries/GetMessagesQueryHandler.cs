using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using MediatR;

namespace DocuMind.Application.Features.Conversations.Queries;

public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, MessageOutDTO[]>
{
    private readonly IConversationRepository _conversations;

    public GetMessagesQueryHandler(IConversationRepository conversations)
    {
        _conversations = conversations ?? throw new ArgumentNullException(nameof(conversations));
    }

    public async Task<MessageOutDTO[]> Handle(GetMessagesQuery request, CancellationToken ct)
    {
        var conversation = await _conversations.GetByIdWithMessagesAsync(request.ConversationId, ct)
            ?? throw new KeyNotFoundException(
                $"Conversation with ID '{request.ConversationId}' was not found.");

        return conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageOutDTO(
                m.Id, m.Role, m.Content, m.ModelId,
                m.PromptTokens, m.CompletionTokens, m.CreatedAt))
            .ToArray();
    }
}
