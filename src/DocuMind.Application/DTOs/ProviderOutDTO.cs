namespace DocuMind.Application.DTOs;

public class ProviderOutDTO
{
    public ProviderOutDTO(string name, string modelId, bool isDefault)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this.ModelId = modelId ?? throw new ArgumentNullException(nameof(modelId));
        this.IsDefault = isDefault;
    }

    public string Name { get; }
    public string ModelId { get; }
    public bool IsDefault { get; }
}
