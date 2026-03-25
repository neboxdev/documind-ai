# DocuMind AI — Day 5 Build Log

## What This Document Is

Walkthrough of Day 5: Azure cloud infrastructure, blob storage, enhanced health checks, CI/CD pipeline, and project finalization.

---

## The Goal

Wire up Azure Blob Storage for file persistence, build a CI/CD pipeline with GitHub Actions, expand health checks to cover all external dependencies, and produce a README that someone could use to understand, configure, and deploy the project without reading the source code.

---

## What Was Built

### Blob Storage (IBlobStorageService)

**Interface** in Application layer:
- `UploadAsync()` — stores a file, returns URL/path
- `DeleteAsync()` — removes a file by its URL/path
- `IsHealthyAsync()` — connectivity check for health endpoint

**Two implementations** in Infrastructure:

1. **LocalFileStorageService** — Development fallback. Stores files under an `uploads/` directory in the working folder. Prefixes filenames with a GUID to avoid collisions. Health check just verifies the directory exists.

2. **AzureBlobStorageService** — Production implementation. Uses `Azure.Storage.Blobs` SDK. Uploads to a configurable container (defaults to "documents"), creates the container if it doesn't exist. Files are organized as `{guid}/{originalFileName}` to avoid collisions while keeping human-readable names in blob explorer.

**DI registration** picks the right one automatically:
```csharp
var azureStorageConnection = configuration["AzureStorage:ConnectionString"];
if (!string.IsNullOrWhiteSpace(azureStorageConnection))
    services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
else
    services.AddSingleton<IBlobStorageService, LocalFileStorageService>();
```

No feature flags, no environment checks — just "is the connection string present?"

### Upload Pipeline Changes

The `UploadDocumentCommandHandler` now stores the original file in blob storage before text extraction:

1. Copy the incoming stream to a `MemoryStream` (because extraction will consume it)
2. Upload the copy to blob storage via `IBlobStorageService`
3. Store the returned URL in `Document.BlobUrl`
4. Reset the original stream position and proceed with text extraction

The stream copy is necessary because `IFormFile.OpenReadStream()` is forward-only in some hosting scenarios, and the text extractors (especially PdfPig) seek within the stream.

### Enhanced Health Checks

The `/health/detailed` endpoint now checks three things:

1. **Database** — `CanConnectAsync()` on the EF Core DbContext
2. **AI Providers** — reports which providers have API keys configured (not a live API call — that would be slow and could hit rate limits)
3. **Blob Storage** — calls `IsHealthyAsync()` on whichever storage implementation is active

Response format:
```json
{
  "database": "healthy",
  "aiProviders": {
    "status": "healthy",
    "configured": ["Claude", "OpenAI"]
  },
  "blobStorage": "healthy"
}
```

Returns 200 if database and storage are healthy, 503 otherwise. AI providers being "degraded" (no keys configured) doesn't trigger a 503 — the API still works for document upload and browsing, just not for Q&A.

### GitHub Actions CI/CD

`.github/workflows/deploy.yml` — two-job pipeline:

**Job 1: build-and-test** (runs on every push and PR to `main`)
- Setup .NET 9
- Restore, build in Release mode, run all tests
- On merge to `main`: publish the API project and upload as an artifact

**Job 2: deploy** (only on merge to `main`)
- Downloads the published artifact
- Deploys to Azure App Service using the publish profile from GitHub Secrets

The workflow uses the `environment: production` protection, so deployment can require manual approval if configured in the repo settings.

### Final README

Rewrote the README from scratch with:
- CI/CD badge
- Expanded architecture diagram showing component names
- Full tech stack table with versions
- AI provider comparison table with SDK references
- Three curl examples with JSON responses (upload, create conversation, send message)
- Complete API reference table
- Docker instructions
- Azure deployment guide with required secrets and app settings
- Design decisions section explaining the "why" behind architectural choices
- Test running instructions

---

## Challenges and How They Were Resolved

### 1. Stream Consumption During Upload

**What happened:** The upload handler was passing the file stream to the blob storage service, then trying to pass the same stream to the text extractor. By the time the extractor got it, the stream position was at the end, so it extracted zero text.

**The fix:** Copy the stream to a `MemoryStream` for blob upload, then reset the original stream position back to 0 before extraction. The `MemoryStream` copy is fine for files up to the 10MB limit enforced by the validator.

### 2. HealthController Constructor Growing

**What happened:** The health controller originally only injected `DocuMindDbContext`. Adding `IAIProviderFactory` and `IBlobStorageService` made the constructor wider. This raised the question of whether to use a dedicated health check infrastructure (ASP.NET Core's `IHealthCheck` registrations) instead of a manual controller.

**The decision:** Kept the controller approach. ASP.NET Core's built-in health check system is great for simple up/down checks, but the `/health/detailed` endpoint returns structured data (which providers are configured, per-component status) that's hard to express through the standard `HealthCheckResult` model. The controller gives full control over the response shape. The basic `/health` endpoint still uses the built-in `MapHealthChecks()`.

### 3. Azure vs. Local Storage DI Decision

**What happened:** Initially considered using a `StorageProvider` enum in configuration, similar to the AI provider pattern. But unlike AI providers (where you want all three registered simultaneously), storage is one-or-the-other.

**The fix:** A simple null check on the Azure connection string at DI registration time. If the string is present, use Azure. If not, use local. No feature flags, no enum, no factory. The `IBlobStorageService` interface is the same either way — consuming code doesn't know or care.

---

## Test Results

All **56 tests** pass (40 unit + 16 integration), 0 warnings, 0 errors.

The existing upload handler unit tests were updated to include a mocked `IBlobStorageService`. Integration tests continue to work because `LocalFileStorageService` is registered by default (no Azure connection string in test config).

No new tests were added in Day 5 specifically — the blob storage integration is covered by the existing upload tests through the mock, and the health endpoint tests from Day 1 still verify `/health/detailed` returns 200.

---

## Project Summary After Day 5

### Final File Count
- **50+ source files** across 4 source projects
- **8 test files** across 2 test projects
- **56 total tests** (40 unit + 16 integration)
- **5 build log documents** in `docs/`
- CI/CD workflow, Dockerfile, docker-compose, nuget.config, .gitignore

### What the API Can Do
1. Upload PDF, DOCX, or TXT documents (stored in blob storage, extracted into text chunks)
2. Create conversations about any uploaded document, selecting Claude, ChatGPT, or Gemini as the AI provider
3. Ask questions — the pipeline finds relevant chunks, builds a context prompt, calls the selected AI, and saves the full exchange with token usage metadata
4. Browse documents, conversations, and message history through paginated endpoints
5. Health monitoring for database, AI providers, and storage
6. Structured error handling with RFC 7807 Problem Details
7. Request validation via FluentValidation pipeline
8. Structured logging with Serilog
9. Docker support and CI/CD to Azure App Service
