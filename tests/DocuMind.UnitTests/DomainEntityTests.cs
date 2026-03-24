using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using FluentAssertions;

namespace DocuMind.UnitTests;

public class DomainEntityTests
{
    [Fact]
    public void Document_NewInstance_HasPendingStatus()
    {
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "test.pdf",
            ContentType = "application/pdf",
            Size = 1024
        };

        doc.Status.Should().Be(DocumentStatus.Pending);
        doc.UploadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Conversation_NewInstance_HasCorrectDefaults()
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            Title = "Test",
            Provider = AIProvider.Claude,
            ModelId = "claude-sonnet-4-20250514"
        };

        conversation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        conversation.Messages.Should().BeEmpty();
    }

    [Fact]
    public void Message_AssistantMessage_CanTrackTokenUsage()
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Role = "assistant",
            Content = "Here is the answer.",
            ModelId = "gpt-4o",
            PromptTokens = 150,
            CompletionTokens = 42
        };

        message.PromptTokens.Should().Be(150);
        message.CompletionTokens.Should().Be(42);
        message.ModelId.Should().Be("gpt-4o");
    }

    [Fact]
    public void Message_UserMessage_HasNullTokenCounts()
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Role = "user",
            Content = "What is this doc about?"
        };

        message.PromptTokens.Should().BeNull();
        message.CompletionTokens.Should().BeNull();
        message.ModelId.Should().BeNull();
    }
}
