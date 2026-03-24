using DocuMind.Application.Interfaces;

namespace DocuMind.Infrastructure.TextProcessing;

public class TextChunker : ITextChunker
{
    /// <summary>
    /// Splits text into chunks of approximately <paramref name="chunkSize"/> characters
    /// with <paramref name="overlap"/> characters of overlap between consecutive chunks.
    /// Tries to break at sentence boundaries when possible.
    /// </summary>
    public string[] ChunkText(string text, int chunkSize = 500, int overlap = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        // Normalize whitespace
        text = string.Join(' ', text.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));

        if (text.Length <= chunkSize)
            return [text];

        var chunks = new List<string>();
        var position = 0;

        while (position < text.Length)
        {
            var end = Math.Min(position + chunkSize, text.Length);
            var chunk = text[position..end];

            // If we're not at the end, try to break at the last sentence boundary
            if (end < text.Length)
            {
                var lastPeriod = chunk.LastIndexOf(". ", StringComparison.Ordinal);
                var lastNewline = chunk.LastIndexOf('\n');
                var breakPoint = Math.Max(lastPeriod, lastNewline);

                if (breakPoint > chunkSize / 3) // only break if we keep at least a third
                {
                    chunk = chunk[..(breakPoint + 1)];
                    end = position + breakPoint + 1;
                }
            }

            chunks.Add(chunk.Trim());

            // Move forward, but step back by overlap amount
            position = end - overlap;
            if (position <= chunks.Sum(c => c.Length) - chunks.Last().Length)
                position = end; // safety: don't go backwards
        }

        return chunks.ToArray();
    }
}
