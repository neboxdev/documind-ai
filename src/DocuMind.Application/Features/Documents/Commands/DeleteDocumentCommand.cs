using MediatR;

namespace DocuMind.Application.Features.Documents.Commands;

public record DeleteDocumentCommand(Guid Id) : IRequest;
