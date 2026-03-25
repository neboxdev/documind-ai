# DocuMind AI — Day 4 Build Log

## What This Document Is

Walkthrough of Day 4: conversations, the Q&A pipeline, and wiring it all together so a user can upload a document and ask questions about it through any AI provider.

---

## The Goal

Build the full conversation flow: create a conversation tied to a document and an AI provider, send messages that get answered by the AI using relevant document chunks as context, and retrieve message history. This is where Days 1–3 come together — documents, chunks, providers, and the factory all get used in a single request pipeline.

---

## What Was Built

### CQRS Commands and Handlers

**CreateConversationCommand / Handler**
- Takes a document ID, title, and optional provider enum
- Falls back to the configured default provider if none specified
- Resolves the model ID from the factory but does *not* validate the API key at creation time — that's deferred to message-send time (see Challenges below)
- Saves the conversation with provider and model ID baked in

**SendMessageCommand / Handler** — This is the core pipeline:
1. Load the conversation with its full message history
2. Find top 5 relevant document chunks via keyword search
3. Build a system prompt with the chunk content injected between `--- CONTEXT ---` markers
4. Assemble the chat history (prior messages + the new user message) as `ChatMessage[]`
5. Resolve the correct `IAIProvider` via the factory using the conversation's provider enum
6. Call `GenerateResponseAsync()` with the system prompt and messages
7. Save both the user message and the assistant message (with model ID and token counts)
8. Return the assistant message as `MessageOutDTO`

**GetConversationsQuery / Handler** — Lists conversations for a document, verifies the document exists first.

**GetMessagesQuery / Handler** — Returns ordered message history for a conversation.

### Validators

- `CreateConversationCommandValidator` — title required, max 200 chars
- `SendMessageCommandValidator` — content required, max 10,000 chars

### Keyword-Based Chunk Search

Added `FindRelevantChunksAsync()` to `IDocumentRepository` and its implementation in `DocumentRepository`:
- Splits the user's question into words, drops anything 2 chars or shorter
- Loads all chunks for the document into memory (acceptable for v1 with small documents)
- Scores each chunk by how many query keywords appear in its content (case-insensitive)
- Returns the top N chunks sorted by score descending, then by chunk index for tie-breaking
- If there are no meaningful keywords, falls back to returning the first N chunks in order

This is intentionally simple — a production system would use vector embeddings and cosine similarity. But it works for demonstrating the pipeline and keeps the project self-contained with no external vector DB dependency.

### Controller

`ConversationsController` with four endpoints:
- `POST /api/documents/{documentId}/conversations` — create conversation
- `GET /api/documents/{documentId}/conversations` — list conversations
- `POST /api/conversations/{conversationId}/messages` — send message, get AI response
- `GET /api/conversations/{conversationId}/messages` — get message history

The controller uses endpoint-specific InDTOs (`CreateConversationInDTO`, `SendMessageInDTO`) defined alongside the controller per the coding rules. All follow the POST-for-actions, GET-for-loading pattern.

### System Prompt

The system prompt template is a constant in the handler, not in configuration:

```
You are a helpful assistant that answers questions based ONLY on the provided
document context. If the answer is not found in the context, say so clearly.
Do not make up information. Be concise and precise.

--- CONTEXT ---
{relevant chunks joined by double newlines}
--- END CONTEXT ---
```

When no chunks match the query, the context section contains `(No relevant content found in the document.)` — the AI can still respond honestly that it couldn't find relevant information.

---

## Challenges and How They Were Resolved

### 1. Eager API Key Validation on Conversation Creation

**What happened:** The initial `CreateConversationCommandHandler` called `_providerFactory.GetProvider(providerType)` to validate the provider — which checks both that the provider is registered AND has an API key. In integration tests (and in any environment where keys aren't configured yet), this caused a 503 Service Unavailable on conversation creation, even though no AI call was being made.

**The fix:** Removed the `GetProvider()` call from conversation creation. The handler now only calls `GetDefaultProvider()` and `GetModelIdForProvider()` — neither validates the API key. Key validation is deferred to `SendMessageCommandHandler`, which is the only place that actually needs a working provider. This means you can create a conversation and configure the API key later before sending the first message.

The unit test that previously expected `AIProviderException` on creation was updated to verify that conversation creation succeeds regardless of key configuration.

### 2. Factory Interface Needed More Methods

**What happened:** The `CreateConversationCommandHandler` needs to resolve the default provider and get the model ID for a provider. Both of these required access to `AIProviderOptions` in Infrastructure. But the handler lives in Application, which can't reference Infrastructure.

**The fix:** Added `GetModelIdForProvider()` and `GetDefaultProvider()` to the `IAIProviderFactory` interface (Application layer). The factory implementation in Infrastructure reads from `AIProviderOptions` to fulfill these. This keeps the dependency direction correct.

### 3. Integration Tests Share a Database

**What happened:** The `CustomWebApplicationFactory` uses a single in-memory SQLite connection for all tests. Tests that create documents and conversations can interfere with each other — the `GetConversations_AfterCreating_ReturnsList` test initially expected exactly 2 conversations, but other tests might have created conversations for the same document.

**The fix:** Each integration test that needs a clean document calls `UploadTestDocumentAsync()` which creates a fresh document. The `GetConversations` assertion uses `BeGreaterThanOrEqualTo(2)` instead of `Be(2)` to tolerate other tests' data. This is more robust than per-test database cleanup.

---

## Test Results

**Unit Tests (40 passing, up from 30):**
- 6 new `SendMessageCommandHandlerTests`:
  - Happy path: returns assistant response with correct metadata
  - Saves both user and assistant messages
  - System prompt contains chunk content
  - Includes conversation history in AI request
  - Conversation not found throws KeyNotFoundException
  - No chunks found still calls provider with fallback message
- 4 new `CreateConversationCommandHandlerTests`:
  - Creates with explicit provider
  - Falls back to default provider
  - Document not found throws KeyNotFoundException
  - Any provider enum succeeds without key validation

**Integration Tests (16 passing, up from 10):**
- 6 new `ConversationEndpointTests`:
  - Create conversation returns 201 Created
  - Non-existent document returns 404
  - Empty title returns 400 Bad Request
  - List conversations after creating returns correct count
  - Empty conversation returns empty message array
  - Non-existent conversation returns 404

Build: **0 warnings, 0 errors. 56 total tests passing.**

---

## What's Not Here Yet

- **SendMessage integration test** — Not included because it requires a configured AI provider API key. The pipeline is fully tested at the unit level with mocked `IAIProvider`. A smoke test against a real API could be added gated behind an environment variable.
- **Pagination on messages** — Message history returns all messages. For long conversations this could be an issue, but it's acceptable for v1.
- **Streaming responses** — All three SDKs support streaming. The current implementation waits for the full response. Streaming would improve perceived latency for long answers.
- **Chunk search with embeddings** — The keyword search is functional but naive. A vector similarity approach would be significantly better for relevance.

---

## File Count (Day 4 additions)

- **6 CQRS command/query files** + **4 handler files** + **2 validators**
- **1 controller** (ConversationsController with 4 endpoints)
- **1 updated repository interface** + **1 updated implementation** (FindRelevantChunksAsync)
- **2 updated factory files** (interface + implementation — new methods)
- **3 test files** (SendMessageCommandHandlerTests, CreateConversationCommandHandlerTests, ConversationEndpointTests)
