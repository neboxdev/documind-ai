using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace DocuMind.IntegrationTests;

public class ConversationEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ConversationEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> UploadTestDocumentAsync()
    {
        var content = new MultipartFormDataContent();
        var text = "This document describes quarterly revenue of $10 million and team headcount of 50 people.";
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(text));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test.txt");

        var response = await _client.PostAsync("/api/documents/upload", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task CreateConversation_ValidDocument_ReturnsCreated()
    {
        var documentId = await UploadTestDocumentAsync();

        var body = new StringContent(
            JsonSerializer.Serialize(new { Title = "Test Convo" }),
            Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/documents/{documentId}/conversations", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Test Convo");
        doc.RootElement.GetProperty("documentId").GetString().Should().Be(documentId);
    }

    [Fact]
    public async Task CreateConversation_NonExistentDocument_ReturnsNotFound()
    {
        var body = new StringContent(
            JsonSerializer.Serialize(new { Title = "Orphan" }),
            Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/documents/{Guid.NewGuid()}/conversations", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateConversation_EmptyTitle_ReturnsBadRequest()
    {
        var documentId = await UploadTestDocumentAsync();

        var body = new StringContent(
            JsonSerializer.Serialize(new { Title = "" }),
            Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/documents/{documentId}/conversations", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetConversations_AfterCreating_ReturnsList()
    {
        var documentId = await UploadTestDocumentAsync();

        // Create two conversations
        for (var i = 0; i < 2; i++)
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { Title = $"Convo {i}" }),
                Encoding.UTF8, "application/json");
            await _client.PostAsync($"/api/documents/{documentId}/conversations", body);
        }

        var response = await _client.GetAsync($"/api/documents/{documentId}/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetMessages_EmptyConversation_ReturnsEmptyArray()
    {
        var documentId = await UploadTestDocumentAsync();

        // Create a conversation
        var createBody = new StringContent(
            JsonSerializer.Serialize(new { Title = "Empty Convo" }),
            Encoding.UTF8, "application/json");

        var createResponse = await _client.PostAsync(
            $"/api/documents/{documentId}/conversations", createBody);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var conversationId = createDoc.RootElement.GetProperty("id").GetString();

        // Get messages — should be empty
        var response = await _client.GetAsync($"/api/conversations/{conversationId}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var messagesDoc = JsonDocument.Parse(json);
        messagesDoc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetMessages_NonExistentConversation_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/conversations/{Guid.NewGuid()}/messages");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
