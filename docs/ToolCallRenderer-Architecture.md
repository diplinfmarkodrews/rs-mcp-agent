# ToolCall Renderer Architecture

## Overview

The ToolCall Renderer is a comprehensive system for displaying AI tool invocations and their results in the RSChatApp. It transforms raw `ChatMessage` objects containing `FunctionCallContent` and `FunctionResultContent` into a rich, interactive UI with tool grouping, type-specific rendering, and result visualization.

## Architecture Principles

1. **Separation of Concerns**: Processing logic (ToolCallProcessor) is separate from rendering (Razor components)
2. **Type-based Routing**: Tool types and result content types determine which specialized renderer to use
3. **Extensibility**: New tool types can be added by creating new descriptors and renderers
4. **Streaming Support**: Handles both live streaming responses and stored historical data
5. **Metadata-driven UI**: Tool descriptors provide icons, colors, permissions, and display names

---

## Core Components

### 1. Data Processing Layer

#### ToolCallProcessor
**Purpose**: Transforms `ChatMessage` with AI content into structured `ProcessedMessage` for rendering

**Key Responsibilities**:
- Extracts `TextContent`, `FunctionCallContent`, and `FunctionResultContent` from ChatMessage
- Creates `ToolInvocation` objects from function calls using ToolRegistry
- Creates `ToolResult` objects from function results with content type detection
- Groups consecutive tool calls of the same type into `ToolGroup` instances

**Flow**:
```
ChatMessage → ProcessMessage() → ProcessedMessage
  ├─ TextContent → Consolidated string
  ├─ FunctionCallContent → ToolInvocation
  └─ FunctionResultContent → ToolResult
```

**Content Type Detection**:
- Search results: Detects `<citation>` or `<result>` XML tags → `ResultContentType.SearchCitations`
- Browser screenshots: Detects JSON with "image" property → `ResultContentType.Image`
- JSON data: Valid JSON structure → `ResultContentType.Json`
- Default: → `ResultContentType.Text`

**Error Detection**:
- Skips error keyword detection for structured content types (SearchCitations, Json, Image)
- Checks for "error", "exception", "failed" keywords in plain text

---

### 2. Data Models

#### ProcessedMessage
```csharp
record ProcessedMessage(
    ChatMessage OriginalMessage,
    string TextContent,
    List<ToolGroup> ToolGroups
)
```
**Purpose**: Structured representation of assistant message for rendering

#### ToolGroup
```csharp
class ToolGroup {
    ToolType Type
    List<ToolInvocation> Invocations
    List<ToolResult?> Results
    bool IsCollapsed
}
```
**Purpose**: Groups related tool calls (same type, consecutive) for organized display

#### ToolInvocation
```csharp
record ToolInvocation(
    string CallId,
    ToolType Type,
    string RawName,
    string DisplayName,
    IReadOnlyDictionary<string, object?> Parameters,
    ToolMetadata Metadata,
    ToolPermissions Permissions
)
```
**Purpose**: Represents a tool call with rich metadata

#### ToolResult
```csharp
record ToolResult(
    string CallId,
    bool IsSuccess,
    ResultContentType ContentType,
    object? Data,
    string? ErrorMessage,
    DateTime CompletedAt
)
```
**Purpose**: Represents tool execution result with typed data

#### ToolType (Enum)
- Unknown, Search, TerminalExecute, BrowserExecute, BrowserNavigate, BrowserScreenshot, FileRead, FileWrite, FileList, ApiRequest

#### ResultContentType (Enum)
- Text, Json, Image, Html, Error, SearchCitations

#### ToolMetadata
```csharp
record ToolMetadata(string? SessionId, DateTime Timestamp, string? TargetInfo)
```

#### ToolPermissions
```csharp
record ToolPermissions(bool CanRerun, bool CanEditResult, bool CanCopy, bool CanExpand)
```

---

### 3. Tool Registry System

#### ToolRegistry
**Purpose**: Central registry mapping tool names to descriptors

**Key Methods**:
- `RegisterDescriptor(descriptor, ...toolNames)`: Register a descriptor with multiple name aliases
- `GetDescriptor(string toolName)`: Get descriptor by function name (with normalization)
- `GetDescriptor(ToolType type)`: Get descriptor by type

**Name Normalization**:
- Removes namespace prefixes (e.g., "terminal.executeCommand" → "executecommand")
- Removes "Async" suffix
- Converts to lowercase alphanumeric only

#### IToolDescriptor
**Interface for tool-specific metadata**:
```csharp
interface IToolDescriptor {
    ToolType Type { get; }
    string GetDisplayName(parameters)
    ToolPermissions GetPermissions(parameters)
    ToolMetadata ExtractMetadata(parameters)
    string GetIconSvg()
    string GetColorClass()
}
```

**Built-in Descriptors**:
- `SearchToolDescriptor`: Search icon, green color, extracts search phrase
- `TerminalToolDescriptor`: Terminal icon, blue color, extracts command
- `BrowserToolDescriptor`: Browser icon, purple color, extracts script/URL
- `UnknownToolDescriptor`: Fallback for unrecognized tools

---

### 4. Rendering Layer (Blazor Components)

#### Component Hierarchy
```
ChatMessageItem
  └─ AssistantMessageView
       ├─ (Text content - Markdown rendering)
       └─ ToolGroupCard (for each ToolGroup)
            ├─ ToolGroupHeader
            └─ ToolInvocationCard (for each invocation-result pair)
                 ├─ ToolCallView (invocation)
                 └─ ToolResultView (result)
                      └─ Result Renderers (type-specific)
```

#### ChatMessageItem.razor
**Entry Point**: Receives `ChatMessage`, determines role

**Responsibilities**:
- Calls `ToolCallProcessor.ProcessMessage()` for Assistant messages
- Routes to UserMessageView or AssistantMessageView
- Manages InProgress state for streaming

**Parameters**:
- `Message`: ChatMessage
- `InProgress`: bool
- `OnToolRerun`: EventCallback<ToolRerunRequest>
- `OnEditInEditor`: EventCallback<EditInEditorRequest>

#### AssistantMessageView.razor
**Assistant Message Container**

**Renders**:
1. Text content (if any) - with Markdown support
2. Each ToolGroup via ToolGroupCard
3. Debug info (currently shows tool group count)

**Layout**: Vertical stack with spacing

#### ToolGroupCard.razor
**Tool Group Container**

**Features**:
- Displays group type icon and name
- Collapse/expand functionality (IsCollapsed state)
- Groups consecutive tool calls of same type
- Shows count of invocations

**CSS**: Scoped styles with color-coded borders by tool type

#### ToolInvocationCard.razor
**Single Invocation-Result Pair**

**Structure**:
```
┌─────────────────────────────┐
│ ToolCallView (invocation)   │
├─────────────────────────────┤
│ ToolResultView (result)     │
└─────────────────────────────┘
```

**Features**:
- Pairs invocation with its result
- Handles null results gracefully
- Vertical layout with divider

#### ToolCallView.razor
**Tool Invocation Display**

**Shows**:
- Tool icon (from descriptor)
- Display name
- Parameters (first 3, with "+N more" if > 3)
- Re-run button (if CanRerun permission)

**Parameter Formatting**:
- Truncates values to 30 chars
- Handles JsonElement extraction
- Key-value display

**CSS**: Icon, content, actions layout with hover effects

#### ToolResultView.razor
**Tool Result Display**

**Header**:
- Status icon (success/error)
- Status text (e.g., "Found 3 result(s)" for search)
- Timestamp

**Content**:
- Routes to specialized renderer based on ResultContentType
- SearchCitations → SearchResultRenderer
- Image → ImageResultRenderer
- Json → JsonResultRenderer
- Error → ErrorResultRenderer
- Default → TextResultRenderer

**Actions**:
- Copy button (if CanCopy)
- Edit in editor button (if CanEditResult)

**Citation Counting**: Parses Data for `<citation>` or `<result>` tags

---

### 5. Result Renderers

#### SearchResultRenderer.razor
**Renders search results with citations**

**Regex Patterns**:
- `<result filename="..." page_number="...">quote</result>`
- `<citation filename='...' page_number='...'>quote</citation>`

**Display**:
- Document icon
- Filename with page number
- Quote text in styled quote box

**Data Handling**: Converts JsonElement to string if needed

#### TextResultRenderer.razor
**Plain text display**

**Renders**: `<pre><code>` block with raw text

**Data Handling**: Extracts string from various types (string, JsonElement, object)

#### JsonResultRenderer.razor
**Formatted JSON display**

**Features**:
- Syntax highlighted JSON
- Pretty-printed with indentation

#### ImageResultRenderer.razor
**Image display**

**Extracts**: "image" property from JSON (base64)

#### ErrorResultRenderer.razor
**Error message display**

**Shows**: ErrorMessage in red styling

---

## Data Flow

### 1. Streaming (Live Response)
```
ChatClient.GetStreamingResponseAsync()
  └─ Collects FunctionCallContent, FunctionResultContent, TextContent
     └─ Creates ChatMessage with all contents
        └─ Updates currentResponseMessage
           └─ ChatMessageItem receives message
              └─ ToolCallProcessor.ProcessMessage()
                 └─ Renders in AssistantMessageView
```

### 2. Loading from Storage
```
ChatHistoryStorage.GetAsync()
  └─ Deserializes JSON with ChatMessageConverter
     └─ Restores FunctionCallContent, FunctionResultContent
        └─ ChatMessage with full Contents
           └─ Same rendering flow as streaming
```

### 3. Processing Pipeline
```
ChatMessage.Contents[]
  ├─ TextContent → Consolidated into TextContent string
  ├─ FunctionCallContent
  │    └─ ToolRegistry.GetDescriptor(name)
  │       └─ Creates ToolInvocation with metadata
  └─ FunctionResultContent
       ├─ GetResultAsString() → extracts string
       ├─ DetectContentType() → determines type
       ├─ IsErrorResult() → checks for errors
       └─ Creates ToolResult with Data
```

---

## Serialization & Storage

### ChatMessageConverter
**Custom JSON converter for polymorphic AIContent**

**Write Strategy**:
- Adds `$type` discriminator for each content type
- ChatRole: Serializes as "User", "Assistant", "System", "Tool"
- TextContent: Serializes Text property
- FunctionCallContent: Serializes CallId, Name, Arguments dictionary
- FunctionResultContent: Serializes CallId, Result as string value (not nested JSON)
- UsageContent: Serializes token counts

**Read Strategy**:
- Reads `$type` to determine content type
- ChatRole: Maps string to ChatRole static instances
- FunctionCallContent: Deserializes Arguments dictionary with type detection
- FunctionResultContent: Uses `GetString()` to avoid escape sequences
- Creates appropriate AIContent instances

**Key Fix**: FunctionResultContent.Result stored as string value, not JSON, preventing double-escaping

---

## CSS Architecture

### Scoped Styles
Each component has `.razor.css` file with scoped styles:
- `ToolGroupCard.razor.css`: Card border colors by tool type
- `ToolCallView.razor.css`: Icon, content, actions layout
- `ToolResultView.razor.css`: Success/error states, header layout
- `SearchResultRenderer.razor.css`: Citation card styling
- (And more for each renderer)

### Color Scheme
- Search: Green border (`--color-search`)
- Terminal: Blue border (`--color-terminal`)
- Browser: Purple border (`--color-browser`)
- Error: Red styling
- Success: Green icon

---

## Extensibility Points

### Adding a New Tool Type

1. **Add ToolType enum value**:
```csharp
public enum ToolType {
    // ... existing
    MyNewTool
}
```

2. **Create descriptor**:
```csharp
public class MyToolDescriptor : IToolDescriptor {
    public ToolType Type => ToolType.MyNewTool;
    public string GetDisplayName(params) => "My Tool";
    public string GetIconSvg() => "<svg>...</svg>";
    public string GetColorClass() => "my-tool-color";
    // ... implement interface
}
```

3. **Register in ToolRegistry**:
```csharp
RegisterDescriptor(new MyToolDescriptor(), "MyTool", "myTool");
```

4. **Add CSS color** (optional):
```css
.tool-group-card[data-tool-type="MyNewTool"] {
    border-color: var(--color-my-tool);
}
```

### Adding a New Result Renderer

1. **Add ResultContentType enum value**:
```csharp
public enum ResultContentType {
    // ... existing
    MyNewType
}
```

2. **Create renderer component**:
```razor
@* MyResultRenderer.razor *@
<div class="my-result">
    @(Result.Data)
</div>
@code {
    [Parameter] public required ToolResult Result { get; set; }
}
```

3. **Update ToolResultView routing**:
```csharp
case ResultContentType.MyNewType:
    builder.OpenComponent<MyResultRenderer>(seq++);
    builder.AddAttribute(seq++, "Result", Result);
    builder.CloseComponent();
    break;
```

4. **Update content detection** in ToolCallProcessor:
```csharp
private ResultContentType DetectContentType(...) {
    // Add detection logic
    if (rawResult.Contains("my-marker")) {
        return ResultContentType.MyNewType;
    }
    // ...
}
```

---

## Class Diagram

```mermaid
classDiagram
    %% Data Models
    class ChatMessage {
        +ChatRole Role
        +List~AIContent~ Contents
    }
    
    class ProcessedMessage {
        +ChatMessage OriginalMessage
        +string TextContent
        +List~ToolGroup~ ToolGroups
    }
    
    class ToolGroup {
        +ToolType Type
        +List~ToolInvocation~ Invocations
        +List~ToolResult~ Results
        +bool IsCollapsed
    }
    
    class ToolInvocation {
        +string CallId
        +ToolType Type
        +string RawName
        +string DisplayName
        +Dictionary Parameters
        +ToolMetadata Metadata
        +ToolPermissions Permissions
    }
    
    class ToolResult {
        +string CallId
        +bool IsSuccess
        +ResultContentType ContentType
        +object Data
        +string ErrorMessage
        +DateTime CompletedAt
    }
    
    class ToolMetadata {
        +string SessionId
        +DateTime Timestamp
        +string TargetInfo
    }
    
    class ToolPermissions {
        +bool CanRerun
        +bool CanEditResult
        +bool CanCopy
        +bool CanExpand
    }
    
    %% Enums
    class ToolType {
        <<enumeration>>
        Unknown
        Search
        TerminalExecute
        BrowserExecute
        BrowserNavigate
        BrowserScreenshot
        FileRead
        FileWrite
        FileList
        ApiRequest
    }
    
    class ResultContentType {
        <<enumeration>>
        Text
        Json
        Image
        Html
        Error
        SearchCitations
    }
    
    %% Processing
    class ToolCallProcessor {
        -ToolRegistry _registry
        -ILogger _logger
        +ProcessMessage(ChatMessage) ProcessedMessage
        -CreateInvocation(FunctionCallContent) ToolInvocation
        -CreateResult(FunctionResultContent) ToolResult
        -DetectContentType(ToolInvocation, string) ResultContentType
        -IsErrorResult(string, ResultContentType) bool
        -GroupConsecutiveTools() List~ToolGroup~
    }
    
    %% Registry
    class ToolRegistry {
        -Dictionary~string,IToolDescriptor~ _descriptorsByName
        -Dictionary~ToolType,IToolDescriptor~ _descriptorsByType
        +RegisterDescriptor(IToolDescriptor, params string[])
        +GetDescriptor(string) IToolDescriptor
        +GetDescriptor(ToolType) IToolDescriptor
    }
    
    class IToolDescriptor {
        <<interface>>
        +ToolType Type
        +GetDisplayName(Dictionary) string
        +GetPermissions(Dictionary) ToolPermissions
        +ExtractMetadata(Dictionary) ToolMetadata
        +GetIconSvg() string
        +GetColorClass() string
    }
    
    class SearchToolDescriptor {
        +ToolType Type = Search
        +GetDisplayName() "Search: {phrase}"
        +GetIconSvg() "<svg>search icon</svg>"
    }
    
    class TerminalToolDescriptor {
        +ToolType Type = TerminalExecute
        +GetDisplayName() "Terminal: {command}"
        +GetIconSvg() "<svg>terminal icon</svg>"
    }
    
    %% Components
    class ChatMessageItem {
        +ChatMessage Message
        +bool InProgress
        +OnParametersSet()
    }
    
    class AssistantMessageView {
        +ProcessedMessage ProcessedMessage
        +RenderFragment for text
        +RenderFragment for ToolGroups
    }
    
    class ToolGroupCard {
        +ToolGroup Group
        +ToolInvocation[] Invocations
        +ToolResult[] Results
        +bool IsCollapsed
    }
    
    class ToolInvocationCard {
        +ToolInvocation Invocation
        +ToolResult Result
    }
    
    class ToolCallView {
        +ToolInvocation Invocation
        +bool ShowParameters
        +EventCallback OnRerun
    }
    
    class ToolResultView {
        +ToolResult Result
        +ToolInvocation Invocation
        +bool ShowActions
        +RenderContent() RenderFragment
    }
    
    class SearchResultRenderer {
        +object Data
        +Regex CitationRegex
        +Regex ResultRegex
        +List~CitationData~ ParsedCitations
    }
    
    class TextResultRenderer {
        +ToolResult Result
        +GetTextContent() string
    }
    
    %% Storage
    class ChatMessageConverter {
        +Read(Utf8JsonReader) ChatMessage
        +Write(Utf8JsonWriter, ChatMessage)
        -CreateFunctionCallContent(JsonElement) FunctionCallContent
        -CreateFunctionResultContent(JsonElement) FunctionResultContent
    }
    
    %% Relationships - Data Models
    ProcessedMessage --> ChatMessage : contains
    ProcessedMessage --> ToolGroup : contains list
    ToolGroup --> ToolInvocation : contains list
    ToolGroup --> ToolResult : contains list
    ToolInvocation --> ToolType : has
    ToolInvocation --> ToolMetadata : has
    ToolInvocation --> ToolPermissions : has
    ToolResult --> ResultContentType : has
    
    %% Relationships - Processing
    ToolCallProcessor --> ToolRegistry : uses
    ToolCallProcessor --> ChatMessage : processes
    ToolCallProcessor --> ProcessedMessage : creates
    ToolCallProcessor --> ToolInvocation : creates
    ToolCallProcessor --> ToolResult : creates
    ToolCallProcessor --> ToolGroup : creates
    
    %% Relationships - Registry
    ToolRegistry --> IToolDescriptor : manages
    SearchToolDescriptor ..|> IToolDescriptor : implements
    TerminalToolDescriptor ..|> IToolDescriptor : implements
    IToolDescriptor --> ToolType : returns
    IToolDescriptor --> ToolPermissions : returns
    IToolDescriptor --> ToolMetadata : returns
    
    %% Relationships - Components
    ChatMessageItem --> ToolCallProcessor : calls
    ChatMessageItem --> AssistantMessageView : renders
    AssistantMessageView --> ProcessedMessage : receives
    AssistantMessageView --> ToolGroupCard : renders
    ToolGroupCard --> ToolGroup : receives
    ToolGroupCard --> ToolInvocationCard : renders
    ToolInvocationCard --> ToolInvocation : receives
    ToolInvocationCard --> ToolResult : receives
    ToolInvocationCard --> ToolCallView : renders
    ToolInvocationCard --> ToolResultView : renders
    ToolCallView --> ToolInvocation : receives
    ToolCallView --> ToolRegistry : uses
    ToolResultView --> ToolResult : receives
    ToolResultView --> SearchResultRenderer : routes to
    ToolResultView --> TextResultRenderer : routes to
    
    %% Relationships - Storage
    ChatMessageConverter --> ChatMessage : serializes
```

---

## Key Design Decisions

1. **Grouping Strategy**: Consecutive tools of same type grouped together to reduce visual clutter

2. **Content Type Detection**: Automatic detection based on content patterns rather than explicit type hints from API

3. **Data as String**: ToolResult.Data stored as string after extraction to ensure consistent rendering regardless of source (streaming vs storage)

4. **Double-Escaping Prevention**: FunctionResultContent.Result serialized as string value, not nested JSON, to avoid escape sequences like `\u003C`

5. **Streaming Updates**: currentResponseMessage updated with ALL contents (not just text) during streaming so tool calls appear immediately

6. **Error Handling**: TaskCanceledException during storage load handled gracefully to support OnInitializedAsync timing

7. **Extensibility**: Descriptor pattern allows new tool types without modifying core renderer code

---

## Future Enhancements

1. **Tool Result Caching**: Cache processed results to avoid re-parsing on re-renders
2. **Lazy Loading**: Render results on-demand for large tool groups
3. **Result Diff View**: Compare results between re-runs
4. **Tool Execution Timeline**: Visual timeline of tool execution order and duration
5. **Export Results**: Export tool results to file formats
6. **Advanced Filtering**: Filter tool groups by type, status, timestamp
7. **Result Search**: Search within tool results across conversation history
