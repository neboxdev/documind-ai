namespace DocuMind.Application.DTOs;

public record MessageDto(
    Guid Id,
    string Role,
    string Content,
    string? ModelId,
    int? PromptTokens,
    int? CompletionTokens,
    DateTime CreatedAt);
