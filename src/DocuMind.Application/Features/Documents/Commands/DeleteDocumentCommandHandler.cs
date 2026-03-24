using DocuMind.Application.Interfaces;
using MediatR;

namespace DocuMind.Application.Features.Documents.Commands;

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IDocumentRepository _documents;

    public DeleteDocumentCommandHandler(IDocumentRepository documents)
    {
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
    }

    public async Task Handle(DeleteDocumentCommand request, CancellationToken ct)
    {
        var document = await _documents.GetByIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException($"Document with ID '{request.Id}' was not found.");

        _documents.Remove(document);
        await _documents.SaveChangesAsync(ct);
    }
}
