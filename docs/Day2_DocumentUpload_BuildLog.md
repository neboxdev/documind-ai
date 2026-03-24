# DocuMind AI — Day 2 Build Log

## What This Document Is

A continuation of the Day 1 build log. This covers the document upload and processing pipeline — extracting text from files, splitting it into chunks, and the full CRUD API around documents.

---

## The Goal

Take the skeleton from Day 1 and make it do something useful: accept file uploads (PDF, DOCX, TXT), extract their text content, split that text into overlapping chunks for future RAG queries, and expose a clean REST API for managing documents. By end of day, a user can upload a file, see it get processed, list their documents, and delete them.

---

## New Files Created

### Infrastructure — Text Processing

```
src/DocuMind.Infrastructure/TextProcessing/
├── PdfTextExtractor.cs       — PDF extraction via PdfPig
├── DocxTextExtractor.cs      — DOCX extraction via OpenXml
├── PlainTextExtractor.cs     — TXT extraction via StreamReader
└── TextChunker.cs            — Splits text into overlapping chunks
```

### Application — CQRS Features

```
src/DocuMind.Application/Features/Documents/
├── Commands/
│   ├── UploadDocumentCommand.cs
│   ├── UploadDocumentCommandHandler.cs
│   ├── UploadDocumentCommandValidator.cs
│   ├── DeleteDocumentCommand.cs
│   └── DeleteDocumentCommandHandler.cs
└── Queries/
    ├── GetDocumentsQuery.cs              — includes PagedResult<T>
    ├── GetDocumentsQueryHandler.cs
    ├── GetDocumentByIdQuery.cs
    └── GetDocumentByIdQueryHandler.cs
```

### API — Controller

```
src/DocuMind.API/Controllers/DocumentsController.cs
```

### Tests

```
tests/DocuMind.UnitTests/
├── TextChunkerTests.cs                     — 6 tests
├── UploadDocumentCommandValidatorTests.cs  — 5 tests
└── UploadDocumentCommandHandlerTests.cs    — 3 tests

tests/DocuMind.IntegrationTests/
└── DocumentEndpointTests.cs                — 7 tests
```

---

## Text Extraction

Each file format has its own `ITextExtractor` implementation. The interface was defined in Day 1; today was about filling it in.

**PDF (PdfPig):** Iterates through each page via `PdfDocument.Open(stream)`, calls `page.Text`, and joins them with blank lines between pages. PdfPig handles the font decoding and text layout internally. The only wrinkle: PdfPig's stable release doesn't exist for the latest .NET targets — only a `1.7.0-custom-5` prerelease was available on NuGet, so it was installed with `--prerelease`.

**DOCX (DocumentFormat.OpenXml):** Opens the document as a `WordprocessingDocument`, navigates to `MainDocumentPart.Document.Body`, and iterates `Paragraph` elements, pulling `InnerText` from each. Simple and reliable for text extraction — doesn't handle embedded images or tables, but that's out of scope for Day 2.

**TXT:** Just a `StreamReader.ReadToEndAsync`. Nothing fancy.

All three extractors are registered as `ITextExtractor` singletons in the DI container. The upload handler receives `IEnumerable<ITextExtractor>` and picks the first one where `CanHandle(contentType)` returns true. This polymorphic dispatch means adding a new format (e.g. HTML, Markdown) is just a new class — no changes to existing code.

---

## Text Chunking

The `TextChunker` splits extracted text into chunks of approximately 500 characters with 50 characters of overlap between consecutive chunks.

The algorithm:
1. Normalize whitespace (collapse runs of spaces/newlines into single spaces)
2. If the text fits in one chunk, return it as-is
3. Otherwise, walk through the text in `chunkSize` steps
4. At each step, look backwards for a sentence boundary (period + space) or newline
5. If a good break point exists in the latter two-thirds of the chunk, break there
6. Step back by `overlap` characters before starting the next chunk

The overlap exists so that RAG queries don't miss information that falls on a chunk boundary. If a sentence spans two chunks, the overlap ensures it appears fully in at least one of them.

The chunker works on character count, not tokens. Token-based chunking would be more precise for AI context windows, but character-based is simpler, doesn't require a tokenizer dependency, and is close enough — a rough 4:1 character-to-token ratio is standard for English text.

---

## The Upload Handler

`UploadDocumentCommandHandler` is the most complex piece of Day 2. The flow:

1. Create a `Document` entity with `Status = Processing`
2. Find the right `ITextExtractor` for the file's content type
3. Extract text from the uploaded stream
4. If extraction yields nothing, mark as `Failed`
5. Chunk the text via `ITextChunker`
6. Create `DocumentChunk` entities and attach them to the document
7. Set `Status = Processed`
8. Save everything (document + chunks) in a single `SaveChangesAsync` call

The entire extract-chunk-save cycle happens synchronously within the request. For small files (the 10 MB limit helps), this is fine. A future optimization would be to return `202 Accepted` and process in the background via a queue, but that's over-engineering for the current scope.

Error handling: if anything goes wrong during extraction or chunking, the catch block clears the chunks, sets `Status = Failed`, and still saves the document. This means the user can see that their upload was received but processing failed — better than a silent 500.

---

## Validation

`UploadDocumentCommandValidator` runs through MediatR's `ValidationBehavior` pipeline (set up in Day 1) before the handler executes:

- **File name** — must not be empty
- **Content type** — must be one of `application/pdf`, the DOCX MIME type, `application/msword`, or `text/plain`
- **Size** — must be greater than 0 and at most 10 MB
- **Stream** — must not be null

If any rule fails, the `ValidationBehavior` throws a `ValidationException`, which the global error handler catches and returns as a 400 with the error list.

---

## The Controller

`DocumentsController` is deliberately thin — each endpoint is 3-5 lines that construct a MediatR request, send it, and return the appropriate HTTP status:

| Endpoint | MediatR Request | Success Response |
|---|---|---|
| `POST /api/documents/upload` | `UploadDocumentCommand` | 201 Created + Location header |
| `GET /api/documents` | `GetDocumentsQuery` | 200 OK + paginated list |
| `GET /api/documents/{id}` | `GetDocumentByIdQuery` | 200 OK + document with chunk count |
| `DELETE /api/documents/{id}` | `DeleteDocumentCommand` | 204 No Content |

The upload endpoint accepts `multipart/form-data` via `IFormFile`. The `CreatedAtAction` call generates a `Location` header pointing back to the `GetById` endpoint — standard REST practice.

Pagination uses query parameters (`page` and `pageSize`, defaulting to 1 and 20). The response includes `totalCount`, `totalPages`, `page`, and `pageSize` alongside the items.

---

## Pagination

A `PagedResult<T>` generic record was introduced in the query file:

```csharp
public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

The `IDocumentRepository` got a new method `GetPagedAsync(int page, int pageSize)` that does the `Skip`/`Take` at the database level and includes chunk counts via `Include(d => d.Chunks)`.

---

## Changes to Existing Files

### Program.cs — Two significant changes

**Serilog registration switched from `UseSerilog` to `AddSerilog`:** The original Day 1 setup used `CreateBootstrapLogger()` + `builder.Host.UseSerilog(...)`. This creates a `ReloadableLogger` that gets "frozen" into a regular logger when the host builds. The problem: `WebApplicationFactory` in integration tests builds the host multiple times (once per test fixture class), and `Freeze()` can only be called once on the static `Log.Logger`. The second fixture class would throw `InvalidOperationException: The logger is already frozen.`

The fix was switching to `builder.Services.AddSerilog(...)` and `CreateLogger()` (not `CreateBootstrapLogger()`). `AddSerilog` registers Serilog as a regular DI service without the reloadable/freeze pattern. The only trade-off is that startup failures before DI is configured go to the plain static logger instead of the "bootstrap" one — but the static logger already writes to console, so nothing is lost.

**JSON enum serialization:** Added `JsonStringEnumConverter` to the controller JSON options so that enums like `DocumentStatus` serialize as `"Processed"` instead of `2`. This is better for API consumers and matches the string storage in the database.

### GlobalExceptionHandler.cs — Development error details

The generic 500 handler now checks `IHostEnvironment.IsDevelopment()` and includes the actual exception message in development mode. In production, it still returns a generic message. This was added during debugging — the original "An unexpected error occurred" message was hiding real errors during integration test development.

### Infrastructure DependencyInjection.cs — Text processing registrations

Added four new singleton registrations:
- `ITextExtractor` → `PdfTextExtractor`
- `ITextExtractor` → `DocxTextExtractor`
- `ITextExtractor` → `PlainTextExtractor`
- `ITextChunker` → `TextChunker`

Note that `ITextExtractor` has three implementations registered. When the handler asks for `IEnumerable<ITextExtractor>`, DI gives all three. The handler then picks the right one based on content type. This is the standard .NET DI pattern for strategy dispatch.

### IDocumentRepository.cs — New paged query method

Added `GetPagedAsync(int page, int pageSize)` returning a tuple of `(List<Document>, int TotalCount)`. The implementation in `DocumentRepository` uses `Include(d => d.Chunks)` so chunk counts are available without a second query.

### CustomWebApplicationFactory.cs — Complete rewrite

The Day 1 factory used `UseInMemoryDatabase` which caused constant headaches with the SQLite provider already being registered. Today it was rewritten to use an **in-memory SQLite connection** instead:

```csharp
private readonly SqliteConnection _connection;

public CustomWebApplicationFactory()
{
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();
}
```

This uses the same database provider (SQLite) as production, just with an in-memory connection string. The connection is kept open for the factory's lifetime because SQLite's in-memory databases are destroyed when the last connection closes.

This approach eliminates the dual-provider conflict entirely and gives tests a database that behaves identically to the real one — including foreign key constraints, column types, and migration behavior.

---

## Challenges and How They Were Resolved

### 1. PdfPig Has No Stable Release

**What happened:** `dotnet add package UglyToad.PdfPig` failed with "There are no stable versions available." The only version on NuGet was `1.7.0-custom-5`, a prerelease.

**The fix:** Added with `--prerelease`. PdfPig is a mature library — the version numbering is a packaging artifact, not a quality signal. The API is stable and well-documented. If a stable release appears later, the upgrade is a one-line version bump in the `.csproj`.

### 2. Serilog "Logger Is Already Frozen" in Integration Tests

**What happened:** After adding `DocumentEndpointTests` as a second `IClassFixture<CustomWebApplicationFactory>`, all integration tests started failing with `InvalidOperationException: The logger is already frozen`. The `HealthEndpointTests` fixture (from Day 1) would build the host and freeze the Serilog `ReloadableLogger`. Then `DocumentEndpointTests` would try to build a second host and call `Freeze()` again on the same static logger.

**First attempt (failed):** Override `CreateHost` in the factory and set `Log.Logger` to a fresh non-reloadable logger before `base.CreateHost()`. This didn't work because the `UseSerilog` extension in `Program.cs` still tried to freeze the original bootstrap logger.

**Second attempt (failed):** Strip all Serilog service descriptors from the container and re-add basic logging. This broke Serilog's internal dependency chain — `UseSerilog` registers internal types like `RegisteredLogger` that other Serilog services depend on. Partial removal caused `No service for type 'RegisteredLogger'` errors.

**Final fix:** Changed `Program.cs` from `CreateBootstrapLogger()` + `UseSerilog(...)` to `CreateLogger()` + `AddSerilog(...)`. The `AddSerilog` method doesn't use the reloadable logger pattern at all, so there's nothing to freeze. This is actually the more modern Serilog registration approach — the bootstrap pattern is a legacy holdover from when `IHostBuilder` and `IWebHostBuilder` were separate.

### 3. EF Core InMemory Provider vs. Multi-SaveChanges

**What happened:** The upload handler originally did:
1. `AddAsync(document)` → `SaveChangesAsync()` (INSERT)
2. Set `Status = Processing` → `SaveChangesAsync()` (UPDATE)
3. Add chunks, set `Status = Processed` → `SaveChangesAsync()` (UPDATE + INSERTs)

With the InMemory provider, step 2 failed with "Attempted to update or delete an entity that does not exist in the store." The InMemory provider has known quirks with multi-step entity tracking that real databases handle fine.

**First attempt (partial fix):** Collapsed steps 1-2 by starting with `Status = Processing` instead of `Pending`. This eliminated one round trip but didn't fully solve the problem — the InMemory provider still choked on the subsequent update after insert.

**Final fix (two parts):**
1. Restructured the handler to do a single `AddAsync` + `SaveChangesAsync` at the end, after all processing is complete. This is actually better design anyway — it's transactional. Either the document and all its chunks are saved, or nothing is.
2. Replaced the InMemory provider with in-memory SQLite (`DataSource=:memory:`) in the test factory. This uses a real relational database engine that handles entity tracking, foreign keys, and multi-step operations correctly.

### 4. Dual Database Provider Conflict

**What happened:** The `CustomWebApplicationFactory` needs to replace the production SQLite database with a test database. The naive approach — removing `DbContextOptions<DocuMindDbContext>` and adding a new one with `UseInMemoryDatabase` — causes EF Core to detect two providers (SQLite + InMemory) and throw.

A more aggressive approach — removing all service descriptors containing "EntityFrameworkCore" — fixed the dual-provider error but broke other things (EF's internal services that InMemory needs, Serilog services getting caught in the filter).

**The fix:** Stop using two different providers. The in-memory SQLite approach (`DataSource=:memory:`) means both production and test use the same SQLite provider. The factory only needs to swap out the `DbContextOptions` — a single, surgical removal. No provider conflicts, no internal service chain breakage.

### 5. Enum Serialization Mismatch

**What happened:** The integration test asserted `doc.RootElement.GetProperty("status").GetString().Should().Be("Processed")`, but the JSON had `"status": 2` (the integer value). C# enums serialize as integers by default in `System.Text.Json`.

**The fix:** Added `JsonStringEnumConverter` to the controller's JSON options in `Program.cs`. This makes all enums serialize as their string names across every endpoint. This is better for API consumers anyway — `"Processed"` is self-documenting, `2` is not.

---

## New NuGet Packages

| Package | Version | Project | Purpose |
|---|---|---|---|
| UglyToad.PdfPig | 1.7.0-custom-5 | Infrastructure | PDF text extraction |
| DocumentFormat.OpenXml | 3.5.1 | Infrastructure | DOCX text extraction |
| Microsoft.Extensions.Logging.Abstractions | 9.0.3 | Application | `ILogger<T>` interface for handlers |
| Microsoft.EntityFrameworkCore.Sqlite | 9.0.3 | IntegrationTests | In-memory SQLite for tests |
| Serilog.AspNetCore | 9.0.0 | IntegrationTests | Serilog types for test factory |

---

## Test Summary

**Unit Tests — 23 total (all passing):**

*Domain entities (4, from Day 1):*
- Document default status and timestamp
- Conversation defaults
- Message token tracking
- Message nullable fields

*Text chunker (6):*
- Empty/null input returns empty list
- Short text returns single chunk
- Long text produces multiple chunks
- Chunks respect approximate size limit
- Adjacent chunks share overlapping content
- Whitespace normalization

*Upload validator (5):*
- Valid content types (PDF, DOCX, TXT) pass
- Invalid content types (PNG, JSON, MP4) fail
- File exceeding 10 MB fails
- Empty file name fails
- Zero-size file fails

*Upload handler (3):*
- Valid file creates document with chunks and Processed status
- Unsupported content type sets Failed status
- Empty extraction result sets Failed status

**Integration Tests — 9 total (all passing):**

*Health (2, from Day 1):*
- Basic health check returns 200
- Detailed health check returns database status

*Document endpoints (7):*
- Upload valid TXT returns 201 Created with correct filename and status
- Upload unsupported type returns 400
- GET all returns paginated result
- GET nonexistent ID returns 404
- DELETE nonexistent ID returns 404
- Upload → GET by ID returns document with chunk count > 0
- Upload → DELETE → GET returns 204 then 404

Build: **0 warnings, 0 errors.**

---

## What's Not Here Yet

- **AI provider implementations** — interfaces are defined, configuration exists, but no actual Claude/OpenAI/Gemini calls. Day 3.
- **Conversations and messages** — the entities exist but there are no CQRS handlers or controller endpoints. Day 3.
- **Blob storage** — files are processed immediately and the text is stored as chunks in the database. The original file isn't persisted to Azure Blob Storage yet. Day 4.
- **Background processing** — uploads are processed synchronously in the request. For production with large files, this should be a background job. Future enhancement.
- **Swagger XML comments** — the controller has `<summary>` tags on each endpoint, but XML documentation generation isn't enabled in the `.csproj` yet. Minor gap.
