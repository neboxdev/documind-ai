using DocuMind.Application.DTOs;
using MediatR;

namespace DocuMind.Application.Features.Documents.Commands;

public record UploadDocumentCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long Size) : IRequest<DocumentDto>;
