using DocuMind.Application.DTOs;
using DocuMind.Application.Exceptions;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Enums;
using DocuMind.Infrastructure.AI.Configuration;
using DocuMind.Infrastructure.AI.Factory;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DocuMind.UnitTests;

public class AIProviderFactoryTests
{
    private static AIProviderOptions CreateOptions(
        string? claudeKey = null, string? openAiKey = null, string? geminiKey = null)
    {
        return new AIProviderOptions
        {
            DefaultProvider = "Claude",
            Claude = new ProviderSettings { ApiKey = claudeKey ?? "", ModelId = "claude-sonnet-4-20250514" },
            OpenAI = new ProviderSettings { ApiKey = openAiKey ?? "", ModelId = "gpt-4o" },
            Gemini = new ProviderSettings { ApiKey = geminiKey ?? "", ModelId = "gemini-2.5-flash" }
        };
    }

    private static IAIProvider CreateMockProvider(AIProvider type)
    {
        var mock = new Mock<IAIProvider>();
        mock.Setup(p => p.ProviderType).Returns(type);
        return mock.Object;
    }

    private static AIProviderFactory CreateFactory(
        AIProviderOptions options, params IAIProvider[] providers)
    {
        return new AIProviderFactory(
            providers,
            Options.Create(options),
            Mock.Of<ILogger<AIProviderFactory>>());
    }

    [Fact]
    public void GetProvider_WithConfiguredKey_ReturnsProvider()
    {
        var options = CreateOptions(claudeKey: "sk-test-key");
        var claudeProvider = CreateMockProvider(AIProvider.Claude);
        var factory = CreateFactory(options, claudeProvider);

        var result = factory.GetProvider(AIProvider.Claude);

        result.Should().BeSameAs(claudeProvider);
    }

    [Fact]
    public void GetProvider_WithoutKey_ThrowsAIProviderException()
    {
        var options = CreateOptions(); // no keys
        var claudeProvider = CreateMockProvider(AIProvider.Claude);
        var factory = CreateFactory(options, claudeProvider);

        var act = () => factory.GetProvider(AIProvider.Claude);

        act.Should().Throw<AIProviderException>()
            .Which.Provider.Should().Be(AIProvider.Claude);
    }

    [Fact]
    public void GetProvider_UnregisteredProvider_ThrowsAIProviderException()
    {
        var options = CreateOptions(claudeKey: "sk-test");
        var claudeProvider = CreateMockProvider(AIProvider.Claude);
        var factory = CreateFactory(options, claudeProvider);

        // OpenAI is not registered
        var act = () => factory.GetProvider(AIProvider.OpenAI);

        act.Should().Throw<AIProviderException>()
            .Which.Provider.Should().Be(AIProvider.OpenAI);
    }

    [Fact]
    public void GetAvailableProviders_ReturnsOnlyConfiguredProviders()
    {
        var options = CreateOptions(claudeKey: "key1", geminiKey: "key3");
        var providers = new[]
        {
            CreateMockProvider(AIProvider.Claude),
            CreateMockProvider(AIProvider.OpenAI),
            CreateMockProvider(AIProvider.Gemini)
        };
        var factory = CreateFactory(options, providers);

        var available = factory.GetAvailableProviders();

        available.Should().HaveCount(2);
        available.Should().Contain(AIProvider.Claude);
        available.Should().Contain(AIProvider.Gemini);
        available.Should().NotContain(AIProvider.OpenAI);
    }

    [Fact]
    public void GetAvailableProviders_NoKeysConfigured_ReturnsEmpty()
    {
        var options = CreateOptions();
        var providers = new[]
        {
            CreateMockProvider(AIProvider.Claude),
            CreateMockProvider(AIProvider.OpenAI)
        };
        var factory = CreateFactory(options, providers);

        var available = factory.GetAvailableProviders();

        available.Should().BeEmpty();
    }

    [Fact]
    public void GetAvailableProviderDetails_ReturnsCorrectDTOs()
    {
        var options = CreateOptions(claudeKey: "key1", openAiKey: "key2");
        var providers = new[]
        {
            CreateMockProvider(AIProvider.Claude),
            CreateMockProvider(AIProvider.OpenAI),
            CreateMockProvider(AIProvider.Gemini)
        };
        var factory = CreateFactory(options, providers);

        var details = factory.GetAvailableProviderDetails();

        details.Should().HaveCount(2);

        var claude = details.First(d => d.Name == "Claude");
        claude.ModelId.Should().Be("claude-sonnet-4-20250514");
        claude.IsDefault.Should().BeTrue();

        var openai = details.First(d => d.Name == "OpenAI");
        openai.ModelId.Should().Be("gpt-4o");
        openai.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void GetAvailableProviderDetails_AllKeysConfigured_ReturnsAll()
    {
        var options = CreateOptions(claudeKey: "k1", openAiKey: "k2", geminiKey: "k3");
        var providers = new[]
        {
            CreateMockProvider(AIProvider.Claude),
            CreateMockProvider(AIProvider.OpenAI),
            CreateMockProvider(AIProvider.Gemini)
        };
        var factory = CreateFactory(options, providers);

        var details = factory.GetAvailableProviderDetails();

        details.Should().HaveCount(3);
        details.Should().ContainSingle(d => d.IsDefault);
    }
}
