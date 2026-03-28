# AGENTS.md

## Architecture Overview

.NET 9 / Aspire solution: a **Blazor Server AI chat app** (`RSChatApp.Web`) backed by a **Model Context Protocol server** (`RsMcpServer.Api`) that exposes ReportServer (Java/GWT BI platform) capabilities as AI tools.

```
RSChatApp.AppHost        ← .NET Aspire orchestrator (Qdrant, PostgreSQL, Ollama, both services)
├── RSChatApp.Web        ← Blazor Server UI, Semantic Kernel, MCP client, chat/terminal/browser UI
├── RsMcpServer.Api      ← MCP server: exposes TerminalTool + TerminalResource over SSE/HTTP + REST
├── RSChatApp.Common     ← BaseAggregate, BaseEvent, Result<T>, IEventStoreRepository<T>
├── RSChatApp.ReportServer  ← IReportServerClient abstraction + GWT RPC client impl
└── RSChatApp.RSChatApp
    ├── Application      ← CQRS handlers (static), feature folders
    ├── Domain           ← Aggregates + domain events
    ├── Infrastructure   ← Marten/PostgreSQL event store, Wolverine config, projections, prompts
    └── Shared.Infrastructure.Mcp ← BrowserTool, SemanticSearchTool, ScriptStoreTool, ingestion, chat clients
```

## Running the Solution

```bash
# Full stack via Aspire (preferred)
dotnet run --project src/RSChatApp.AppHost/RSChatApp.AppHost

# Individual services (requires Qdrant + ReportServer running separately)
dotnet run --project src/RSChatApp.RsMcpServer/RsMcpServer.Api        # :5002
dotnet run --project src/RSChatApp.RSChatApp/RSChatApp.Web             # :5008
```

API keys are referenced by name in `appsettings.json` (e.g. `"ApiKey": "ANTHROPIC_KEY"`) and resolved from environment variables via `openAiSettings.SetApiKey()` — set `ANTHROPIC_KEY` (or equivalent) in the environment.

## Key Patterns

### MCP Tool Registration
Tools are decorated with `[McpServerTool, Description("...")]` (and optionally `[KernelFunction]`). Two registration paths:
- **RsMcpServer.Api**: registers via `.AddMcpServer().WithTools<TerminalTool>().WithResources<TerminalResource>().WithHttpTransport()`. Also exposes REST endpoints via `MapRsRestEndpoints()` at `/api/rs-rest/terminal/*`.
- **RSChatApp.Web**: at startup, connects to RsMcpServer over SSE, fetches tools with `mcpClient.ListToolsAsync()`, and registers them as `KernelFunction`s into a singleton `KernelPluginCollection`. Web-local tools (`BrowserTool`, `AuthenticationTool`, `UserConfirmedTerminalTool`, `SemanticSearchTool`, `DocumentLookupTool`, `ScriptStoreTool`) are registered via `ToolCollectionService`. The `Kernel` is **scoped** (per Blazor circuit), wrapping the singleton plugin collection.
- **External MCP clients**: additional stdio-based MCP servers can be configured via `McpClientSettings:Clients` in `appsettings.json` (e.g. SequentialThinking). Tools from these are registered into the shared `KernelPluginCollection`.

`ToolCollectionService` groups all tools by category ("Knowledge Base", "File Store", "TerminalTool", plus Kernel plugins). `IToolDescriptor` implementations provide per-tool UI metadata (icons, display names, permissions). `ToolSelectionStorage` persists per-user tool enable/disable to browser storage.

`IFunctionInvocationFilter` implementations (`UserConfirmToolCallInvocationFilter`, `UserConfirmToolResultInvocationFilter`) gate tool execution behind user confirmation via `IWaitForUserInteraction<TRequest, TResult>`.

### Event Sourcing (Marten + Wolverine)
Aggregates extend `BaseAggregate`; use `ApplyAndEnqueue(event, applyAction)`:
```csharp
var @event = MessageCreatedEvent.Create(...);
message.ApplyAndEnqueue(@event, e => message.Apply((MessageCreatedEvent)e));
```
Command handlers are static methods; Wolverine discovers them via assembly scanning. Marten uses PostgreSQL (Aspire connection `"postgres"`) with inline projections (`ConversationProjection`, `MessageProjection`) and `HotCold` async daemon for event forwarding. `ConversationSaga` exists but is currently commented out (not in use).

### AI Client Keys
Three keyed `IChatClient` instances resolved via `IChatClientFactory.Create(key)`:
- `ChatClientServiceKeys.MainModel` (`"main"`) — OpenAI/Anthropic or Ollama, used for primary chat
- `ChatClientServiceKeys.HelperModel` (`"helper"`) — Ollama, used for suggestions (`ChatSuggestions`)
- `ChatClientServiceKeys.VisionLargeModel` (`"vision-large"`) — Ollama, vision tasks

The `OpenAISettings.Model` field determines provider: if set (e.g. `"claude-sonnet-4-5"`), uses OpenAI-compatible API with Anthropic base URL; otherwise falls back to Ollama `"chat"` model.

### Result Pattern
All ReportServer client operations return `Result<T>` (from `RSChatApp.Common`):
```csharp
var result = await _reportServer.AuthenticateAsync(user, pass);
if (result.IsSuccess) { /* result.Data */ } else { /* result.Error / result.Message */ }
```

### Prompts (Hot-Reloadable)
System and suggestion prompts live in `RSChatApp.Web/Prompts/SystemPrompt.md` and `SuggestionPrompt.md`. `PromptFileStore` watches them via `IFileProvider` change tokens — edits are picked up without restart. Accessed via `IPromptService.GetPrompt(new SystemPromptRequest(...))`.

### Browser Storage
Chat history is persisted to browser protected storage (LocalStorage in dev, SessionStorage in prod). `ChatHistoryStorage` manually serializes with `ChatMessageConverter` because `ProtectedBrowserStorage` bypasses global `JsonSerializerOptions`.

### Data Ingestion
`DataIngestor` runs at startup, ingesting PDFs and `.txt` files from `wwwroot/Data` into Qdrant collections (`data-rschatapp-chunks`, `data-rschatapp-documents`). RS scripts are served as static files via the `StaticContent:Sources` configuration (path: `scripts/rs-scripts`).

## Tests

```bash
dotnet test tests/TestRsMcpServer
```

Uses MSTest + xUnit hybrid. `DistributedServicesFixture` starts both `RSChatApp.Web` and `RsMcpServer.Api` in-process via `WebApplicationFactory` for integration tests. The `RsMcpServer.Api` `Program` is made accessible via `[assembly: InternalsVisibleTo("TestRsMcpServer")]`.

## Key Files

| Path | Purpose |
|------|---------|
| `src/RSChatApp.AppHost/.../Program.cs` | Aspire wiring: Qdrant, PostgreSQL, Ollama models, service references |
| `src/RSChatApp.RSChatApp/RSChatApp.Web/Program.cs` | Full DI setup for the chat app, MCP client bootstrap |
| `src/RSChatApp.RsMcpServer/RsMcpServer.Api/Program.cs` | MCP server setup, Keycloak + legacy auth, REST endpoints |
| `src/.../RSChatApp.Shared.Infrastructure.Mcp/Browser/Mcp/BrowserTool.cs` | Playwright tool (~582 lines) |
| `src/.../RSChatApp.Shared.Infrastructure.Mcp/ReportServer/Mcp/TerminalTool.cs` | RS terminal MCP tool |
| `src/.../RSChatApp.Shared.Infrastructure.Mcp/StaticFileContent/Mcp/ScriptStoreTool.cs` | File store tool (scripts + skills) |
| `src/.../RSChatApp.Infrastructure/Extensions/DependencyInjection.cs` | Marten/Wolverine/PostgreSQL config |
| `src/.../RSChatApp.Web/Extensions/DependencyInjection.cs` | Tool collection, OpenAI client, logger setup |
| `src/.../RSChatApp.Web/Prompts/SystemPrompt.md` | Editable system prompt (hot-reload) |
| `src/RSChatApp.Common/RSChatApp.Common.Kernel/BaseAggregate.cs` | Event sourcing base class |

