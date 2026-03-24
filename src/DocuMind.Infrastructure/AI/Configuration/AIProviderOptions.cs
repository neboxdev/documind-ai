namespace DocuMind.Infrastructure.AI.Configuration;

public class AIProviderOptions
{
    public const string SectionName = "AIProviders";

    public string DefaultProvider { get; set; } = "Claude";
    public ProviderSettings Claude { get; set; } = new();
    public ProviderSettings OpenAI { get; set; } = new();
    public ProviderSettings Gemini { get; set; } = new();
}

public class ProviderSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
}
