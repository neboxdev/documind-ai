using DocuMind.API.Middleware;
using DocuMind.Application;
using DocuMind.Infrastructure;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog — use AddSerilog instead of UseSerilog to avoid the
    // ReloadableLogger/freeze pattern that breaks WebApplicationFactory
    builder.Services.AddSerilog(config => config
        .ReadFrom.Configuration(builder.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/documind-.log", rollingInterval: RollingInterval.Day));

    // Layer registrations
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // API stuff
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "DocuMind AI",
            Version = "v1",
            Description = "Intelligent Document Analysis API with multi-provider AI support"
        });
    });

    builder.Services.AddTransient<GlobalExceptionHandler>();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Auto-migrate in development — EnsureCreated works for both SQLite and InMemory
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DocuMindDbContext>();
        db.Database.EnsureCreated();
    }

    app.UseSerilogRequestLogging();
    app.UseMiddleware<GlobalExceptionHandler>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Needed for integration test WebApplicationFactory
public partial class Program { }
