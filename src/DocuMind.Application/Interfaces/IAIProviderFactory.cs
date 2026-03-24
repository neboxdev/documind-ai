using DocuMind.Domain.Enums;

namespace DocuMind.Application.Interfaces;

public interface IAIProviderFactory
{
    IAIProvider GetProvider(AIProvider providerType);
    AIProvider[] GetAvailableProviders();
}
