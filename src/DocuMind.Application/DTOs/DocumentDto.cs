using DocuMind.Domain.Enums;

namespace DocuMind.Application.DTOs;

public record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    DocumentStatus Status,
    DateTime UploadedAt,
    int ChunkCount);
