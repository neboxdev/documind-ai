using System.Data.Common;
using DocuMind.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.IntegrationTests;

/// <summary>
/// Uses an in-memory SQLite connection for integration tests.
/// This avoids the dual-provider conflict that happens when mixing
/// the SQLite and InMemory EF providers in the same service collection.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Kept open for the lifetime of the factory — closing it destroys the in-memory DB
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var optionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DocuMindDbContext>));
            if (optionsDescriptor != null)
                services.Remove(optionsDescriptor);

            services.AddDbContext<DocuMindDbContext>(options =>
                options.UseSqlite(_connection));
        });

        builder.UseEnvironment("Development");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
