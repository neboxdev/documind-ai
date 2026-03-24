namespace DocuMind.Application.DTOs;

public record AIRequest(
    string SystemPrompt,
    IReadOnlyList<ChatMessage> Messages,
    string? ModelId = null,
    float Temperature = 0.7f,
    int MaxTokens = 4096);

public record ChatMessage(string Role, string Content);
