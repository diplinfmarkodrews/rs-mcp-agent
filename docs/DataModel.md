# Data Model — Lean Message + Document Entities + Session Enhancements + ConversationSaga

## Overview

The domain uses **two event-sourced aggregates** (Marten event streams) and **two CRUD Marten documents** (plain PostgreSQL JSON documents), connected by `Guid` references. A **Wolverine saga** (`ConversationSaga`) orchestrates the LLM request lifecycle including tool call confirmation, pause/resume, cancel, retry, and startup recovery. All primitive-wrapping types use **Vogen value objects** with validation or **SmartEnum** for type safety.

---

## Event-Sourced Aggregates

### SessionAggregate

Represents a chat conversation. Supports hierarchical sub-conversations via self-referencing `ParentSessionId`. Mutable fields (`Title`, `Summary`, `Rating`) evolve over time through `SessionUpdatedEvent`.

| Property | Type | Notes |
|----------|------|-------|
| Id | `Guid` | Stream identity |
| UserId | `UserId` | Vogen, not empty |
| ParentSessionId | `Guid?` | Self-ref for sub-conversations |
| Title | `string` | Updated via `SessionUpdatedEvent` |
| Summary | `string?` | Updated via `SessionUpdatedEvent` |
| Rating | `Rating?` | Vogen, 1–5 |
| StartedAt | `DateTime` | Immutable, set on creation |
| LastActivityAt | `DateTime` | Updated on every event |
| DeletedAt | `DateTime?` | Soft delete |

**Events:** `SessionCreatedEvent`, `SessionUpdatedEvent`, `SessionDeletedEvent`

### MessageAggregate

A single message within a session. Deliberately lean — tool call payloads and model config live in separate documents, referenced by ID. `Content` is nullable for pure tool-call messages. Assistant messages stream via `MessageUpdatedEvent` deltas and finalize with `MessageCompletedEvent`.

| Property | Type | Notes |
|----------|------|-------|
| Id | `Guid` | Stream identity |
| SessionId | `Guid` | FK → SessionAggregate |
| SenderId | `UserId` | Vogen, not empty |
| Content | `string?` | Nullable for tool-call-only messages; accumulated via deltas |
| Role | `ChatRole` | SmartEnum: User, Assistant, System, Tool |
| MessageType | `MessageType` | Vogen flags: TextDelta(1), TextFull(2), ToolCall(4), ToolResult(8) |
| ChatMessageId | `string?` | LLM-assigned message ID |
| AuthorName | `string?` | LLM-assigned author |
| ModelSettingsId | `Guid?` | FK → ModelSettingsDocument |
| SentAt | `DateTime` | Immutable |

**Events:** `MessageCreatedEvent`, `MessageUpdatedEvent`, `MessageCompletedEvent`

- `MessageCreatedEvent` — creates the stream. User messages are created as `TextFull`; assistant messages as `TextDelta` (incomplete).
- `MessageUpdatedEvent(string TextDelta)` — appends streaming content to `Content`.
- `MessageCompletedEvent(MessageType, ChatMessageId?, AuthorName?)` — finalizes the message. Promotes `TextDelta` to `TextFull`. Absence of this event signals an incomplete/orphaned message.

---

## CRUD Marten Documents

### ModelSettingsDocument

Immutable snapshot of AI model configuration. Deduplicated per session — a new document is only created when settings change (`EquivalentTo()` comparison). Many messages share the same `ModelSettingsId`.

| Property | Type | Notes |
|----------|------|-------|
| Id | `Guid` | Document identity |
| SessionId | `Guid` | FK → SessionAggregate |
| ServiceId | `string` | e.g. `"main"`, `"helper"` |
| ModelId | `string?` | e.g. `"claude-sonnet-4-5"` |
| IsPrivate | `bool` | Privacy flag |
| ActiveToolNames | `IReadOnlyList<string>` | Enabled tool names at send time |
| ExecutionSettings | `AiChatPromptExecutionSettings?` | Temperature, TopP, FrequencyPenalty, PresencePenalty, AllowMultipleToolCalls |
| CreatedAt | `DateTime` | Snapshot timestamp |

**Indexes:** `SessionId`

### ToolCallDocument

Tracks a single tool invocation lifecycle. Created with `Status=Requested`, transitions through confirmation and execution states. Linked to both the triggering message and the session.

| Property | Type | Notes |
|----------|------|-------|
| Id | `Guid` | Document identity |
| CallId | `string` | LLM-assigned call identifier |
| MessageId | `Guid` | FK → MessageAggregate |
| SessionId | `Guid` | FK → SessionAggregate |
| ToolName | `string` | e.g. `"BrowserTool"` |
| Arguments | `Dictionary<string, object>` | Tool call arguments |
| Status | `ToolCallStatus` | See value objects table |
| Result | `object?` | Populated on completion |
| IsError | `bool` | Error flag |
| RequestedAt | `DateTime` | Creation timestamp |
| CompletedAt | `DateTime?` | Completion timestamp |

**Indexes:** `SessionId`, `MessageId`, `CallId`

---

## Value Objects

| Type | Kind | Validation |
|------|------|------------|
| `UserId` | Vogen `string` | Not empty |
| `Rating` | Vogen `int` | 1–5 |
| `MessageType` | Vogen `int` (flags) | TextDelta(1), TextFull(2), ToolCall(4), ToolResult(8). Combinable via bitwise OR. |
| `ChatRole` | SmartEnum `int` | NotSet(0), User(1), Assistant(2), System(3), Tool(4) |
| `ToolCallStatus` | Vogen `int` | Requested(1), AwaitingCallConfirmation(2), Executing(3), AwaitingResultConfirmation(4), Completed(5), CompletedRedacted(6), Rejected(7), Failed(8), Expired(9) |
| `GaveUpReasons` | SmartEnum `int` | NotSet(0), LlmError(1), Timeout(2), MaxRetriesExceeded(3), SessionDeleted(4), ToolCallRejected(5), ToolResultRejected(6), Cancelled(7) |
| `AiChatPromptExecutionSettings` | Record | Temperature, TopP, FrequencyPenalty, PresencePenalty, AllowMultipleToolCalls |

---

## Relationships

```
SessionAggregate ←──(ParentSessionId)──→ SessionAggregate  (self-ref, sub-conversations)
SessionAggregate ←──(SessionId)───────── MessageAggregate
SessionAggregate ←──(SessionId)───────── ModelSettingsDocument
SessionAggregate ←──(SessionId)───────── ToolCallDocument
SessionAggregate ←──(Id)──────────────── ConversationSaga
MessageAggregate ←──(ModelSettingsId)──→ ModelSettingsDocument
MessageAggregate ←──(MessageId)───────── ToolCallDocument
ConversationSaga ──(ActiveMessageId)──→ MessageAggregate
ConversationSaga ──(ModelSettingsId)──→ ModelSettingsDocument
```

## Projections (inline, synchronous)

| Projection | Source Events | Target DTO |
|------------|--------------|------------|
| `ConversationProjection` | `SessionCreatedEvent`, `SessionUpdatedEvent`, `MessageCreatedEvent`, `SessionDeletedEvent` | `ConversationDto` |
| `MessageProjection` | `MessageCreatedEvent`, `MessageUpdatedEvent`, `MessageCompletedEvent` | `MessageDto` (includes `IsComplete` flag) |

**Note:** `MessageDto.IsComplete` is `true` for user messages on creation, and `true` for assistant messages only after `MessageCompletedEvent`. Incomplete messages (orphaned from interrupted streams) are filtered out by `MartenChatMessageQuery` and `IPromptBuilder`.

---

## ConversationSaga (Wolverine)

Orchestrates the full LLM request lifecycle per session. Persisted by Marten — survives restarts.

### Saga State

| Property | Type | Notes |
|----------|------|-------|
| Id | `Guid` | = SessionAggregate.Id |
| UserId | `UserId` | Session owner |
| ActiveRequestId | `Guid?` | Currently in-flight LLM request |
| ActiveMessageId | `Guid?` | Assistant message being streamed |
| ModelSettingsId | `Guid?` | Settings used for current/last request |
| LastAiChatRequest | `AiChatRequest?` | Preserved on gave-up for retry/recovery |
| PendingToolCallConfirmations | `Dictionary<Guid, Guid>` | ToolCallDocumentId → RequestId |
| PendingToolResultConfirmations | `Dictionary<Guid, Guid>` | ToolCallDocumentId → RequestId |

### Handled Messages

| Message | Action |
|---------|--------|
| `SessionCreatedEvent` | `Start` — creates saga |
| `MessageCreatedEvent` | Load settings, save user message, create assistant message, publish `LlmResponseRequestedEvent` |
| `LlmResponseCompletedEvent` | Clear all active state + `LastAiChatRequest` |
| `LlmResponseGaveUpEvent` | Clear active state, preserve `LastAiChatRequest` for retry |
| `ToolCallConfirmationRequestedEvent` | Track in `PendingToolCallConfirmations` |
| `ConfirmToolCallCommand` | Remove from pending |
| `RejectToolCallCommand` | Remove from pending, publish `LlmResponseGaveUpEvent(ToolCallRejected)` |
| `ToolResultConfirmationRequestedEvent` | Track in `PendingToolResultConfirmations` |
| `ConfirmToolResultCommand` | Remove from pending |
| `RejectToolResultCommand` | Remove from pending, publish `LlmResponseGaveUpEvent(ToolResultRejected)` |
| `RedactToolResultCommand` | Remove from pending |
| `CancelGenerationCommand` | `registry.Cancel(ActiveRequestId)` |
| `PauseGenerationCommand` | `registry.Pause(ActiveRequestId)` |
| `ResumeGenerationCommand` | `registry.Resume(ActiveRequestId)` |
| `RetryGenerationCommand` | Re-issue from `LastAiChatRequest` with new assistant message |
| `SessionDeletedEvent` | Cancel active request, `MarkCompleted()` |

### IActiveRequestRegistry (singleton)

Non-serializable runtime state for in-flight requests. Backed by `ConcurrentDictionary<Guid, ActiveRequestHandle>`.

| Method | Description |
|--------|-------------|
| `Register(requestId)` | Returns `IPausableStreamControl` (CTS + ManualResetEventSlim gate) |
| `Cancel(requestId)` | Signals CTS; unblocks gate if paused |
| `Pause(requestId)` | Resets gate — blocks stream enumeration |
| `Resume(requestId)` | Sets gate — unblocks stream enumeration |
| `Unregister(requestId)` | Cleanup on handler completion |

### Startup Recovery (`ActiveRequestRecoveryHostedService`)

On app start, queries `ConversationSaga` documents where `ActiveRequestId != null`. For each:
- Creates a new assistant `MessageAggregate` (old incomplete one filtered by `IsComplete`).
- Updates saga's `ActiveRequestId` + `ActiveMessageId`.
- Re-publishes `LlmResponseRequestedEvent` with stored `LastAiChatRequest`.

---

## Commands / Handlers

### StartChatCommand → StartChatHandler

Creates a `SessionAggregate`. Accepts optional `ParentSessionId` for sub-conversations.

### SendMessageCommand → SendMessageHandler

Creates a `MessageAggregate`. Handler orchestrates: store/reuse `ModelSettingsDocument` (dedup by value equality), store `ToolCallDocument`s (Status=Requested), then create lean `MessageAggregate` with refs. On tool-result messages, update existing `ToolCallDocument`s (Status=Completed + Result).

### GenerateLlmResponseHandler

Handles `LlmResponseRequestedEvent`. Registers with `IActiveRequestRegistry`, wraps `IAiChatClient` stream in `PausableAsyncEnumerable`, publishes `LlmTokenGeneratedEvent`, `LlmToolCallEvent`, `LlmToolResultEvent` during streaming, and `LlmResponseCompletedEvent` on success. Retries up to 3 times. Publishes `LlmResponseGaveUpEvent` on failure/cancel.

### Tool Confirmation Handlers (standalone, complement saga)

| Handler | Trigger | Action |
|---------|---------|--------|
| `PersistToolCallHandler` | `LlmToolCallEvent` | Store `ToolCallDocument`, emit `ToolCallConfirmationRequestedEvent` if not auto-confirmed |
| `ConfirmToolCallHandler` | `ConfirmToolCallCommand` | Doc → Executing, apply edited arguments |
| `RejectToolCallHandler` | `RejectToolCallCommand` | Doc → Rejected |
| `PersistToolResultHandler` | `LlmToolResultEvent` | Update doc with result, emit `ToolResultConfirmationRequestedEvent` if not auto-confirmed |
| `ConfirmToolResultHandler` | `ConfirmToolResultCommand` | Doc → Completed |
| `RejectToolResultHandler` | `RejectToolResultCommand` | Doc → Rejected |
| `RedactToolResultHandler` | `RedactToolResultCommand` | Doc → CompletedRedacted with redacted result |

### Message Lifecycle Handlers (standalone, complement saga)

| Handler | Trigger | Action |
|---------|---------|--------|
| `AppendMessageDeltaHandler` | `LlmTokenGeneratedEvent` | Loads `MessageAggregate`, calls `AppendDelta()` |
| `CompleteMessageHandler` | `LlmResponseCompletedEvent` | Loads `MessageAggregate`, calls `Complete()` — sets `IsComplete` |

---

## Design Decisions

1. **ModelSettings as document, not aggregate** — immutable snapshots with no state transitions. Event-sourcing would be YAGNI.
2. **ToolCallDocument as document, not aggregate** — lifecycle is simple CRUD (create → update with result). Queryable directly by `SessionId`/`MessageId`/`CallId` without projections.
3. **Content nullable** — assistant messages with only tool calls carry no text. Validation relaxed: only `ChatRole.User` requires non-empty content.
4. **MessageType as Vogen flags** — combinable via bitwise OR. TextDelta(1) promoted to TextFull(2) on completion. ToolCall(4) and ToolResult(8) can combine with text.
5. **Dictionary<string, object> for Arguments** — clean Marten/PostgreSQL round-trips. Supports typed values from LLM function calling.
6. **ModelSettings dedup via EquivalentTo()** — handler compares current settings against last `ModelSettingsDocument` for the session. If identical, reuses its `Id`. If different, creates a new document.
7. **ParentSessionId for sub-conversations** — self-referencing `Guid?` on `SessionAggregate`. Sub-sessions have their own message streams. Query children via `ConversationDto.ParentSessionId`.
8. **Rating as Vogen value object** — validated 1–5, type-safe throughout the stack.
9. **IsComplete signal via MessageCompletedEvent** — absence of `MessageCompletedEvent` marks an assistant message as incomplete/orphaned. `MartenChatMessageQuery` filters these out so `IPromptBuilder` never includes partial content in LLM context.
10. **IActiveRequestRegistry as singleton** — non-serializable `CancellationTokenSource` + `ManualResetEventSlim` cannot live in saga state. Keyed by `RequestId` in a `ConcurrentDictionary`.
11. **Startup recovery re-issues, not cancels** — `ActiveRequestRecoveryHostedService` creates a new assistant message and re-publishes `LlmResponseRequestedEvent`. The old incomplete message is naturally filtered by `IsComplete`.
12. **LastAiChatRequest preserved on gave-up** — enables `RetryGenerationCommand` without the user re-sending. Cleared only on `LlmResponseCompletedEvent`.

## Data model

```mermaid
graph TD
    subgraph ValueObjects["Value Objects"]
        UserId["UserId (Vogen)<br/>─<br/>string, not empty"]
        Rating["Rating (Vogen)<br/>─<br/>int, 1–5"]
        MessageType["MessageType (Vogen flags)<br/>─<br/>TextDelta(1) | TextFull(2)<br/>ToolCall(4) | ToolResult(8)"]
        ChatRole["ChatRole (SmartEnum)<br/>─<br/>NotSet | User | Assistant<br/>System | Tool"]
        ToolCallStatus["ToolCallStatus (Vogen)<br/>─<br/>Requested | AwaitingCallConfirmation<br/>Executing | AwaitingResultConfirmation<br/>Completed | CompletedRedacted<br/>Rejected | Failed | Expired"]
        GaveUpReasons["GaveUpReasons (SmartEnum)<br/>─<br/>LlmError | Timeout<br/>MaxRetriesExceeded | SessionDeleted<br/>ToolCallRejected | ToolResultRejected<br/>Cancelled"]
        ExecSettings["AiChatPromptExecutionSettings<br/>─<br/>Temperature, TopP<br/>FrequencyPenalty<br/>PresencePenalty<br/>AllowMultipleToolCalls"]
    end

    subgraph SessionAgg["SessionAggregate (event-sourced)"]
        SA["SessionAggregate<br/>─<br/>UserId: UserId<br/>ParentSessionId: Guid?<br/>Title: string<br/>Summary: string?<br/>Rating: Rating?<br/>StartedAt, LastActivityAt<br/>DeletedAt?"]
        SCE["SessionCreatedEvent<br/>─<br/>Id, UserId, ParentSessionId?"]
        SUE["SessionUpdatedEvent<br/>─<br/>Title?, Summary?, Rating?"]
        SDE["SessionDeletedEvent<br/>─<br/>Id"]
        SA --- SCE & SUE & SDE
    end

    subgraph MessageAgg["MessageAggregate (event-sourced)"]
        MA["MessageAggregate<br/>─<br/>SenderId: UserId<br/>SessionId: Guid<br/>Content: string?<br/>Role: ChatRole<br/>MessageType: MessageType<br/>ChatMessageId: string?<br/>AuthorName: string?<br/>ModelSettingsId: Guid?<br/>SentAt: DateTime"]
        MCE["MessageCreatedEvent"]
        MUE["MessageUpdatedEvent<br/>─<br/>TextDelta"]
        MCOE["MessageCompletedEvent<br/>─<br/>MessageType, ChatMessageId?<br/>AuthorName?"]
        MA --- MCE & MUE & MCOE
    end

    subgraph SagaState["ConversationSaga (Wolverine)"]
        CS["ConversationSaga<br/>─<br/>Id: Guid (= SessionId)<br/>UserId: UserId<br/>ActiveRequestId: Guid?<br/>ActiveMessageId: Guid?<br/>ModelSettingsId: Guid?<br/>LastAiChatRequest: AiChatRequest?<br/>PendingToolCallConfirmations<br/>PendingToolResultConfirmations"]
    end

    subgraph RuntimeServices["Runtime Services (singleton)"]
        ARR["IActiveRequestRegistry<br/>─<br/>Register / Cancel / Pause<br/>Resume / Unregister"]
        PSC["IPausableStreamControl<br/>─<br/>CancellationToken<br/>Pause() / Resume()"]
        ARR --- PSC
    end

    subgraph Documents["Marten Documents (CRUD)"]
        MSD["ModelSettingsDocument<br/>─<br/>Id: Guid<br/>SessionId: Guid<br/>ServiceId: string<br/>ModelId: string?<br/>IsPrivate: bool<br/>ActiveToolNames: IReadOnlyList‹string›<br/>ExecutionSettings:<br/>AiChatPromptExecutionSettings<br/>CreatedAt: DateTime<br/>─<br/>EquivalentTo() for dedup"]
        TCD["ToolCallDocument<br/>─<br/>Id: Guid<br/>CallId: string<br/>MessageId: Guid<br/>SessionId: Guid<br/>ToolName: string<br/>Arguments: Dict‹string,object›<br/>Status: ToolCallStatus<br/>Result: object?<br/>IsError: bool<br/>RequestedAt: DateTime<br/>CompletedAt: DateTime?"]
    end

    subgraph Projections["Marten Projections (inline)"]
        CP["ConversationProjection<br/>→ ConversationDto"]
        MP["MessageProjection<br/>→ MessageDto (+ IsComplete)"]
    end

    subgraph DTOs["Application DTOs"]
        ConvDto["ConversationDto<br/>─<br/>Id, UserId, Title<br/>ParentSessionId?<br/>Summary?, Rating?<br/>StartedAt, LastActivityAt<br/>Closed, Version"]
        MsgDto["MessageDto<br/>─<br/>Id, SessionId, SenderId<br/>Content?, Role, MessageType<br/>ChatMessageId?, AuthorName?<br/>ModelSettingsId?, IsComplete<br/>SentAt, Version"]
    end

    subgraph CQRS["Commands / Handlers"]
        SCC["StartChatCommand"]
        SMC["SendMessageCommand"]
        CGC["CancelGenerationCommand"]
        PGC["PauseGenerationCommand"]
        RGC["ResumeGenerationCommand"]
        RTG["RetryGenerationCommand"]
        SCH["StartChatHandler"]
        SMH["SendMessageHandler"]
        GLR["GenerateLlmResponseHandler"]
        SCC --> SCH
        SMC --> SMH
        CGC & PGC & RGC & RTG --> CS
    end

    SA -.->|self-ref| SA
    MA -.->|SessionId ref| SA
    MA -.->|ModelSettingsId ref| MSD
    TCD -.->|MessageId ref| MA
    TCD -.->|SessionId ref| SA
    MSD -.->|SessionId ref| SA
    MSD -.->|contains| ExecSettings
    CS -.->|Id = SessionId| SA
    CS -.->|ActiveMessageId| MA
    CS -.->|ModelSettingsId| MSD
    GLR -.->|uses| ARR

    SA -.->|uses| UserId & Rating
    MA -.->|uses| UserId & ChatRole & MessageType

    SCE & SUE & SDE -->|drives| CP --> ConvDto
    MCE & MUE & MCOE -->|drives| MP --> MsgDto

    SCH --> SA
    SMH --> MA
    SMH -.->|stores| MSD & TCD

    style ValueObjects fill:#2d2d44,stroke:#a78bfa,color:#eee
    style SessionAgg fill:#1a1a2e,stroke:#e94560,color:#eee
    style MessageAgg fill:#1a1a2e,stroke:#e94560,color:#eee
    style SagaState fill:#1a2e1a,stroke:#4ade80,color:#eee
    style RuntimeServices fill:#2e1a2e,stroke:#f472b6,color:#eee
    style Documents fill:#1e3a2f,stroke:#4ade80,color:#eee
    style Projections fill:#16213e,stroke:#0f3460,color:#eee
    style DTOs fill:#0f3460,stroke:#533483,color:#eee
    style CQRS fill:#16213e,stroke:#e94560,color:#eee
```

## Conversation flow

```mermaid
flowchart TD
    A[SessionCreatedEvent] -->|Start saga| B[ConversationSaga created]

    B --> C[MessageCreatedEvent]
    C --> C1{Role == User?}
    C1 -->|No| SKIP[Return]
    C1 -->|Yes| C2[Load ModelSettingsDocument]
    C2 --> C3[Build AiChatSettings + AiChatRequest]
    C3 --> C4[Save user MessageAggregate]
    C4 --> C5[Store LastAiChatRequest + ModelSettingsId in saga]
    C5 --> C6[Create new assistant MessageAggregate + Save]
    C6 --> C7[Set ActiveRequestId + ActiveMessageId]
    C7 --> C8[Publish LlmResponseRequestedEvent]

    C8 --> D[GenerateLlmResponseHandler]
    D --> D0[registry.Register requestId → IPausableStreamControl]
    D0 --> D1[Stream IAiChatClient wrapped in PausableAsyncEnumerable]

    D1 --> D2{Update type?}
    D2 -->|TextDelta| D3[Publish LlmTokenGeneratedEvent]
    D3 --> D3H[AppendMessageDeltaHandler persists delta]
    D3H --> D2

    D2 -->|ToolCall| D4[Publish LlmToolCallEvent]
    D4 --> E[PersistToolCallHandler]
    E --> E1{AutoConfirm?}
    E1 -->|Yes| E2[Doc → Executing]
    E1 -->|No| E3[Doc → AwaitingCallConfirmation]
    E3 --> E4[Emit ToolCallConfirmationRequestedEvent]
    E4 --> F[Saga: add to PendingToolCallConfirmations]
    F --> G{User decision}
    G -->|ConfirmToolCallCommand| G1[Saga: remove from pending]
    G1 --> G1H[ConfirmToolCallHandler: Doc → Executing]
    G -->|RejectToolCallCommand| G2[Saga: remove from pending]
    G2 --> G2A[Publish LlmResponseGaveUpEvent ToolCallRejected]
    G2A --> G2H[RejectToolCallHandler: Doc → Rejected]

    D2 -->|ToolResult| H[Publish LlmToolResultEvent]
    H --> I[PersistToolResultHandler]
    I --> I1{AutoConfirm?}
    I1 -->|Yes| I2[Doc → Completed]
    I1 -->|No| I3[Doc → AwaitingResultConfirmation]
    I3 --> I4[Emit ToolResultConfirmationRequestedEvent]
    I4 --> J[Saga: add to PendingToolResultConfirmations]
    J --> K{User decision}
    K -->|ConfirmToolResultCommand| K1[Saga: remove from pending]
    K1 --> K1H[ConfirmToolResultHandler: Doc → Completed]
    K -->|RejectToolResultCommand| K2[Saga: remove from pending]
    K2 --> K2A[Publish LlmResponseGaveUpEvent ToolResultRejected]
    K2A --> K2H[RejectToolResultHandler: Doc → Rejected]
    K -->|RedactToolResultCommand| K3[Saga: remove from pending]
    K3 --> K3H[RedactToolResultHandler: Doc → CompletedRedacted]

    D2 -->|Stream ends| L[Publish LlmResponseCompletedEvent]
    L --> LH[CompleteMessageHandler: MessageAggregate.Complete → IsComplete=true]
    L --> M[Saga: clear ActiveRequestId, ActiveMessageId, LastAiChatRequest, pending]

    D -->|Error/Timeout| N[Publish LlmResponseGaveUpEvent]
    N --> O[Saga: clear ActiveRequestId, ActiveMessageId, pending]
    O --> O1[LastAiChatRequest preserved for retry]

    subgraph Cancel
        CA[CancelGenerationCommand] --> CB[Saga: registry.Cancel ActiveRequestId]
        CB --> CC[CTS cancelled → stream aborts]
        CC --> CD[Handler catches OperationCanceledException]
        CD --> CE[Publish LlmResponseGaveUpEvent Cancelled]
    end

    subgraph Pause / Resume
        PA[PauseGenerationCommand] --> PB[Saga: registry.Pause ActiveRequestId]
        PB --> PC[PausableAsyncEnumerable gate blocks]
        PD[ResumeGenerationCommand] --> PE[Saga: registry.Resume ActiveRequestId]
        PE --> PF[Gate opens → stream continues]
    end

    subgraph Retry
        RA[RetryGenerationCommand] --> RB{ActiveRequestId == null AND LastAiChatRequest != null?}
        RB -->|Yes| RC[Create new assistant MessageAggregate]
        RC --> RD[Set new ActiveRequestId + ActiveMessageId]
        RD --> RE[Publish LlmResponseRequestedEvent]
        RB -->|No| RF[Return - nothing to retry]
    end

    subgraph Startup Recovery
        SR1[ActiveRequestRecoveryHostedService starts]
        SR1 --> SR2[Query ConversationSaga where ActiveRequestId != null]
        SR2 --> SR3{LastAiChatRequest != null?}
        SR3 -->|Yes| SR4[Create new assistant MessageAggregate]
        SR4 --> SR5[Update saga: new RequestId + MessageId]
        SR5 --> SR6[Re-publish LlmResponseRequestedEvent]
        SR6 --> SR7[Old incomplete message filtered by IsComplete]
        SR3 -->|No| SR8[Clear ActiveRequestId only]
    end

    DEL[SessionDeletedEvent] --> DEL1[Saga: registry.Cancel ActiveRequestId]
    DEL1 --> DEL2[Publish LlmResponseGaveUpEvent SessionDeleted]
    DEL2 --> DEL3[MarkCompleted - saga ends]
```