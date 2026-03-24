# DocuMind AI

A production-grade .NET 9 REST API for intelligent document analysis. Upload documents (PDF, DOCX, TXT), and query their content using natural language through your choice of AI provider — Claude, ChatGPT, or Gemini.

## Architecture

```
┌──────────────────────────────────────────────────────────┐
│                    DocuMind.API                           │
│              Controllers · Middleware · Swagger           │
├──────────────────────────────────────────────────────────┤
│                 DocuMind.Application                      │
│          CQRS Handlers · Interfaces · DTOs               │
├──────────────────────────────────────────────────────────┤
│                   DocuMind.Domain                         │
│              Entities · Enums · Value Objects             │
├──────────────────────────────────────────────────────────┤
│                DocuMind.Infrastructure                    │
│     EF Core · AI Providers · Blob Storage · Repos        │
└──────────────────────────────────────────────────────────┘
```

## Tech Stack

| Category          | Technology                              |
|-------------------|-----------------------------------------|
| Framework         | .NET 9 / ASP.NET Core 9                |
| ORM               | Entity Framework Core 9                 |
| Database          | SQLite (dev) / Azure SQL (prod)         |
| CQRS              | MediatR                                 |
| Validation        | FluentValidation                        |
| Logging           | Serilog                                 |
| AI Providers      | Claude, ChatGPT (OpenAI), Gemini        |
| Cloud             | Azure App Service + Blob Storage        |
| CI/CD             | GitHub Actions                          |

## Supported AI Providers

| Provider   | Default Model               | SDK                |
|------------|-----------------------------|--------------------|
| Claude     | claude-sonnet-4-20250514    | Anthropic          |
| OpenAI     | gpt-4o                      | OpenAI             |
| Gemini     | gemini-2.5-flash            | Google.GenAI       |

## Quick Start

```bash
git clone https://github.com/your-username/DocuMindAI.git
cd DocuMindAI
dotnet run --project src/DocuMind.API
```

The API will be available at `https://localhost:5001`. Open `/swagger` for the interactive docs.

## Configuration

Set your AI provider API keys via user secrets (development):

```bash
cd src/DocuMind.API
dotnet user-secrets set "AIProviders:Claude:ApiKey" "your-key"
dotnet user-secrets set "AIProviders:OpenAI:ApiKey" "your-key"
dotnet user-secrets set "AIProviders:Gemini:ApiKey" "your-key"
```

## Docker

```bash
docker compose up --build
```

API available at `http://localhost:5000/swagger`

## API Overview

| Method | Route                                     | Description                      |
|--------|-------------------------------------------|----------------------------------|
| POST   | /api/documents/upload                     | Upload a document                |
| GET    | /api/documents                            | List all documents               |
| GET    | /api/documents/{id}                       | Get document details             |
| DELETE | /api/documents/{id}                       | Delete a document                |
| GET    | /api/providers                            | List available AI providers      |
| POST   | /api/documents/{id}/conversations         | Start a conversation             |
| GET    | /api/documents/{id}/conversations         | List conversations               |
| POST   | /api/conversations/{id}/messages          | Ask a question                   |
| GET    | /api/conversations/{id}/messages          | Get message history              |
| GET    | /health                                   | Basic health check               |
| GET    | /health/detailed                          | Detailed health check            |

## Design Decisions

- **Multi-provider AI**: Strategy pattern + factory lets you swap between Claude, GPT, and Gemini per conversation
- **Clean Architecture**: strict dependency flow keeps business logic independent of frameworks
- **CQRS**: commands and queries are separated through MediatR for clarity and testability
- **Per-conversation provider selection**: simpler than per-message, keeps conversation history consistent

## License

MIT
