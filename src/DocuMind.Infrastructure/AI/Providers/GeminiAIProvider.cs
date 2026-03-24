using DocuMind.Application.DTOs;
using DocuMind.Application.Exceptions;
using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure.AI.Configuration;
using GenerativeAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AIProviderEnum = DocuMind.Domain.Enums.AIProvider;

namespace DocuMind.Infrastructure.AI.Providers;

public class GeminiAIProvider : IAIProvider
{
    private readonly AIProviderOptions _options;
    private readonly ILogger<GeminiAIProvider> _logger;

    public GeminiAIProvider(IOptions<AIProviderOptions> options, ILogger<GeminiAIProvider> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AIProviderEnum ProviderType => AIProviderEnum.Gemini;

    public async Task<AIResponseOutDTO> GenerateResponseAsync(AIRequestInDTO request, CancellationToken ct = default)
    {
        var settings = _options.Gemini;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new AIProviderException("Gemini API key is not configured.", AIProviderEnum.Gemini);

        try
        {
            var modelId = request.ModelId ?? settings.ModelId;

            var model = new GenerativeModel(
                settings.ApiKey,
                modelId,
                systemInstruction: request.SystemPrompt);

            // Build conversation via Gemini's chat session
            var chat = model.StartChat();

            // Replay history except the last message
            for (var i = 0; i < request.Messages.Length - 1; i++)
            {
                var msg = request.Messages[i];
                if (msg.Role == "user")
                    await chat.GenerateContentAsync(msg.Content, ct);
                // Assistant responses are automatically tracked in chat history
            }

            // Send the final user message
            var lastMessage = request.Messages[^1];
            var response = await chat.GenerateContentAsync(lastMessage.Content, ct);

            var responseText = response.Text ?? string.Empty;

            return new AIResponseOutDTO(
                responseText,
                modelId,
                response.UsageMetadata?.PromptTokenCount ?? 0,
                response.UsageMetadata?.CandidatesTokenCount ?? 0,
                AIProviderEnum.Gemini);
        }
        catch (AIProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API call failed");
            throw new AIProviderException(
                $"Gemini API call failed: {ex.Message}", ex, AIProviderEnum.Gemini);
        }
    }
}
