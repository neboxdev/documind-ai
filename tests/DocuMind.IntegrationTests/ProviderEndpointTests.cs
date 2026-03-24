using System.Net;
using FluentAssertions;

namespace DocuMind.IntegrationTests;

public class ProviderEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProviderEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProviders_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/providers");

        // No API keys configured in test environment, so it should return
        // an empty list but still a 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
