using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure.AI.Configuration;
using DocuMind.Infrastructure.AI.Factory;
using DocuMind.Infrastructure.AI.Providers;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Persistence.Repositories;
using DocuMind.Infrastructure.TextProcessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core + SQLite
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=documind.db";

        services.AddDbContext<DocuMindDbContext>(options =>
            options.UseSqlite(connectionString));

        // Repositories
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();

        // Text processing
        services.AddSingleton<ITextExtractor, PdfTextExtractor>();
        services.AddSingleton<ITextExtractor, DocxTextExtractor>();
        services.AddSingleton<ITextExtractor, PlainTextExtractor>();
        services.AddSingleton<ITextChunker, TextChunker>();

        // AI providers
        services.AddAIProviders(configuration);

        return services;
    }

    /// <summary>
    /// Registers AI provider configuration, implementations, and factory.
    /// Each provider is registered as a singleton IAIProvider so the factory
    /// can resolve all of them via IEnumerable.
    /// </summary>
    public static IServiceCollection AddAIProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AIProviderOptions>(
            configuration.GetSection(AIProviderOptions.SectionName));

        // Register each provider as IAIProvider — the factory collects them all
        services.AddSingleton<IAIProvider, ClaudeAIProvider>();
        services.AddSingleton<IAIProvider, OpenAIProvider>();
        services.AddSingleton<IAIProvider, GeminiAIProvider>();
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();

        return services;
    }
}
