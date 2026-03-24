using DocuMind.Application.DTOs;
using DocuMind.Application.Exceptions;
using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AIProviderEnum = DocuMind.Domain.Enums.AIProvider;

namespace DocuMind.Infrastructure.AI.Factory;

public class AIProviderFactory : IAIProviderFactory
{
    private readonly Dictionary<AIProviderEnum, IAIProvider> _providers;
    private readonly AIProviderOptions _options;
    private readonly ILogger<AIProviderFactory> _logger;

    public AIProviderFactory(
        IEnumerable<IAIProvider> providers,
        IOptions<AIProviderOptions> options,
        ILogger<AIProviderFactory> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _providers = (providers ?? throw new ArgumentNullException(nameof(providers)))
            .ToDictionary(p => p.ProviderType);

        LogAvailableProviders();
    }

    public IAIProvider GetProvider(AIProviderEnum providerType)
    {
        if (!_providers.TryGetValue(providerType, out var provider))
            throw new AIProviderException(
                $"AI provider '{providerType}' is not registered.", providerType);

        // Check that the provider has an API key configured
        var settings = GetSettingsFor(providerType);
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new AIProviderException(
                $"AI provider '{providerType}' is registered but has no API key configured.", providerType);

        return provider;
    }

    public AIProviderEnum[] GetAvailableProviders()
    {
        return _providers.Keys
            .Where(p => !string.IsNullOrWhiteSpace(GetSettingsFor(p).ApiKey))
            .ToArray();
    }

    public ProviderOutDTO[] GetAvailableProviderDetails()
    {
        var available = GetAvailableProviders();
        var defaultProvider = _options.DefaultProvider;

        return available
            .Select(p =>
            {
                var modelId = GetSettingsFor(p).ModelId;
                var isDefault = p.ToString().Equals(defaultProvider, StringComparison.OrdinalIgnoreCase);
                return new ProviderOutDTO(p.ToString(), modelId, isDefault);
            })
            .ToArray();
    }

    private ProviderSettings GetSettingsFor(AIProviderEnum provider) => provider switch
    {
        AIProviderEnum.Claude => _options.Claude,
        AIProviderEnum.OpenAI => _options.OpenAI,
        AIProviderEnum.Gemini => _options.Gemini,
        _ => new ProviderSettings()
    };

    private void LogAvailableProviders()
    {
        var available = GetAvailableProviders();
        if (available.Length == 0)
        {
            _logger.LogWarning("No AI providers have API keys configured. " +
                "Set keys via user-secrets or appsettings.");
        }
        else
        {
            _logger.LogInformation("Available AI providers: {Providers}",
                string.Join(", ", available));
        }
    }
}
