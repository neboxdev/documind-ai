using DocuMind.Domain.Enums;

namespace DocuMind.Application.DTOs;

public record AIResponse(
    string Content,
    string ModelId,
    int PromptTokens,
    int CompletionTokens,
    AIProvider Provider);
