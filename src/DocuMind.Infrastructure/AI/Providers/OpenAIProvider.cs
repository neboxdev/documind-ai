using DocuMind.Application.DTOs;
using DocuMind.Application.Exceptions;
using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using AIProviderEnum = DocuMind.Domain.Enums.AIProvider;

// Alias to avoid collision with our own ChatMessage DTO
using OpenAIChatMessage = OpenAI.Chat.ChatMessage;

namespace DocuMind.Infrastructure.AI.Providers;

public class OpenAIProvider : IAIProvider
{
    private readonly AIProviderOptions _options;
    private readonly ILogger<OpenAIProvider> _logger;

    public OpenAIProvider(IOptions<AIProviderOptions> options, ILogger<OpenAIProvider> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AIProviderEnum ProviderType => AIProviderEnum.OpenAI;

    public async Task<AIResponseOutDTO> GenerateResponseAsync(AIRequestInDTO request, CancellationToken ct = default)
    {
        var settings = _options.OpenAI;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new AIProviderException("OpenAI API key is not configured.", AIProviderEnum.OpenAI);

        try
        {
            var modelId = request.ModelId ?? settings.ModelId;
            var client = new ChatClient(modelId, settings.ApiKey);

            var messages = new List<OpenAIChatMessage>();
            messages.Add(new SystemChatMessage(request.SystemPrompt));

            foreach (var msg in request.Messages)
            {
                if (msg.Role == "user")
                    messages.Add(new UserChatMessage(msg.Content));
                else
                    messages.Add(new AssistantChatMessage(msg.Content));
            }

            var options = new ChatCompletionOptions
            {
                Temperature = request.Temperature,
                MaxOutputTokenCount = request.MaxTokens
            };

            var response = await client.CompleteChatAsync(
                (IEnumerable<OpenAIChatMessage>)messages, options, ct);

            var completion = response.Value;
            var responseText = completion.Content?.ToString() ?? string.Empty;

            return new AIResponseOutDTO(
                responseText,
                modelId,
                completion.Usage?.InputTokenCount ?? 0,
                completion.Usage?.OutputTokenCount ?? 0,
                AIProviderEnum.OpenAI);
        }
        catch (AIProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI API call failed");
            throw new AIProviderException(
                $"OpenAI API call failed: {ex.Message}", ex, AIProviderEnum.OpenAI);
        }
    }
}
