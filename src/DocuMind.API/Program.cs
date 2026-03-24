using DocuMind.API.Middleware;
using DocuMind.Application;
using DocuMind.Infrastructure;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/documind-.log", rollingInterval: RollingInterval.Day));

    // Layer registrations
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // API stuff
    builder.Services.AddControllers();
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
