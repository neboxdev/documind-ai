using DocuMind.Application.DTOs;
using DocuMind.Domain.Enums;

namespace DocuMind.Application.Interfaces;

public interface IAIProviderFactory
{
    IAIProvider GetProvider(AIProvider providerType);
    AIProvider[] GetAvailableProviders();

    /// <summary>
    /// Returns provider metadata (name, default model, isDefault flag)
    /// for all providers that have an API key configured.
    /// </summary>
    ProviderOutDTO[] GetAvailableProviderDetails();
}
