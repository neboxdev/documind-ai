using DocuMind.Application.DTOs;
using MediatR;

namespace DocuMind.Application.Features.Conversations.Queries;

public record GetConversationsQuery(Guid DocumentId) : IRequest<ConversationOutDTO[]>;
