using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure.AI.Configuration;
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

        // AI provider configuration — actual provider implementations
        // will be registered in Day 3 via AddAIProviders()
        services.Configure<AIProviderOptions>(
            configuration.GetSection(AIProviderOptions.SectionName));

        return services;
    }
}
