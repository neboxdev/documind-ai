namespace DocuMind.Application.DTOs;

public class AIRequestInDTO
{
    public AIRequestInDTO(string systemPrompt, ChatMessage[] messages,
        string? modelId = null, float temperature = 0.7f, int maxTokens = 4096)
    {
        this.SystemPrompt = systemPrompt ?? throw new ArgumentNullException(nameof(systemPrompt));
        this.Messages = messages ?? throw new ArgumentNullException(nameof(messages));
        this.ModelId = modelId;
        this.Temperature = temperature;
        this.MaxTokens = maxTokens;
    }

    public string SystemPrompt { get; }
    public ChatMessage[] Messages { get; }
    public string? ModelId { get; }
    public float Temperature { get; }
    public int MaxTokens { get; }
}

public class ChatMessage
{
    public ChatMessage(string role, string content)
    {
        this.Role = role ?? throw new ArgumentNullException(nameof(role));
        this.Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public string Role { get; }
    public string Content { get; }
}
