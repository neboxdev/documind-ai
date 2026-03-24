using DocuMind.Domain.Enums;

namespace DocuMind.Application.DTOs;

public class DocumentOutDTO
{
    public DocumentOutDTO(Guid id, string fileName, string contentType, long size,
        DocumentStatus status, DateTime uploadedAt, int chunkCount)
    {
        this.Id = id;
        this.FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        this.ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        this.Size = size;
        this.Status = status;
        this.UploadedAt = uploadedAt;
        this.ChunkCount = chunkCount;
    }

    public Guid Id { get; }
    public string FileName { get; }
    public string ContentType { get; }
    public long Size { get; }
    public DocumentStatus Status { get; }
    public DateTime UploadedAt { get; }
    public int ChunkCount { get; }
}
