namespace DocuMind.Application.Interfaces;

public interface ITextChunker
{
    /// <summary>
    /// Splits text into overlapping chunks suitable for RAG context windows.
    /// </summary>
    List<string> ChunkText(string text, int chunkSize = 500, int overlap = 50);
}
