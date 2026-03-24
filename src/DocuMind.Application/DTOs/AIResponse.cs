using DocuMind.Domain.Enums;

namespace DocuMind.Application.DTOs;

public class AIResponseOutDTO
{
    public AIResponseOutDTO(string content, string modelId,
        int promptTokens, int completionTokens, AIProvider provider)
    {
        this.Content = content ?? throw new ArgumentNullException(nameof(content));
        this.ModelId = modelId ?? throw new ArgumentNullException(nameof(modelId));
        this.PromptTokens = promptTokens;
        this.CompletionTokens = completionTokens;
        this.Provider = provider;
    }

    public string Content { get; }
    public string ModelId { get; }
    public int PromptTokens { get; }
    public int CompletionTokens { get; }
    public AIProvider Provider { get; }
}
