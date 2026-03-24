using DocuMind.Application.DTOs;
using MediatR;

namespace DocuMind.Application.Features.Documents.Queries;

public record GetDocumentByIdQuery(Guid Id) : IRequest<DocumentDto>;
