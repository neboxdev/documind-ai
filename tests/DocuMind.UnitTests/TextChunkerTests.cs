using DocuMind.Infrastructure.TextProcessing;
using FluentAssertions;

namespace DocuMind.UnitTests;

public class TextChunkerTests
{
    private readonly TextChunker _chunker = new();

    [Fact]
    public void ChunkText_EmptyString_ReturnsEmptyList()
    {
        var result = _chunker.ChunkText("");
        result.Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_Null_ReturnsEmptyList()
    {
        var result = _chunker.ChunkText(null!);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_ShortText_ReturnsSingleChunk()
    {
        var text = "This is a short piece of text.";
        var result = _chunker.ChunkText(text, chunkSize: 500);

        result.Should().HaveCount(1);
        result[0].Should().Be(text);
    }

    [Fact]
    public void ChunkText_LongText_ReturnsMultipleChunks()
    {
        // Build text that's definitely longer than one chunk
        var sentences = Enumerable.Range(1, 50)
            .Select(i => $"This is sentence number {i} in a rather long document.")
            .ToList();
        var text = string.Join(" ", sentences);

        var result = _chunker.ChunkText(text, chunkSize: 200, overlap: 30);

        result.Should().HaveCountGreaterThan(1);
        // Every chunk should be non-empty
        result.Should().AllSatisfy(chunk => chunk.Should().NotBeNullOrWhiteSpace());
    }

    [Fact]
    public void ChunkText_RespectsApproximateChunkSize()
    {
        var sentences = Enumerable.Range(1, 100)
            .Select(i => $"Sentence {i} has some words in it.")
            .ToList();
        var text = string.Join(" ", sentences);

        var result = _chunker.ChunkText(text, chunkSize: 300, overlap: 40);

        // No chunk should be dramatically larger than the target size
        // Allow some slack for sentence boundary alignment
        result.Should().AllSatisfy(chunk =>
            chunk.Length.Should().BeLessThan(400, "chunks should roughly respect the size limit"));
    }

    [Fact]
    public void ChunkText_WithOverlap_ChunksShareContent()
    {
        var sentences = Enumerable.Range(1, 30)
            .Select(i => $"Word{i}")
            .ToList();
        var text = string.Join(" ", sentences);

        var result = _chunker.ChunkText(text, chunkSize: 50, overlap: 15);

        // With overlap, adjacent chunks should share some content
        if (result.Length >= 2)
        {
            var endOfFirst = result[0][^15..];
            result[1].Should().Contain(endOfFirst.Trim(),
                "overlap should cause shared content between adjacent chunks");
        }
    }

    [Fact]
    public void ChunkText_NormalizesWhitespace()
    {
        var text = "Hello   world.\n\n\nThis   has   extra    spaces.";
        var result = _chunker.ChunkText(text, chunkSize: 500);

        result.Should().HaveCount(1);
        result[0].Should().NotContain("   ", "consecutive spaces should be normalized");
    }
}
