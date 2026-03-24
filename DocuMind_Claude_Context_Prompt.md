# Context Prompt — DocuMind AI Portfolio Project
## Paste this at the start of your Claude Code conversation to continue building

---

I'm a Senior .NET/C# engineer with 8+ years of experience. I'm building a portfolio
project called **DocuMind AI** to showcase on Upwork and GitHub. I need your help
building it day by day using Claude Code.

## What is DocuMind AI?

A production-grade REST API built with .NET 9 that allows users to upload documents
(PDF, DOCX, TXT), processes them into searchable chunks, and lets users query their
content using natural language powered by **multiple AI providers** — Claude (Anthropic),
ChatGPT (OpenAI), and Gemini (Google) — selectable per conversation.

## Architecture

Clean Architecture with 4 main projects + 2 test projects:

```
DocuMindAI/
├── src/
│   ├── DocuMind.API/                    # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   └── Program.cs
│   ├── DocuMind.Application/            # CQRS with MediatR
│   │   ├── Features/
│   │   │   ├── Documents/               # Commands, Queries, Handlers
│   │   │   ├── Conversations/
│   │   │   └── Providers/
│   │   ├── Interfaces/                  # IAIProvider, IRepository, etc.
│   │   ├── DTOs/
│   │   └── Behaviors/                   # Validation pipeline behavior
│   ├── DocuMind.Domain/                 # Entities and enums
│   │   ├── Entities/
│   │   └── Enums/
│   └── DocuMind.Infrastructure/         # EF Core, AI providers, storage
│       ├── Persistence/                 # DbContext, Repositories, Migrations
│       ├── AI/
│       │   ├── Providers/               # ClaudeProvider, OpenAIProvider, GeminiProvider
│       │   └── Factory/                 # AIProviderFactory
│       ├── Storage/                     # Blob storage service
│       └── TextProcessing/              # Extraction, Chunking
├── tests/
│   ├── DocuMind.UnitTests/
│   └── DocuMind.IntegrationTests/
├── Dockerfile
├── docker-compose.yml
└── DocuMind.sln
```

## Tech Stack

| Category            | Technology                                         |
|---------------------|----------------------------------------------------|
| Framework           | .NET 9, ASP.NET Core 9                             |
| ORM                 | Entity Framework Core 9                            |
| Database            | SQLite (dev) / Azure SQL (prod)                    |
| CQRS                | MediatR 12.x                                      |
| Validation          | FluentValidation 11.x                              |
| Logging             | Serilog with structured logging                    |
| API Docs            | Swagger / OpenAPI (Swashbuckle)                    |
| AI — Anthropic      | `Anthropic` NuGet (official SDK)                   |
| AI — OpenAI         | `OpenAI` NuGet (official SDK)                      |
| AI — Google         | `Google.GenAI` NuGet (official SDK)                |
| PDF Extraction      | PdfPig                                             |
| DOCX Extraction     | DocumentFormat.OpenXml                             |
| Cloud               | Azure App Service + Blob Storage + Key Vault       |
| CI/CD               | GitHub Actions                                     |
| Containerization    | Docker + docker-compose                            |

## Domain Entities

### AIProvider (Enum)
```csharp
public enum AIProvider
{
    Claude = 0,
    OpenAI = 1,
    Gemini = 2
}
```

### Document
| Property    | Type              | Notes                                      |
|-------------|-------------------|--------------------------------------------|
| Id          | Guid              | Primary key                                |
| FileName    | string            |                                            |
| ContentType | string            | "application/pdf", "text/plain", etc.      |
| Size        | long              | File size in bytes                         |
| BlobUrl     | string?           | Azure Blob URL (null in local dev)         |
| Status      | DocumentStatus    | Pending → Processing → Processed / Failed  |
| UploadedAt  | DateTime          |                                            |

### DocumentChunk
| Property    | Type   | Notes                          |
|-------------|--------|--------------------------------|
| Id          | Guid   | Primary key                    |
| DocumentId  | Guid   | FK → Document                  |
| Content     | string | Chunk text                     |
| ChunkIndex  | int    | Order within the document      |

### Conversation
| Property    | Type        | Notes                                         |
|-------------|-------------|-----------------------------------------------|
| Id          | Guid        | Primary key                                   |
| DocumentId  | Guid        | FK → Document                                 |
| Title       | string      |                                               |
| Provider    | AIProvider  | Selected AI provider for this conversation    |
| ModelId     | string      | Specific model used (e.g. "gpt-4o")          |
| CreatedAt   | DateTime    |                                               |

### Message
| Property         | Type    | Notes                                    |
|------------------|---------|------------------------------------------|
| Id               | Guid    | Primary key                              |
| ConversationId   | Guid    | FK → Conversation                        |
| Role             | string  | "user" or "assistant"                    |
| Content          | string  |                                          |
| ModelId          | string? | Model that generated this response       |
| PromptTokens     | int?    | Token usage tracking                     |
| CompletionTokens | int?    | Token usage tracking                     |
| CreatedAt        | DateTime|                                          |

## AI Provider Architecture (Strategy Pattern + Factory)

### Interface (Application layer)
```csharp
public interface IAIProvider
{
    AIProvider ProviderType { get; }
    Task<AIResponse> GenerateResponseAsync(AIRequest request, CancellationToken ct = default);
}

public interface IAIProviderFactory
{
    IAIProvider GetProvider(AIProvider providerType);
    IReadOnlyList<AIProvider> GetAvailableProviders();
}
```

### Shared DTOs (Application layer)
```csharp
public record AIRequest(
    string SystemPrompt,
    IReadOnlyList<ChatMessage> Messages,
    string? ModelId = null,
    float Temperature = 0.7f,
    int MaxTokens = 4096);

public record AIResponse(
    string Content,
    string ModelId,
    int PromptTokens,
    int CompletionTokens,
    AIProvider Provider);

public record ChatMessage(string Role, string Content);
```

### Implementations (Infrastructure layer)
- **ClaudeAIProvider** — Uses `Anthropic` SDK → `MessageClient.CreateAsync()`
- **OpenAIProvider** — Uses `OpenAI` SDK → `ChatClient.CompleteChatAsync()`
- **GeminiAIProvider** — Uses `Google.GenAI` SDK → `GenerativeModel.GenerateContentAsync()`

### Configuration
```json
{
  "AIProviders": {
    "DefaultProvider": "Claude",
    "Claude": {
      "ApiKey": "",
      "ModelId": "claude-sonnet-4-20250514"
    },
    "OpenAI": {
      "ApiKey": "",
      "ModelId": "gpt-4o"
    },
    "Gemini": {
      "ApiKey": "",
      "ModelId": "gemini-2.5-flash"
    }
  }
}
```

API keys: User Secrets in development, Azure Key Vault in production.

### DI Registration
```csharp
public static IServiceCollection AddAIProviders(this IServiceCollection services, IConfiguration config)
{
    services.Configure<AIProviderOptions>(config.GetSection("AIProviders"));
    services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
    services.AddSingleton<ClaudeAIProvider>();
    services.AddSingleton<OpenAIProvider>();
    services.AddSingleton<GeminiAIProvider>();
    return services;
}
```

## API Endpoints

### Documents
| Method | Route                         | Description                        |
|--------|-------------------------------|------------------------------------|
| POST   | /api/documents/upload         | Upload file (PDF, DOCX, TXT)      |
| GET    | /api/documents                | List all documents                 |
| GET    | /api/documents/{id}           | Get document details + status      |
| DELETE | /api/documents/{id}           | Delete document and its chunks     |

### AI Providers
| Method | Route            | Description                              |
|--------|------------------|------------------------------------------|
| GET    | /api/providers   | List available providers + default model |

### Conversations
| Method | Route                                      | Description                         |
|--------|--------------------------------------------|-------------------------------------|
| POST   | /api/documents/{id}/conversations          | Start conversation (select provider)|
| GET    | /api/documents/{id}/conversations          | List conversations for a document   |
| POST   | /api/conversations/{id}/messages           | Ask question → AI answer            |
| GET    | /api/conversations/{id}/messages           | Get message history                 |

### Health
| Method | Route              | Description                            |
|--------|--------------------|----------------------------------------|
| GET    | /health            | Basic health check                     |
| GET    | /health/detailed   | Check DB, AI providers, Blob Storage   |

### Example: Create Conversation with Provider
```http
POST /api/documents/{documentId}/conversations
Content-Type: application/json

{
  "title": "Questions about Q4 Report",
  "provider": "OpenAI"
}
```
Response:
```json
{
  "id": "...",
  "documentId": "...",
  "title": "Questions about Q4 Report",
  "provider": "OpenAI",
  "modelId": "gpt-4o",
  "createdAt": "..."
}
```

## AI Pipeline

### On Upload
1. Validate file type and size
2. Extract text (PdfPig for PDF, Open XML SDK for DOCX, StreamReader for TXT)
3. Chunk text into ~500 tokens with ~50 token overlap
4. Save DocumentChunk records to database
5. Update Document.Status to Processed (or Failed on error)

### On Message
1. Receive user question + conversation ID
2. Look up conversation to get the selected AI provider
3. Find top 5 relevant DocumentChunks for the document (keyword search v1)
4. Build context string from chunk contents
5. Call the selected IAIProvider via IAIProviderFactory
6. Save user Message and assistant Message (with ModelId, token counts)
7. Return the assistant message with metadata

### System Prompt (used across all providers)
```
You are a helpful assistant that answers questions based ONLY on the provided
document context. If the answer is not found in the context, say so clearly.
Do not make up information. Be concise and precise.

--- CONTEXT ---
{relevantChunks}
--- END CONTEXT ---
```

## Error Handling and Resilience
- Each provider implementation catches provider-specific exceptions and maps to a common `AIProviderException`
- Retry with Polly for transient failures (HTTP 429, 503)
- Startup validation warns if a provider has no API key configured
- If the selected provider fails, the API returns a descriptive error (not a generic 500)
- Global exception handling middleware with structured Serilog logging

## Testing Strategy
- **Unit tests**: Mock `IAIProvider` to test MediatR handlers without calling real APIs
- **Integration tests**: Test factory resolution, configuration binding, EF Core queries
- **Provider smoke tests** (optional): Call real API with a simple prompt, gated behind environment variable

## Key Design Decisions

| Decision                  | Choice                           | Rationale                                              |
|---------------------------|----------------------------------|--------------------------------------------------------|
| AI provider abstraction   | Strategy Pattern + Factory       | Clean separation, easy to add new providers            |
| Provider selection scope  | Per-conversation                 | Simpler UX, consistent conversation history            |
| SDK approach              | Official SDKs directly           | Full feature access, better portfolio showcase         |
| Configuration             | Options pattern (IOptions<T>)    | Standard .NET 9 pattern, supports hot reload           |
| API key storage           | User Secrets (dev) / Key Vault   | Security best practice                                 |
| Default provider          | Configurable via appsettings     | Flexible per deployment                                |
| Text chunking             | ~500 tokens, 50 token overlap    | Balance between context quality and token budget       |
| Chunk retrieval           | Keyword search (v1)              | Simple starting point; can upgrade to embeddings later |

---

## Build Plan (5 Days)

### Day 1 — Foundation

```
Create a new .NET 9 Web API project called "DocuMind AI" using Clean Architecture:

Solution: DocuMind.sln
Projects:
- DocuMind.API (Web API, entry point)
- DocuMind.Application (CQRS, interfaces, DTOs)
- DocuMind.Domain (Entities, enums, value objects)
- DocuMind.Infrastructure (Repositories, DB, AI providers, file storage)
- DocuMind.UnitTests (xUnit)
- DocuMind.IntegrationTests (xUnit)

Domain entities:
- AIProvider enum (Claude = 0, OpenAI = 1, Gemini = 2)
- DocumentStatus enum (Pending, Processing, Processed, Failed)
- Document (Id, FileName, ContentType, Size, BlobUrl, Status, UploadedAt)
- DocumentChunk (Id, DocumentId, Content, ChunkIndex)
- Conversation (Id, DocumentId, Title, Provider, ModelId, CreatedAt)
- Message (Id, ConversationId, Role, Content, ModelId, PromptTokens, CompletionTokens, CreatedAt)

Requirements:
- Entity Framework Core 9 with SQLite for development
- Repository pattern with interfaces in Application layer
- MediatR for CQRS (Commands and Queries separated)
- FluentValidation with MediatR pipeline behavior
- Serilog for structured logging (console + file sinks)
- Global error handling middleware that returns RFC 7807 Problem Details
- Swagger / OpenAPI configured and enabled
- Docker support (Dockerfile + docker-compose with SQLite volume)
- .gitignore for .NET
- README.md skeleton with project description and architecture diagram in ASCII

Keep the code clean, well-structured, and production-grade. Use .NET 9 best practices.
```

### Day 2 — Document Upload and Processing

```
Add document upload and processing functionality:

1. POST /api/documents/upload endpoint that accepts multipart/form-data
   - Accept PDF, DOCX, TXT files up to 10 MB
   - Return 400 with Problem Details for invalid files

2. Text extraction per format:
   - PDF: use PdfPig NuGet package
   - DOCX: use DocumentFormat.OpenXml NuGet package
   - TXT: direct StreamReader

3. ITextChunker service: split extracted text into ~500 token chunks
   with ~50 token overlap, save as DocumentChunk records

4. Document.Status transitions: Pending → Processing → Processed (or Failed)

5. Complete CRUD endpoints:
   - GET /api/documents (paginated)
   - GET /api/documents/{id} (include chunk count in response)
   - DELETE /api/documents/{id} (cascade delete chunks)

6. Swagger fully documented with XML comments, request/response examples

Use CQRS: UploadDocumentCommand, GetDocumentsQuery, GetDocumentByIdQuery,
DeleteDocumentCommand. All business logic in Application layer handlers,
controllers are thin. Add unit tests for the chunking service.
```

### Day 3 — Multi-Provider AI Abstraction

```
Add the multi-provider AI abstraction layer:

1. Application layer interfaces:
   - IAIProvider with: AIProvider ProviderType, Task<AIResponse> GenerateResponseAsync(AIRequest, CancellationToken)
   - IAIProviderFactory with: IAIProvider GetProvider(AIProvider), IReadOnlyList<AIProvider> GetAvailableProviders()
   - Shared DTOs: AIRequest, AIResponse, ChatMessage records

2. Configuration:
   - AIProviderOptions class with DefaultProvider + per-provider settings (ApiKey, ModelId)
   - appsettings.json section: AIProviders.Claude, AIProviders.OpenAI, AIProviders.Gemini
   - Default models: claude-sonnet-4-20250514, gpt-4o, gemini-2.5-flash

3. Provider implementations in Infrastructure/AI/Providers/:
   - ClaudeAIProvider using Anthropic NuGet SDK
   - OpenAIProvider using OpenAI NuGet SDK
   - GeminiAIProvider using Google.GenAI NuGet SDK
   Each maps AIRequest/AIResponse to/from the provider-specific format.

4. AIProviderFactory in Infrastructure/AI/Factory/:
   - Resolves correct provider by enum
   - GetAvailableProviders() returns only providers with configured API keys
   - Throws descriptive AIProviderException if provider not configured

5. DI registration via AddAIProviders() extension method

6. GET /api/providers endpoint:
   - Returns list of available providers with name, modelId, isDefault flag
   - Uses CQRS: GetAvailableProvidersQuery

7. Unit tests:
   - Factory resolution tests
   - Provider availability tests (mock configuration with/without keys)

Handle provider-specific exceptions and map to a common AIProviderException.
Add Polly retry policies for transient HTTP failures (429, 503).
```

### Day 4 — Conversations and Q&A Pipeline

```
Add conversation and Q&A functionality with provider selection:

1. POST /api/documents/{id}/conversations — create conversation
   - Request body: { "title": "...", "provider": "Claude" }
   - Provider is optional, defaults to AIProviders.DefaultProvider from config
   - Validates provider is available (has API key)
   - Stores Provider enum and resolved ModelId on the Conversation entity
   - Returns conversation with provider info

2. GET /api/documents/{id}/conversations — list conversations for document

3. POST /api/conversations/{id}/messages — send message, get AI response
   - Request body: { "content": "What is this document about?" }
   - Pipeline:
     a. Load conversation (get Provider, DocumentId)
     b. Find top 5 relevant DocumentChunks using keyword matching
     c. Build system prompt with document context
     d. Resolve IAIProvider via factory using conversation's Provider
     e. Call GenerateResponseAsync with system prompt + message history + new question
     f. Save user Message and assistant Message (with ModelId, token counts)
     g. Return assistant message with metadata (provider, model, tokens)

4. GET /api/conversations/{id}/messages — get message history with metadata

Use CQRS: CreateConversationCommand, GetConversationsQuery,
SendMessageCommand, GetMessagesQuery.
All handlers in Application layer. Add unit tests with mocked IAIProvider.
Test the full pipeline: chunk retrieval → prompt building → provider call → message save.
```

### Day 5 — Cloud Deployment and Polish

```
Add Azure cloud infrastructure and finalize the project:

1. Azure Blob Storage:
   - IBlobStorageService interface in Application layer
   - AzureBlobStorageService implementation in Infrastructure
   - Upload files to blob on document upload, store URL in Document.BlobUrl
   - Local file storage fallback for development

2. Azure Key Vault:
   - Configure app to read secrets from Azure Key Vault in production
   - Secrets: ConnectionStrings--DefaultConnection, AIProviders--Claude--ApiKey,
     AIProviders--OpenAI--ApiKey, AIProviders--Gemini--ApiKey,
     AzureStorage--ConnectionString

3. GitHub Actions workflow (.github/workflows/deploy.yml):
   - Trigger on push to main
   - Build, run tests
   - Deploy to Azure App Service
   - Use GitHub Secrets for credentials

4. Health checks:
   - GET /health — basic ASP.NET Core health check
   - GET /health/detailed — check DB connectivity, each AI provider reachability,
     Blob Storage connectivity. Return per-provider status.

5. Final README.md:
   - Project title with CI/CD badge
   - What it does (2-3 sentences emphasizing multi-provider AI)
   - Architecture diagram (ASCII)
   - Tech stack table
   - Supported AI providers with model info
   - Quick start (clone → configure → run in 3 commands)
   - Example API requests (curl) with JSON responses showing provider selection
   - Azure deployment guide
   - Design decisions section
   - API key configuration guide for all three providers

Make the README professional — this is a portfolio project that demonstrates
Clean Architecture, multi-provider AI integration, and Azure cloud deployment.
```

---

*This context was prepared for Nebox Dev LLC using Claude Code (claude-opus-4-6).*
