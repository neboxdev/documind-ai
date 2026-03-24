using DocuMind.Domain.Enums;

namespace DocuMind.Application.DTOs;

public record ConversationDto(
    Guid Id,
    Guid DocumentId,
    string Title,
    AIProvider Provider,
    string ModelId,
    DateTime CreatedAt);
