using DocuMind.Application.DTOs;
using MediatR;

namespace DocuMind.Application.Features.Conversations.Commands;

public record SendMessageCommand(
    Guid ConversationId,
    string Content) : IRequest<MessageOutDTO>;
