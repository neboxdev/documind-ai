using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocuMind.Application.Features.Conversations.Commands;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageOutDTO>
{
    private readonly IConversationRepository _conversations;
    private readonly IDocumentRepository _documents;
    private readonly IAIProviderFactory _providerFactory;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    private const string SystemPromptTemplate =
        """
        You are a helpful assistant that answers questions based ONLY on the provided
        document context. If the answer is not found in the context, say so clearly.
        Do not make up information. Be concise and precise.

        --- CONTEXT ---
        {0}
        --- END CONTEXT ---
        """;

    public SendMessageCommandHandler(
        IConversationRepository conversations,
        IDocumentRepository documents,
        IAIProviderFactory providerFactory,
        ILogger<SendMessageCommandHandler> logger)
    {
        _conversations = conversations ?? throw new ArgumentNullException(nameof(conversations));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MessageOutDTO> Handle(SendMessageCommand request, CancellationToken ct)
    {
        // 1. Load conversation with message history
        var conversation = await _conversations.GetByIdWithMessagesAsync(request.ConversationId, ct)
            ?? throw new KeyNotFoundException(
                $"Conversation with ID '{request.ConversationId}' was not found.");

        // 2. Find top 5 relevant chunks via keyword search
        var relevantChunks = await _documents.FindRelevantChunksAsync(
            conversation.DocumentId, request.Content, maxChunks: 5, ct);

        var contextText = relevantChunks.Length > 0
            ? string.Join("\n\n", relevantChunks.Select(c => c.Content))
            : "(No relevant content found in the document.)";

        // 3. Build system prompt with document context
        var systemPrompt = string.Format(SystemPromptTemplate, contextText);

        // 4. Build message history from existing conversation messages
        var chatHistory = conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessage(m.Role, m.Content))
            .ToList();

        // Add the new user message to the history
        chatHistory.Add(new ChatMessage("user", request.Content));

        // 5. Resolve the AI provider for this conversation
        var provider = _providerFactory.GetProvider(conversation.Provider);

        // 6. Call the AI provider
        var aiRequest = new AIRequestInDTO(
            systemPrompt,
            chatHistory.ToArray());

        var aiResponse = await provider.GenerateResponseAsync(aiRequest, ct);

        // 7. Save the user message
        var userMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "user",
            Content = request.Content
        };

        // 8. Save the assistant message with metadata
        var assistantMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = aiResponse.Content,
            ModelId = aiResponse.ModelId,
            PromptTokens = aiResponse.PromptTokens,
            CompletionTokens = aiResponse.CompletionTokens
        };

        conversation.Messages.Add(userMessage);
        conversation.Messages.Add(assistantMessage);
        await _conversations.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Message processed for conversation {ConversationId}: " +
            "provider={Provider} model={Model} promptTokens={PromptTokens} completionTokens={CompletionTokens}",
            conversation.Id, conversation.Provider, aiResponse.ModelId,
            aiResponse.PromptTokens, aiResponse.CompletionTokens);

        return new MessageOutDTO(
            assistantMessage.Id, assistantMessage.Role, assistantMessage.Content,
            assistantMessage.ModelId, assistantMessage.PromptTokens,
            assistantMessage.CompletionTokens, assistantMessage.CreatedAt);
    }
}
