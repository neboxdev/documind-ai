using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace DocuMind.IntegrationTests;

public class DocumentEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DocumentEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Upload_ValidTxtFile_ReturnsCreated()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Hello world. This is a test document."));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test.txt");

        var response = await _client.PostAsync("/api/documents/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("fileName").GetString().Should().Be("test.txt");
        doc.RootElement.GetProperty("status").GetString().Should().Be("Processed");
    }

    [Fact]
    public async Task Upload_UnsupportedType_ReturnsBadRequest()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([1, 2, 3]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "photo.png");

        var response = await _client.PostAsync("/api/documents/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithPaginatedResult()
    {
        var response = await _client.GetAsync("/api/documents?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("pageSize").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/documents/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/documents/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadThenGetById_ReturnsDocumentWithChunkCount()
    {
        // Upload
        var content = new MultipartFormDataContent();
        var text = string.Join(" ", Enumerable.Range(1, 50).Select(i => $"Sentence number {i} in the document."));
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(text));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "multi-chunk.txt");

        var uploadResponse = await _client.PostAsync("/api/documents/upload", content);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var uploadJson = await uploadResponse.Content.ReadAsStringAsync();
        using var uploadDoc = JsonDocument.Parse(uploadJson);
        var id = uploadDoc.RootElement.GetProperty("id").GetString();

        // Get by ID
        var getResponse = await _client.GetAsync($"/api/documents/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = await getResponse.Content.ReadAsStringAsync();
        using var getDoc = JsonDocument.Parse(getJson);
        getDoc.RootElement.GetProperty("chunkCount").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UploadThenDelete_ReturnsNoContent()
    {
        // Upload
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Delete me."));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "deletable.txt");

        var uploadResponse = await _client.PostAsync("/api/documents/upload", content);
        var uploadJson = await uploadResponse.Content.ReadAsStringAsync();
        using var uploadDoc = JsonDocument.Parse(uploadJson);
        var id = uploadDoc.RootElement.GetProperty("id").GetString();

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/documents/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone
        var getResponse = await _client.GetAsync($"/api/documents/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
