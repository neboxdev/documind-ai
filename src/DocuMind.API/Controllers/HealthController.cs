using DocuMind.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly DocuMindDbContext _db;

    public HealthController(DocuMindDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Detailed health check — verifies database connectivity.
    /// AI provider and storage checks will be added in Day 5.
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> Detailed(CancellationToken ct)
    {
        var checks = new Dictionary<string, string>();

        try
        {
            await _db.Database.CanConnectAsync(ct);
            checks["database"] = "healthy";
        }
        catch
        {
            checks["database"] = "unhealthy";
        }

        var allHealthy = checks.Values.All(v => v == "healthy");
        return allHealthy ? Ok(checks) : StatusCode(503, checks);
    }
}
