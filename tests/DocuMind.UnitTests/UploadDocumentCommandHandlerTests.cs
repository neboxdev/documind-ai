using System.Text;
using DocuMind.Application.Features.Documents.Commands;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocuMind.UnitTests;

public class UploadDocumentCommandHandlerTests
{
    private readonly Mock<IDocumentRepository> _repoMock = new();
    private readonly Mock<ITextChunker> _chunkerMock = new();
    private readonly Mock<ITextExtractor> _extractorMock = new();
    private readonly UploadDocumentCommandHandler _handler;

    public UploadDocumentCommandHandlerTests()
    {
        _extractorMock.Setup(e => e.CanHandle("text/plain")).Returns(true);
        _extractorMock
            .Setup(e => e.ExtractAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Some extracted text content from the document.");

        _chunkerMock
            .Setup(c => c.ChunkText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(["chunk one", "chunk two", "chunk three"]);

        _handler = new UploadDocumentCommandHandler(
            _repoMock.Object,
            [_extractorMock.Object],
            _chunkerMock.Object,
            Mock.Of<ILogger<UploadDocumentCommandHandler>>());
    }

    [Fact]
    public async Task Handle_ValidFile_CreatesDocumentWithChunks()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));
        var command = new UploadDocumentCommand(stream, "test.txt", "text/plain", 11);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(DocumentStatus.Processed);
        result.ChunkCount.Should().Be(3);
        result.FileName.Should().Be("test.txt");

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoExtractorAvailable_SetsStatusToFailed()
    {
        var stream = new MemoryStream([1, 2, 3]);
        var command = new UploadDocumentCommand(stream, "test.xyz", "application/octet-stream", 3);

        // Handler won't find an extractor for this content type
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(DocumentStatus.Failed);
    }

    [Fact]
    public async Task Handle_EmptyExtraction_SetsStatusToFailed()
    {
        _extractorMock
            .Setup(e => e.ExtractAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        var command = new UploadDocumentCommand(stream, "empty.txt", "text/plain", 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(DocumentStatus.Failed);
    }
}
