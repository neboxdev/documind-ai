using DocuMind.Application.DTOs;
using MediatR;

namespace DocuMind.Application.Features.Conversations.Queries;

public record GetMessagesQuery(Guid ConversationId) : IRequest<MessageOutDTO[]>;
