using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly DocuMindDbContext _db;
    private readonly IAIProviderFactory _providerFactory;
    private readonly IBlobStorageService _blobStorage;

    public HealthController(
        DocuMindDbContext db,
        IAIProviderFactory providerFactory,
        IBlobStorageService blobStorage)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
    }

    /// <summary>
    /// Detailed health check — verifies database, AI providers, and blob storage.
    /// </summary>
    [HttpGet("detailed", Name = nameof(Detailed))]
    public async Task<IActionResult> Detailed(CancellationToken ct)
    {
        var checks = new Dictionary<string, object>();

        // Database
        try
        {
            await _db.Database.CanConnectAsync(ct);
            checks["database"] = "healthy";
        }
        catch
        {
            checks["database"] = "unhealthy";
        }

        // AI Providers — report which ones have keys configured
        var availableProviders = _providerFactory.GetAvailableProviders();
        checks["aiProviders"] = new
        {
            status = availableProviders.Length > 0 ? "healthy" : "degraded",
            configured = availableProviders.Select(p => p.ToString()).ToArray()
        };

        // Blob Storage
        try
        {
            var storageHealthy = await _blobStorage.IsHealthyAsync(ct);
            checks["blobStorage"] = storageHealthy ? "healthy" : "unhealthy";
        }
        catch
        {
            checks["blobStorage"] = "unhealthy";
        }

        var dbHealthy = checks["database"]?.ToString() == "healthy";
        var storageOk = checks["blobStorage"]?.ToString() == "healthy";
        var allHealthy = dbHealthy && storageOk;

        return allHealthy ? Ok(checks) : StatusCode(503, checks);
    }
}
