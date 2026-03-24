namespace DocuMind.Application.DTOs;

public class MessageOutDTO
{
    public MessageOutDTO(Guid id, string role, string content,
        string? modelId, int? promptTokens, int? completionTokens, DateTime createdAt)
    {
        this.Id = id;
        this.Role = role ?? throw new ArgumentNullException(nameof(role));
        this.Content = content ?? throw new ArgumentNullException(nameof(content));
        this.ModelId = modelId;
        this.PromptTokens = promptTokens;
        this.CompletionTokens = completionTokens;
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public string Role { get; }
    public string Content { get; }
    public string? ModelId { get; }
    public int? PromptTokens { get; }
    public int? CompletionTokens { get; }
    public DateTime CreatedAt { get; }
}
