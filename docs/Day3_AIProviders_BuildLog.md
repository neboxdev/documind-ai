# DocuMind AI — Day 3 Build Log

## What This Document Is

Walkthrough of Day 3: building the multi-provider AI abstraction layer. Three providers (Claude, OpenAI, Gemini), a factory to resolve them, a CQRS query to list them, and tests to make sure the wiring holds together.

---

## The Goal

Make it so any part of the application can call an AI provider through a single interface — `IAIProvider.GenerateResponseAsync()` — and the caller never has to know whether it's talking to Claude, GPT, or Gemini underneath. The factory resolves the right one based on the enum, and the configuration holds the API keys and default models.

---

## What Was Built

### Provider Implementations

Three classes in `Infrastructure/AI/Providers/`, one per provider:

**ClaudeAIProvider** — Uses the `Anthropic` NuGet SDK (v12.9.0)
- Creates an `AnthropicClient` with the API key from configuration
- Maps our normalized `AIRequestInDTO` messages to `MessageParam` objects with `"user"` / `"assistant"` roles
- Calls `client.Messages.Create()` with `MessageCreateParams`
- Extracts the response text using `block.TryPickText()` on the content blocks — the Anthropic SDK uses a discriminated union pattern for content blocks, not inheritance
- Returns token counts from `response.Usage.InputTokens` and `OutputTokens`

**OpenAIProvider** — Uses the `OpenAI` NuGet SDK (v2.9.1)
- Creates a `ChatClient(model, apiKey)` per request
- Maps messages to `SystemChatMessage`, `UserChatMessage`, `AssistantChatMessage` — OpenAI SDK has a class hierarchy for message types
- Uses an explicit type alias (`using OpenAIChatMessage = OpenAI.Chat.ChatMessage`) to avoid collision with our own `ChatMessage` DTO class
- Calls `CompleteChatAsync()` with `ChatCompletionOptions` for temperature and max tokens
- Token usage from `completion.Usage.InputTokenCount` and `OutputTokenCount`

**GeminiAIProvider** — Uses the `Google_GenerativeAI` NuGet SDK (v3.6.3)
- Creates a `GenerativeModel` with API key, model ID, and system instruction
- Uses Gemini's `StartChat()` / `ChatSession` pattern for conversation history
- Replays prior history messages through the chat session, then sends the final user message
- Response text via `response.Text`, token counts from `response.UsageMetadata`

### AIProviderFactory

In `Infrastructure/AI/Factory/AIProviderFactory.cs`:

- Receives all registered `IAIProvider` instances via `IEnumerable<IAIProvider>` DI injection
- Builds a dictionary keyed by `AIProvider` enum for O(1) lookup
- `GetProvider()` validates both that the provider is registered AND has an API key
- `GetAvailableProviders()` returns only providers with configured keys
- `GetAvailableProviderDetails()` returns full DTOs (name, model, isDefault) — this was added to keep the Application layer clean (it shouldn't reference Infrastructure config classes)
- Logs available providers at startup as a diagnostic aid

### DI Registration

`AddAIProviders()` extension method in `Infrastructure/DependencyInjection.cs`:
- Binds `AIProviderOptions` from the `"AIProviders"` config section
- Registers all three providers as `IAIProvider` singletons
- Registers `AIProviderFactory` as `IAIProviderFactory` singleton
- Called from `AddInfrastructure()` automatically

### CQRS Query

`GetAvailableProvidersQuery` / `GetAvailableProvidersQueryHandler`:
- Returns `ProviderOutDTO[]` — name, default model ID, and isDefault flag
- Delegates entirely to `IAIProviderFactory.GetAvailableProviderDetails()`

### ProvidersController

`GET /api/providers` — returns the available providers list. Simple controller, one method, null check on the mediator DI.

---

## Challenges and How They Were Resolved

### 1. SDK APIs Didn't Match Documentation Guesses

**What happened:** The initial provider implementations were written based on inferred API shapes — class names like `AnthropicClient.Options`, `MessageRole.User`, `CreateMessageParams`, and OpenAI's `List<ChatMessage>` being assignable to `IEnumerable<ChatMessage>`. None of these compiled. The SDKs had evolved significantly from what public documentation suggested.

**The fix:** Created a throwaway console project that used reflection to inspect every constructor, property, and method on the actual types loaded from the installed DLLs. This gave the exact API surface:
- Anthropic: `AnthropicClient` takes no constructor args (just set `.ApiKey`), uses `MessageCreateParams` (not `CreateMessageParams`), roles are plain strings not enums
- OpenAI: `ChatMessage` is a base class, subclasses are `SystemChatMessage`/`UserChatMessage`/`AssistantChatMessage`, the list needs explicit casting to `IEnumerable<ChatMessage>`
- Gemini: `GenerativeModel` constructor takes `(apiKey, model, ...)` with named parameters

### 2. Anthropic SDK's Discriminated Union for Content Blocks

**What happened:** The response from Claude's `Messages.Create()` returns `IReadOnlyList<ContentBlock>`, but `ContentBlock` is not a base class for `TextBlock` — you can't pattern-match with `is TextBlock`. It's a discriminated union wrapper type that uses `TryPickText(out TextBlock)` to extract the concrete variant.

**The fix:** Changed from:
```csharp
if (block is TextBlock textBlock) // doesn't compile
```
to:
```csharp
if (block.TryPickText(out var textBlock)) // correct API
```

This was only discoverable through the reflection inspection.

### 3. ChatMessage Name Collision Between OpenAI SDK and Our DTOs

**What happened:** Both `OpenAI.Chat` and `DocuMind.Application.DTOs` export a type called `ChatMessage`. Since the OpenAI provider file imports both namespaces, the compiler couldn't resolve the ambiguity — `CS0104: 'ChatMessage' is an ambiguous reference`.

**The fix:** Added a using alias:
```csharp
using OpenAIChatMessage = OpenAI.Chat.ChatMessage;
```
Then used `List<OpenAIChatMessage>` for the message list and `(IEnumerable<OpenAIChatMessage>)messages` for the explicit cast needed by `CompleteChatAsync()`.

### 4. Application Layer Referencing Infrastructure Types

**What happened:** The first version of `GetAvailableProvidersQueryHandler` imported `AIProviderOptions` from `DocuMind.Infrastructure.AI.Configuration` and `IOptions<T>` from `Microsoft.Extensions.Options`. This violated Clean Architecture — the Application layer must not reference Infrastructure.

**The fix:** Added `GetAvailableProviderDetails()` to the `IAIProviderFactory` interface (which lives in Application). The factory implementation in Infrastructure does the config lookup. The handler now only depends on `IAIProviderFactory` — a clean Application-layer interface.

---

## Test Results

**Unit Tests (30 passing, up from 23):**
- 7 new `AIProviderFactoryTests`:
  - `GetProvider_WithConfiguredKey_ReturnsProvider` — happy path
  - `GetProvider_WithoutKey_ThrowsAIProviderException` — registered but unconfigured
  - `GetProvider_UnregisteredProvider_ThrowsAIProviderException` — not registered at all
  - `GetAvailableProviders_ReturnsOnlyConfiguredProviders` — 2 of 3 with keys
  - `GetAvailableProviders_NoKeysConfigured_ReturnsEmpty` — zero keys
  - `GetAvailableProviderDetails_ReturnsCorrectDTOs` — model IDs and default flag
  - `GetAvailableProviderDetails_AllKeysConfigured_ReturnsAll` — all 3 with keys

**Integration Tests (10 passing, up from 9):**
- 1 new `ProviderEndpointTests`:
  - `GetProviders_ReturnsOk` — verifies endpoint works even with no keys configured

Build: **0 warnings, 0 errors.**

---

## What's Not Here Yet

- **Polly retry policies** — The spec mentioned retry for HTTP 429/503. The providers currently don't have retry logic. This should be added by wrapping the HttpClient each SDK uses, or by using a retry loop around the provider call. Deferred to avoid over-engineering the foundation when we can't easily test retries without real API calls.
- **No real AI calls in tests** — All tests use mocked providers. Smoke tests against real APIs are gated behind environment variables (not implemented yet).
- **Gemini chat history replay** — The current approach replays all history messages through `GenerateContentAsync()` which burns tokens and is slow. A production version would use Gemini's native history parameter. Acceptable for v1.

---

## File Count (Day 3 additions)

- **3 provider implementations** (ClaudeAIProvider, OpenAIProvider, GeminiAIProvider)
- **1 factory** (AIProviderFactory)
- **1 CQRS query + handler** (GetAvailableProvidersQuery)
- **1 controller** (ProvidersController)
- **1 updated DI registration** (AddAIProviders extension)
- **2 test files** (AIProviderFactoryTests, ProviderEndpointTests)
- **3 NuGet packages** (Anthropic 12.9.0, OpenAI 2.9.1, Google_GenerativeAI 3.6.3)
