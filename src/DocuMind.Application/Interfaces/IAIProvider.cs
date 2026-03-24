using DocuMind.Application.DTOs;
using DocuMind.Domain.Enums;

namespace DocuMind.Application.Interfaces;

/// <summary>
/// Each AI provider (Claude, OpenAI, Gemini) implements this to normalize
/// the request/response cycle behind a single contract.
/// </summary>
public interface IAIProvider
{
    AIProvider ProviderType { get; }
    Task<AIResponseOutDTO> GenerateResponseAsync(AIRequestInDTO request, CancellationToken ct = default);
}
