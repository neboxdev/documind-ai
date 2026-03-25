# DocuMind AI

![Build Status](https://github.com/neboxdev/documind-ai/actions/workflows/deploy.yml/badge.svg)
![GitHub](https://img.shields.io/github/license/neboxdev/documind-ai)

A production-grade .NET 9 REST API for intelligent document analysis. Upload documents (PDF, DOCX, TXT), then have natural-language conversations about their content using your choice of AI provider — Claude, ChatGPT, or Gemini. Each conversation locks in a provider, keeping context consistent throughout the exchange.

## Architecture

```
                        ┌────────────────────────┐
                        │      HTTP Clients       │
                        └───────────┬────────────┘
                                    │
┌───────────────────────────────────▼──────────────────────────────────┐
│                         DocuMind.API                                 │
│            Controllers · GlobalExceptionHandler · Swagger            │
├─────────────────────────────────────────────────────────────────────┤
│                      DocuMind.Application                            │
│     CQRS Commands/Queries · MediatR Handlers · FluentValidation      │
│         Interfaces · DTOs (InDTO/OutDTO) · ValidationBehavior        │
├─────────────────────────────────────────────────────────────────────┤
│                        DocuMind.Domain                               │
│           Document · DocumentChunk · Conversation · Message          │
│                 AIProvider enum · DocumentStatus enum                 │
├─────────────────────────────────────────────────────────────────────┤
│                     DocuMind.Infrastructure                          │
│    EF Core (SQLite) · AI Providers · Blob Storage · Repositories     │
│   ClaudeAIProvider · OpenAIProvider · GeminiAIProvider · Factory      │
│       PdfTextExtractor · DocxTextExtractor · TextChunker             │
└─────────────────────────────────────────────────────────────────────┘
```

## Tech Stack

| Category          | Technology                              |
|-------------------|-----------------------------------------|
| Framework         | .NET 9 / ASP.NET Core 9                |
| ORM               | Entity Framework Core 9                 |
| Database          | SQLite (dev) / Azure SQL (prod)         |
| CQRS              | MediatR 12                              |
| Validation        | FluentValidation 11                     |
| Logging           | Serilog (console + rolling file)        |
| AI Providers      | Claude, ChatGPT (OpenAI), Gemini        |
| File Storage      | Local filesystem (dev) / Azure Blob (prod) |
| CI/CD             | GitHub Actions → Azure App Service      |
| Testing           | xUnit + FluentAssertions + Moq          |

## Supported AI Providers

| Provider   | Default Model               | SDK             | Docs                                      |
|------------|-----------------------------|-----------------|--------------------------------------------|
| Claude     | claude-sonnet-4-20250514    | Anthropic .NET  | https://docs.anthropic.com/                |
| OpenAI     | gpt-4o                      | OpenAI .NET     | https://platform.openai.com/docs           |
| Gemini     | gemini-2.5-flash            | Google.GenAI    | https://ai.google.dev/docs                 |

## Quick Start

```bash
git clone https://github.com/neboxdev/documind-ai.git
cd DocuMindAI
dotnet run --project src/DocuMind.API
```

Open `https://localhost:5001/swagger` for the interactive API docs.

## Configuration

### AI Provider API Keys (required for Q&A)

```bash
cd src/DocuMind.API
dotnet user-secrets init
dotnet user-secrets set "AIProviders:Claude:ApiKey" "sk-ant-..."
dotnet user-secrets set "AIProviders:OpenAI:ApiKey" "sk-..."
dotnet user-secrets set "AIProviders:Gemini:ApiKey" "AIza..."
```

You only need to configure the providers you plan to use. The API will report which are available via `GET /api/providers`.

### Azure Blob Storage (optional)

For local development, files are stored on the filesystem under `uploads/`. For production:

```bash
dotnet user-secrets set "AzureStorage:ConnectionString" "DefaultEndpointsProtocol=https;..."
```

## Example API Requests

### Upload a document

```bash
curl -X POST https://localhost:5001/api/documents/upload \
  -F "file=@report.pdf"
```

```json
{
  "id": "a1b2c3d4-...",
  "fileName": "report.pdf",
  "contentType": "application/pdf",
  "size": 45230,
  "status": "Processed",
  "uploadedAt": "2025-03-25T10:30:00Z",
  "chunkCount": 12
}
```

### Start a conversation with a specific provider

```bash
curl -X POST https://localhost:5001/api/documents/{documentId}/conversations \
  -H "Content-Type: application/json" \
  -d '{"title": "Q4 Report Review", "provider": "OpenAI"}'
```

```json
{
  "id": "e5f6g7h8-...",
  "documentId": "a1b2c3d4-...",
  "title": "Q4 Report Review",
  "provider": "OpenAI",
  "modelId": "gpt-4o",
  "createdAt": "2025-03-25T10:31:00Z"
}
```

### Ask a question

```bash
curl -X POST https://localhost:5001/api/conversations/{conversationId}/messages \
  -H "Content-Type: application/json" \
  -d '{"content": "What were the key revenue figures?"}'
```

```json
{
  "id": "i9j0k1l2-...",
  "role": "assistant",
  "content": "According to the document, Q4 revenue was...",
  "modelId": "gpt-4o",
  "promptTokens": 850,
  "completionTokens": 120,
  "createdAt": "2025-03-25T10:31:05Z"
}
```

## API Reference

| Method | Route                                     | Description                           |
|--------|-------------------------------------------|---------------------------------------|
| POST   | /api/documents/upload                     | Upload a document (PDF, DOCX, TXT)    |
| GET    | /api/documents                            | List all documents (paginated)        |
| GET    | /api/documents/{id}                       | Get document details + chunk count    |
| POST   | /api/documents/{id}/delete                | Delete a document and its data        |
| GET    | /api/providers                            | List available AI providers           |
| POST   | /api/documents/{id}/conversations         | Start a conversation                  |
| GET    | /api/documents/{id}/conversations         | List conversations for a document     |
| POST   | /api/conversations/{id}/messages          | Send a message, get AI response       |
| GET    | /api/conversations/{id}/messages          | Get message history                   |
| GET    | /health                                   | Basic health check                    |
| GET    | /health/detailed                          | DB + AI providers + storage status    |

## Frontend

A lightweight HTML/CSS/JS frontend is included in the `frontend/` folder. Open `frontend/index.html` in your browser (with the API running) to interact with DocuMind AI without any tools. No build step required.

## Docker

```bash
docker compose up --build
```

API available at `http://localhost:5000/swagger`.

## Azure Deployment

The project includes a GitHub Actions workflow (`.github/workflows/deploy.yml`) that:
1. Builds and runs all tests on every push/PR to `main`
2. Publishes and deploys to Azure App Service on merge to `main`

### Required GitHub Secrets

| Secret                           | Description                          |
|----------------------------------|--------------------------------------|
| `AZURE_WEBAPP_PUBLISH_PROFILE`   | Azure App Service publish profile    |

### Required Azure App Settings

| Setting                                    | Description                  |
|--------------------------------------------|------------------------------|
| `ConnectionStrings__DefaultConnection`     | Azure SQL connection string  |
| `AIProviders__Claude__ApiKey`              | Anthropic API key            |
| `AIProviders__OpenAI__ApiKey`              | OpenAI API key               |
| `AIProviders__Gemini__ApiKey`              | Google AI API key            |
| `AzureStorage__ConnectionString`           | Azure Blob connection string |

## Design Decisions

- **Multi-provider AI via Strategy pattern**: each provider implements `IAIProvider`, resolved by `AIProviderFactory`. Adding a new provider means one class and one DI registration.
- **Per-conversation provider selection**: once you start a conversation, the provider and model are locked in. This keeps the AI context consistent and avoids mixing system prompts across providers.
- **Clean Architecture**: Domain has zero dependencies. Application defines interfaces. Infrastructure implements them. API composes everything. Tests can mock any boundary.
- **CQRS with MediatR**: every operation is a discrete command or query, validated by FluentValidation before the handler runs. Controllers contain no business logic.
- **Keyword-based chunk search (v1)**: relevance scoring by keyword hits in document chunks. Simple and dependency-free. A production system would use vector embeddings.
- **Local storage fallback**: blob storage uses the local filesystem in development, Azure Blob Storage in production — same interface, swapped at DI registration time.

## Running Tests

```bash
dotnet test DocuMind.slnx
```

56 tests: 40 unit + 16 integration. Unit tests mock all external dependencies. Integration tests use an in-memory SQLite database via `WebApplicationFactory`.

## License

MIT
