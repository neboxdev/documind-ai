using DocuMind.Application.DTOs;
using DocuMind.Domain.Enums;
using MediatR;

namespace DocuMind.Application.Features.Conversations.Commands;

public record CreateConversationCommand(
    Guid DocumentId,
    string Title,
    AIProvider? Provider = null) : IRequest<ConversationOutDTO>;
