using DocuMind.Application.DTOs;
using DocuMind.Application.Features.Conversations.Commands;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocuMind.UnitTests;

public class SendMessageCommandHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepo = new();
    private readonly Mock<IDocumentRepository> _documentRepo = new();
    private readonly Mock<IAIProviderFactory> _providerFactory = new();
    private readonly Mock<IAIProvider> _aiProvider = new();
    private readonly SendMessageCommandHandler _handler;

    private readonly Conversation _testConversation;

    public SendMessageCommandHandlerTests()
    {
        _testConversation = new Conversation
        {
            Id = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            Title = "Test Conversation",
            Provider = AIProvider.Claude,
            ModelId = "claude-sonnet-4-20250514",
            Messages = new List<Message>()
        };

        _conversationRepo
            .Setup(r => r.GetByIdWithMessagesAsync(_testConversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testConversation);

        _documentRepo
            .Setup(r => r.FindRelevantChunksAsync(
                _testConversation.DocumentId, It.IsAny<string>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new DocumentChunk { Id = Guid.NewGuid(), Content = "Chunk about quarterly revenue.", ChunkIndex = 0 },
                new DocumentChunk { Id = Guid.NewGuid(), Content = "Chunk about team headcount.", ChunkIndex = 1 }
            });

        _aiProvider.Setup(p => p.ProviderType).Returns(AIProvider.Claude);
        _aiProvider
            .Setup(p => p.GenerateResponseAsync(It.IsAny<AIRequestInDTO>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIResponseOutDTO(
                "The quarterly revenue was $10M.",
                "claude-sonnet-4-20250514",
                200, 35, AIProvider.Claude));

        _providerFactory
            .Setup(f => f.GetProvider(AIProvider.Claude))
            .Returns(_aiProvider.Object);

        _handler = new SendMessageCommandHandler(
            _conversationRepo.Object,
            _documentRepo.Object,
            _providerFactory.Object,
            Mock.Of<ILogger<SendMessageCommandHandler>>());
    }

    [Fact]
    public async Task Handle_ValidMessage_ReturnsAssistantResponse()
    {
        var command = new SendMessageCommand(_testConversation.Id, "What was the revenue?");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Role.Should().Be("assistant");
        result.Content.Should().Be("The quarterly revenue was $10M.");
        result.ModelId.Should().Be("claude-sonnet-4-20250514");
        result.PromptTokens.Should().Be(200);
        result.CompletionTokens.Should().Be(35);
    }

    [Fact]
    public async Task Handle_ValidMessage_SavesBothUserAndAssistantMessages()
    {
        var command = new SendMessageCommand(_testConversation.Id, "What was the revenue?");

        await _handler.Handle(command, CancellationToken.None);

        // Should have saved 2 messages to the conversation
        _testConversation.Messages.Should().HaveCount(2);
        _testConversation.Messages.Should().ContainSingle(m => m.Role == "user");
        _testConversation.Messages.Should().ContainSingle(m => m.Role == "assistant");

        _conversationRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BuildsSystemPromptWithChunkContext()
    {
        var command = new SendMessageCommand(_testConversation.Id, "Tell me about revenue.");

        await _handler.Handle(command, CancellationToken.None);

        // Verify the AI provider was called with a system prompt containing chunk content
        _aiProvider.Verify(p => p.GenerateResponseAsync(
            It.Is<AIRequestInDTO>(req =>
                req.SystemPrompt.Contains("quarterly revenue") &&
                req.SystemPrompt.Contains("team headcount") &&
                req.SystemPrompt.Contains("--- CONTEXT ---")),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Handle_IncludesConversationHistoryInRequest()
    {
        // Add a prior exchange to the conversation
        _testConversation.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            Role = "user",
            Content = "Previous question",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        });
        _testConversation.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            Role = "assistant",
            Content = "Previous answer",
            CreatedAt = DateTime.UtcNow.AddMinutes(-4)
        });

        var command = new SendMessageCommand(_testConversation.Id, "Follow-up question");

        await _handler.Handle(command, CancellationToken.None);

        // The AI request should include the history + the new message
        _aiProvider.Verify(p => p.GenerateResponseAsync(
            It.Is<AIRequestInDTO>(req =>
                req.Messages.Length == 3 && // 2 history + 1 new
                req.Messages[0].Content == "Previous question" &&
                req.Messages[1].Content == "Previous answer" &&
                req.Messages[2].Content == "Follow-up question"),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ThrowsKeyNotFound()
    {
        _conversationRepo
            .Setup(r => r.GetByIdWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var command = new SendMessageCommand(Guid.NewGuid(), "Hello?");

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_NoChunksFound_StillCallsProvider()
    {
        _documentRepo
            .Setup(r => r.FindRelevantChunksAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DocumentChunk>());

        var command = new SendMessageCommand(_testConversation.Id, "Anything?");

        var result = await _handler.Handle(command, CancellationToken.None);

        // Should still get a response even with no chunks
        result.Should().NotBeNull();
        _aiProvider.Verify(p => p.GenerateResponseAsync(
            It.Is<AIRequestInDTO>(req =>
                req.SystemPrompt.Contains("No relevant content")),
            It.IsAny<CancellationToken>()));
    }
}
