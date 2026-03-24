using Anthropic;
using Anthropic.Models.Messages;
using DocuMind.Application.DTOs;
using DocuMind.Application.Exceptions;
using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AIProviderEnum = DocuMind.Domain.Enums.AIProvider;

namespace DocuMind.Infrastructure.AI.Providers;

public class ClaudeAIProvider : IAIProvider
{
    private readonly AIProviderOptions _options;
    private readonly ILogger<ClaudeAIProvider> _logger;

    public ClaudeAIProvider(IOptions<AIProviderOptions> options, ILogger<ClaudeAIProvider> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AIProviderEnum ProviderType => AIProviderEnum.Claude;

    public async Task<AIResponseOutDTO> GenerateResponseAsync(AIRequestInDTO request, CancellationToken ct = default)
    {
        var settings = _options.Claude;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new AIProviderException("Claude API key is not configured.", AIProviderEnum.Claude);

        try
        {
            var client = new AnthropicClient { ApiKey = settings.ApiKey };
            var modelId = request.ModelId ?? settings.ModelId;

            var messages = request.Messages
                .Select(m => new MessageParam
                {
                    Role = m.Role == "user" ? "user" : "assistant",
                    Content = m.Content
                })
                .ToList();

            var response = await client.Messages.Create(new MessageCreateParams
            {
                Model = modelId,
                MaxTokens = request.MaxTokens,
                System = request.SystemPrompt,
                Messages = messages,
                Temperature = request.Temperature
            }, ct);

            // Extract text from the first text content block
            var responseText = string.Empty;
            foreach (var block in response.Content)
            {
                if (block.TryPickText(out var textBlock))
                {
                    responseText = textBlock.Text;
                    break;
                }
            }

            return new AIResponseOutDTO(
                responseText,
                modelId,
                (int)response.Usage.InputTokens,
                (int)response.Usage.OutputTokens,
                AIProviderEnum.Claude);
        }
        catch (AIProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude API call failed");
            throw new AIProviderException(
                $"Claude API call failed: {ex.Message}", ex, AIProviderEnum.Claude);
        }
    }
}
