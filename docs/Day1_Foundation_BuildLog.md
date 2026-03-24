# DocuMind AI — Day 1 Build Log

## What This Document Is

A plain-English walkthrough of the Day 1 foundation work for DocuMind AI: what was built, what tools were used, what broke along the way, and how each problem was solved. This isn't a polished retrospective — it's closer to a lab notebook.

---

## The Goal

Stand up a .NET 9 solution from scratch with Clean Architecture, wire up the database, configure logging and error handling, get Swagger running, make Docker work, and end the day with a green test suite. Nothing fancy yet — no AI calls, no file uploads — just a solid skeleton that the next four days can build on top of without fighting the foundation.

---

## Solution Structure

Six projects, split across `src/` and `tests/`:

```
DocuMind.slnx
├── src/
│   ├── DocuMind.API            → ASP.NET Core Web API (entry point)
│   ├── DocuMind.Application    → CQRS handlers, interfaces, DTOs, validation
│   ├── DocuMind.Domain         → Entities and enums (zero dependencies)
│   └── DocuMind.Infrastructure → EF Core, repositories, AI config, external concerns
└── tests/
    ├── DocuMind.UnitTests          → xUnit + Moq + FluentAssertions
    └── DocuMind.IntegrationTests   → WebApplicationFactory-based API tests
```

The dependency flow follows Clean Architecture strictly:

- **Domain** depends on nothing.
- **Application** depends on Domain only.
- **Infrastructure** depends on Application (and therefore Domain transitively).
- **API** depends on Application and Infrastructure — it's the composition root.
- **UnitTests** reference Application, Domain, and Infrastructure for testing.
- **IntegrationTests** reference only API (they boot the whole app via `WebApplicationFactory`).

This means the business logic in Application has no idea what database it's talking to, which AI provider is active, or how HTTP requests are shaped. That's the whole point.

---

## Tools and Libraries

| Library | Version | Why It's Here |
|---|---|---|
| **EF Core 9 + SQLite** | 9.0.3 | ORM with a zero-config local database. SQLite was chosen for development; the plan is Azure SQL in production. The connection string is swappable via config. |
| **MediatR** | 12.4.1 | Implements CQRS — every operation is either a command or a query, dispatched through a mediator. This keeps controllers thin and makes each operation independently testable. |
| **FluentValidation** | 11.11.0 | Declarative validation rules that plug into MediatR's pipeline via a custom `ValidationBehavior`. Validators are discovered by assembly scanning — drop a new one in and it just works. |
| **Serilog** | 9.0.0 | Structured logging to both console and rolling log files. The bootstrap logger catches startup failures before the full pipeline is configured. |
| **Swashbuckle** | 7.3.1 | Swagger/OpenAPI UI. Only enabled in Development — you don't want this exposed in production. |
| **xUnit + FluentAssertions + Moq** | Various | Standard .NET test stack. FluentAssertions makes test assertions read like sentences. Moq is there for unit tests that need to fake out interfaces. |
| **Microsoft.AspNetCore.Mvc.Testing** | 9.0.3 | Boots the real ASP.NET pipeline in-memory for integration tests without needing a running server. |

---

## The Domain Layer

Four entities, two enums. Deliberately simple — no base classes, no `IAuditable` interfaces, no event sourcing. Just POCOs with navigation properties.

**Enums:**
- `AIProvider` — Claude, OpenAI, Gemini. Integer-backed (0, 1, 2) but stored as strings in the database for readability.
- `DocumentStatus` — Pending → Processing → Processed, or Failed. Tracks the document's lifecycle.

**Entities:**
- `Document` — represents an uploaded file. Tracks filename, content type, size, blob URL, and status. Has collections of chunks and conversations.
- `DocumentChunk` — a slice of extracted text from a document, indexed for ordered reassembly.
- `Conversation` — ties a document to an AI provider. Each conversation locks in which provider (Claude/OpenAI/Gemini) and model it uses. This was a deliberate design choice: per-conversation provider selection is simpler than per-message switching and keeps the conversation history consistent for the AI.
- `Message` — a single turn in a conversation. Tracks role ("user" or "assistant"), content, token usage, and which model generated the response.

All entities use `Guid` primary keys and `DateTime.UtcNow` defaults for timestamps.

---

## The Application Layer

This is where the contracts live. No implementations — just interfaces that Infrastructure fills in.

**Interfaces:**
- `IDocumentRepository` / `IConversationRepository` — standard async CRUD. Nothing surprising.
- `IAIProvider` — the core abstraction. Each provider (Claude, OpenAI, Gemini) will implement this to normalize request/response behind one contract.
- `IAIProviderFactory` — resolves which `IAIProvider` to use based on the `AIProvider` enum. Strategy pattern.
- `ITextExtractor` — polymorphic extraction: each file type (PDF, DOCX, TXT) gets its own extractor.
- `ITextChunker` — splits extracted text into overlapping chunks for RAG context windows.

**DTOs:**
Record types for everything. `AIRequest` and `AIResponse` normalize the multi-provider communication. `DocumentDto`, `ConversationDto`, `MessageDto` are what the API actually returns — the controllers never expose raw entities.

**ValidationBehavior:**
A MediatR pipeline behavior that runs all `IValidator<TRequest>` instances before the handler executes. If any rule fails, it throws a `ValidationException` that the global error handler catches and turns into a 400 response. No validators exist yet (Day 1), but the pipeline is ready — just add a validator class and it gets picked up automatically.

**AIProviderException:**
A custom exception type that carries the provider name. The global error handler maps this to a 503 Service Unavailable with a meaningful message.

---

## The Infrastructure Layer

**DocuMindDbContext:**
Four `DbSet` properties. Entity configurations are loaded from the assembly via `ApplyConfigurationsFromAssembly` — each entity has its own `IEntityTypeConfiguration` class with max lengths, required fields, indexes, and cascade rules.

Notable configuration choices:
- Document status and AI provider enums are stored as strings (`HasConversion<string>()`), not integers. Makes the database human-readable and survives enum reordering.
- `DocumentChunk` has a composite index on `(DocumentId, ChunkIndex)` for efficient ordered retrieval.
- All parent-child relationships use `Cascade` delete — deleting a document removes its chunks and conversations automatically.

**Repositories:**
Thin wrappers around EF Core. Nothing clever. The `GetByIdWithChunksAsync` method eagerly loads chunks in order; `GetByIdWithMessagesAsync` does the same for conversation messages. Both repositories expose `SaveChangesAsync` — the alternative of a Unit of Work pattern was considered but rejected as over-engineering for this project's scope.

**AI Configuration:**
`AIProviderOptions` maps to the `AIProviders` section in `appsettings.json`. Each provider has an API key and a default model ID. The actual provider implementations don't exist yet — that's Day 3.

**DI Registration:**
The `AddInfrastructure()` extension method handles all Infrastructure wiring: DbContext, repositories, and options binding. The comment makes it explicit that AI providers aren't registered here yet.

---

## The API Layer

**Program.cs:**
Minimal API style (top-level statements) with a try/catch/finally for startup safety. Key decisions:

1. **Bootstrap logger** — Serilog creates a temporary console-only logger first, then replaces it with the fully configured one. This ensures startup failures before configuration loads are still logged.
2. **EnsureCreated vs. Migrate** — The original plan was `Database.Migrate()`, but that conflicts with the InMemory provider used in integration tests. `EnsureCreated()` works with both SQLite and InMemory. The trade-off is that `EnsureCreated` doesn't apply migrations — for production, we'll use explicit migration commands in the CI/CD pipeline.
3. **HostAbortedException filter** — The catch block uses `when (ex is not HostAbortedException)` to avoid logging the normal shutdown exception that EF Core tools trigger. Without this filter, `dotnet ef migrations add` would log a spurious fatal error.
4. **`public partial class Program`** — Required at the bottom so `WebApplicationFactory<Program>` can find the entry point assembly. Without it, integration tests can't boot the app.

**GlobalExceptionHandler:**
Implements `IMiddleware` (not the delegate pattern) to get proper constructor injection. Maps exceptions to RFC 7807 Problem Details responses:
- `ValidationException` → 400 Bad Request with error list
- `AIProviderException` → 503 Service Unavailable
- `KeyNotFoundException` → 404 Not Found
- Everything else → 500 Internal Server Error with a generic message (no stack traces leak to clients)

**HealthController:**
Two endpoints. `/health` is the built-in ASP.NET health check (returns 200 with "Healthy"). `/health/detailed` manually checks database connectivity and returns a JSON object with component statuses. AI provider and storage health checks are stubbed out — they'll arrive when those features are implemented.

---

## Challenges and How They Were Resolved

### 1. Azure DevOps Feed Blocking NuGet Restore

**What happened:** The development machine had a NuGet source configured for `pkgs.dev.azure.com/trueclicks/...` — a private Azure DevOps feed from a previous project. Every `dotnet add package` command tried to authenticate against it and failed with a 401, which caused the entire restore to fail.

**The fix:** Added a `nuget.config` at the solution root that clears all inherited sources and explicitly lists only `nuget.org`:

```xml
<packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources>
```

The `<clear />` directive is the key — it prevents the machine-level NuGet config from polluting the project. This file also gets committed to source control, so anyone cloning the repo won't hit the same issue.

### 2. .NET 10 Preview SDK Scaffolding `net10.0` Projects

**What happened:** The machine only had .NET SDK 10.0.201 (preview) installed. Every `dotnet new` command scaffolded projects targeting `net10.0` with NuGet packages at version 10.x. The project spec calls for .NET 9.

**The fix:** The .NET 10 SDK can compile `net9.0` targets just fine — the runtime was already installed. So instead of installing an older SDK, all `.csproj` files had their `<TargetFramework>` changed from `net10.0` to `net9.0`, and all Microsoft packages were pinned to their 9.0.3 versions. Swashbuckle was pinned to 7.3.1 and Serilog.AspNetCore to 9.0.0 — the latest stable versions compatible with .NET 9.

This was a manual find-and-replace across six `.csproj` files. Not glamorous, but necessary.

### 3. `Database.IsInMemory()` Missing Without the InMemory Package

**What happened:** The first attempt to handle the SQLite vs. InMemory provider conflict in `Program.cs` used `db.Database.IsInMemory()` to conditionally call `Migrate()` or `EnsureCreated()`. This compiled locally but failed in the test build because the API project didn't reference `Microsoft.EntityFrameworkCore.InMemory` — and that extension method lives in that package.

**The fix:** Instead of adding a test-only dependency to the API project (which would be architecturally wrong), the approach was simplified: just use `EnsureCreated()` for both providers. It works with SQLite and InMemory alike. The trade-off is that `EnsureCreated()` doesn't know about migrations, but that's fine for development. Production deployments will run `dotnet ef database update` explicitly.

### 4. Dual Database Provider Registration in Integration Tests

**What happened:** The `CustomWebApplicationFactory` originally only removed the `DbContextOptions<DocuMindDbContext>` service descriptor before adding the InMemory provider. But EF Core also registers internal services scoped to the provider — SQLite's internal services were still in the container. At runtime, EF Core detected two providers (SQLite + InMemory) and threw:

> "Services for database providers 'Sqlite', 'InMemory' have been registered in the service provider."

**The fix:** The factory now removes every service descriptor whose type name contains "EntityFrameworkCore", plus the `DbContext` and `DbContextOptions` registrations. This is a broader sweep than strictly necessary, but it's reliable:

```csharp
var descriptorsToRemove = services
    .Where(d =>
        d.ServiceType == typeof(DbContextOptions<DocuMindDbContext>) ||
        d.ServiceType == typeof(DocuMindDbContext) ||
        d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
    .ToList();
```

The `.ToList()` is important — you can't modify a collection while iterating it.

### 5. `HostAbortedException` Swallowing Startup Failures

**What happened:** The original `Program.cs` had a generic `catch (Exception ex)` that logged and returned silently. This meant that when `WebApplicationFactory` booted the app and it failed, the factory saw a clean exit instead of an exception — and reported "The server has not been started" instead of the actual error.

**The fix:** The catch was narrowed to `catch (Exception ex) when (ex is not HostAbortedException)`, and a `throw;` was added. The `HostAbortedException` filter is needed because EF Core's `dotnet ef` tooling intentionally aborts the host after extracting the service provider — that's not a real failure. Every other exception now propagates correctly, which means integration tests get real error messages.

---

## Test Results

**Unit Tests (4 passing):**
- `Document_NewInstance_HasPendingStatus` — verifies default status and timestamp
- `Conversation_NewInstance_HasCorrectDefaults` — verifies empty messages collection and timestamp
- `Message_AssistantMessage_CanTrackTokenUsage` — verifies token count properties
- `Message_UserMessage_HasNullTokenCounts` — verifies nullable fields default to null

**Integration Tests (2 passing):**
- `Health_ReturnsOk` — hits `/health`, expects 200
- `HealthDetailed_ReturnsOk_WithDatabaseStatus` — hits `/health/detailed`, expects 200 and "database" in response body

Build: **0 warnings, 0 errors.**

---

## What's Not Here Yet (and Why)

- **No CQRS command/query handlers** — The MediatR pipeline is wired up but empty. Handlers arrive in Day 2 (documents) and Day 3 (AI conversations).
- **No AI provider implementations** — The `IAIProvider` interface exists, the factory interface exists, the configuration exists. But there's no HTTP client calling Claude, OpenAI, or Gemini yet. Day 3.
- **No file upload or text extraction** — `ITextExtractor` and `ITextChunker` are defined but not implemented. Day 2.
- **No blob storage** — The `Document.BlobUrl` field is there, but nothing writes to Azure Blob Storage yet. Day 4.
- **No authentication** — The API is wide open. Auth comes in Day 5.
- **The Providers controller doesn't exist** — `/api/providers` is in the API spec but not implemented. It depends on the `IAIProviderFactory`, which has no concrete implementation yet.

The skeleton is intentionally incomplete. Every interface and DTO is designed for where the code is going, not just where it is now. The folder structure (`Features/Documents/Commands`, `Features/Conversations/Queries`, etc.) is already created and waiting for handlers.

---

## File Count

- **28 source files** across 4 projects
- **5 test files** across 2 test projects
- **7 config/devops files** (Dockerfile, docker-compose.yml, nuget.config, .gitignore, appsettings.json, appsettings.Development.json, launchSettings.json)
- **3 auto-generated migration files**
