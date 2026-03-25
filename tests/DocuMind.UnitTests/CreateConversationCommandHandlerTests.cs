using DocuMind.Application.Exceptions;
using DocuMind.Application.Features.Conversations.Commands;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DocuMind.UnitTests;

public class CreateConversationCommandHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepo = new();
    private readonly Mock<IDocumentRepository> _documentRepo = new();
    private readonly Mock<IAIProviderFactory> _providerFactory = new();
    private readonly CreateConversationCommandHandler _handler;

    private readonly Document _testDocument = new()
    {
        Id = Guid.NewGuid(),
        FileName = "test.pdf",
        ContentType = "application/pdf",
        Size = 1024
    };

    public CreateConversationCommandHandlerTests()
    {
        _documentRepo
            .Setup(r => r.GetByIdAsync(_testDocument.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDocument);

        var mockProvider = new Mock<IAIProvider>();
        _providerFactory
            .Setup(f => f.GetProvider(It.IsAny<AIProvider>()))
            .Returns(mockProvider.Object);
        _providerFactory
            .Setup(f => f.GetDefaultProvider())
            .Returns(AIProvider.Claude);
        _providerFactory
            .Setup(f => f.GetModelIdForProvider(AIProvider.Claude))
            .Returns("claude-sonnet-4-20250514");
        _providerFactory
            .Setup(f => f.GetModelIdForProvider(AIProvider.OpenAI))
            .Returns("gpt-4o");

        _handler = new CreateConversationCommandHandler(
            _conversationRepo.Object,
            _documentRepo.Object,
            _providerFactory.Object);
    }

    [Fact]
    public async Task Handle_WithExplicitProvider_CreatesConversation()
    {
        var command = new CreateConversationCommand(
            _testDocument.Id, "Test Convo", AIProvider.OpenAI);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be("Test Convo");
        result.Provider.Should().Be(AIProvider.OpenAI);
        result.ModelId.Should().Be("gpt-4o");
        result.DocumentId.Should().Be(_testDocument.Id);

        _conversationRepo.Verify(r => r.AddAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()), Times.Once);
        _conversationRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutProvider_UsesDefault()
    {
        var command = new CreateConversationCommand(_testDocument.Id, "Default Provider Convo");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Provider.Should().Be(AIProvider.Claude);
        result.ModelId.Should().Be("claude-sonnet-4-20250514");
    }

    [Fact]
    public async Task Handle_DocumentNotFound_ThrowsKeyNotFound()
    {
        var command = new CreateConversationCommand(Guid.NewGuid(), "Orphan Convo");

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_AnyProviderEnum_CreatesConversationWithoutKeyCheck()
    {
        // Provider key validation is deferred to message-send time,
        // so creating a conversation with any valid enum should succeed
        _providerFactory
            .Setup(f => f.GetModelIdForProvider(AIProvider.Gemini))
            .Returns("gemini-2.5-flash");

        var command = new CreateConversationCommand(
            _testDocument.Id, "Gemini Convo", AIProvider.Gemini);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Provider.Should().Be(AIProvider.Gemini);
        result.ModelId.Should().Be("gemini-2.5-flash");
    }
}
