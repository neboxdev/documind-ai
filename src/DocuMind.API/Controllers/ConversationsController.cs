using DocuMind.Application.Features.Conversations.Commands;
using DocuMind.Application.Features.Conversations.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.API.Controllers;

[ApiController]
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConversationsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Start a new conversation for a document.
    /// Optionally specify an AI provider; defaults to the configured default.
    /// </summary>
    [HttpPost("api/documents/{documentId:guid}/conversations", Name = nameof(CreateConversation))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateConversation(
        Guid documentId,
        [FromBody] CreateConversationInDTO body,
        CancellationToken ct)
    {
        var command = new CreateConversationCommand(documentId, body.Title, body.Provider);
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetConversations), new { documentId }, result);
    }

    /// <summary>
    /// List all conversations for a document.
    /// </summary>
    [HttpGet("api/documents/{documentId:guid}/conversations", Name = nameof(GetConversations))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversations(Guid documentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetConversationsQuery(documentId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Send a message to a conversation and receive an AI response.
    /// The response includes the assistant's answer with token usage metadata.
    /// </summary>
    [HttpPost("api/conversations/{conversationId:guid}/messages", Name = nameof(SendMessage))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageInDTO body,
        CancellationToken ct)
    {
        var command = new SendMessageCommand(conversationId, body.Content);
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetMessages), new { conversationId }, result);
    }

    /// <summary>
    /// Get the full message history for a conversation.
    /// </summary>
    [HttpGet("api/conversations/{conversationId:guid}/messages", Name = nameof(GetMessages))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(Guid conversationId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMessagesQuery(conversationId), ct);
        return Ok(result);
    }
}

// Endpoint-specific InDTOs kept near the controller that uses them
public class CreateConversationInDTO
{
    public string Title { get; set; } = string.Empty;
    public Domain.Enums.AIProvider? Provider { get; set; }
}

public class SendMessageInDTO
{
    public string Content { get; set; } = string.Empty;
}
