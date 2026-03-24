using DocuMind.Domain.Enums;

namespace DocuMind.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? BlobUrl { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<DocumentChunk> Chunks { get; set; } = [];
    public ICollection<Conversation> Conversations { get; set; } = [];
}
