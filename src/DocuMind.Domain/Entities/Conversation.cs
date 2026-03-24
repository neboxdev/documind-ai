using DocuMind.Domain.Enums;

namespace DocuMind.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public AIProvider Provider { get; set; }
    public string ModelId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Document Document { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = [];
}
