using DocuMind.Domain.Enums;

namespace DocuMind.Application.DTOs;

public class ConversationOutDTO
{
    public ConversationOutDTO(Guid id, Guid documentId, string title,
        AIProvider provider, string modelId, DateTime createdAt)
    {
        this.Id = id;
        this.DocumentId = documentId;
        this.Title = title ?? throw new ArgumentNullException(nameof(title));
        this.Provider = provider;
        this.ModelId = modelId ?? throw new ArgumentNullException(nameof(modelId));
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public Guid DocumentId { get; }
    public string Title { get; }
    public AIProvider Provider { get; }
    public string ModelId { get; }
    public DateTime CreatedAt { get; }
}
