using DocuMind.Domain.Enums;

namespace DocuMind.Application.Exceptions;

/// <summary>
/// Thrown when an AI provider call fails or when a provider is not configured.
/// </summary>
public class AIProviderException : Exception
{
    public AIProvider? Provider { get; }

    public AIProviderException(string message, AIProvider? provider = null)
        : base(message)
    {
        Provider = provider;
    }

    public AIProviderException(string message, Exception innerException, AIProvider? provider = null)
        : base(message, innerException)
    {
        Provider = provider;
    }
}
