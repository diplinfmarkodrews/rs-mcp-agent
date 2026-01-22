using Microsoft.Extensions.AI;
using RSChatApp.Web.Models.Chat.ToolCalls;
using RSChatApp.Web.Services.Chat.Tools;
using System.Text.Json;

namespace RSChatApp.Web.Services.Chat;

public class ToolCallProcessor
{
    private readonly ToolRegistry _registry;
    private readonly ILogger<ToolCallProcessor> _logger;

    public ToolCallProcessor(ToolRegistry registry, ILogger<ToolCallProcessor> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public ProcessedMessage ProcessMessage(ChatMessage message)
    {
        var textContents = new List<string>();
        var invocations = new List<ToolInvocation>();
        var results = new Dictionary<string, ToolResult>();

        _logger.LogDebug("[ToolCallProcessor] Processing message with {contentCount} content items", message.Contents.Count);
        
        foreach (var content in message.Contents)
        {
            _logger.LogDebug("[ToolCallProcessor] Content type: {contentType}", content.GetType().Name);
            
            if (content is TextContent tc && !string.IsNullOrWhiteSpace(tc.Text))
            {
                textContents.Add(tc.Text);
                _logger.LogDebug("[ToolCallProcessor] Added text content: {textPreview}...", tc.Text.Substring(0, Math.Min(50, tc.Text.Length)));
            }
            else if (content is FunctionCallContent fcc)
            {
                invocations.Add(CreateInvocation(fcc));
                _logger.LogDebug("[ToolCallProcessor] Added function call: {functionName}", fcc.Name);
            }
            else if (content is FunctionResultContent frc)
            {
                results[frc.CallId] = CreateResult(frc, invocations);
                _logger.LogDebug("[ToolCallProcessor] Added function result for: {callId}", frc.CallId);
            }
        }
        
        _logger.LogDebug("[ToolCallProcessor] Found {invocationCount} invocations and {resultCount} results", invocations.Count, results.Count);

        var groups = GroupConsecutiveTools(invocations, results);
        
        _logger.LogDebug("[ToolCallProcessor] Created {groupCount} tool groups", groups.Count);
        foreach (var group in groups)
        {
            _logger.LogDebug("[ToolCallProcessor]   Group: {groupType}, Invocations: {invocationCount}, Results: {resultCount}", group.Type, group.Invocations.Count, group.Results.Count);
        }

        return new ProcessedMessage(
            OriginalMessage: message,
            TextContent: string.Join("\n", textContents),
            ToolGroups: groups
        );
    }

    private ToolInvocation CreateInvocation(FunctionCallContent fcc)
    {
        var descriptor = _registry.GetDescriptor(fcc.Name);
        var parameters = fcc.Arguments?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                      ?? new Dictionary<string, object?>();

        return new ToolInvocation(
            CallId: fcc.CallId,
            Type: descriptor.Type,
            RawName: fcc.Name,
            DisplayName: descriptor.GetDisplayName(parameters),
            Parameters: parameters,
            Metadata: descriptor.ExtractMetadata(parameters),
            Permissions: descriptor.GetPermissions(parameters)
        );
    }

    private ToolResult CreateResult(FunctionResultContent frc, List<ToolInvocation> invocations)
    {
        var invocation = invocations.FirstOrDefault(i => i.CallId == frc.CallId);
        var rawResult = GetResultAsString(frc);
        var contentType = DetectContentType(invocation, rawResult);
        var isError = IsErrorResult(rawResult, contentType);

        return new ToolResult(
            CallId: frc.CallId,
            IsSuccess: !isError,
            ContentType: isError ? ResultContentType.Error : contentType,
            Data: rawResult,  // Use the string version, not the raw object
            ErrorMessage: isError ? rawResult : null,
            CompletedAt: DateTime.UtcNow
        );
    }

    private ResultContentType DetectContentType(ToolInvocation? invocation, string? rawResult)
    {
        if (string.IsNullOrWhiteSpace(rawResult))
        {
            return ResultContentType.Text;
        }

        // Search results contain citations
        if (invocation?.Type == ToolType.Search)
        {
            if (rawResult.Contains("<citation", StringComparison.OrdinalIgnoreCase) || 
                rawResult.Contains("<result", StringComparison.OrdinalIgnoreCase))
            {
                return ResultContentType.SearchCitations;
            }
        }

        // Browser screenshot results
        if (invocation?.Type == ToolType.BrowserExecute || invocation?.Type == ToolType.BrowserScreenshot)
        {
            if (TryParseJson(rawResult, out var json) && 
                json.RootElement.TryGetProperty("image", out _))
            {
                return ResultContentType.Image;
            }
        }

        // JSON detection
        if (TryParseJson(rawResult, out _))
        {
            return ResultContentType.Json;
        }

        return ResultContentType.Text;
    }

    private List<ToolGroup> GroupConsecutiveTools(
        List<ToolInvocation> invocations,
        Dictionary<string, ToolResult> results)
    {
        var groups = new List<ToolGroup>();
        ToolGroup? currentGroup = null;

        foreach (var invocation in invocations)
        {
            if (currentGroup is null || currentGroup.Type != invocation.Type)
            {
                currentGroup = new ToolGroup(invocation.Type);
                groups.Add(currentGroup);
            }

            currentGroup.Invocations.Add(invocation);
            currentGroup.Results.Add(results.GetValueOrDefault(invocation.CallId));
        }

        return groups;
    }

    private static string? GetResultAsString(FunctionResultContent frc)
    {
        return frc.Result switch
        {
            null => null,
            string s => s,
            JsonElement e when e.ValueKind == JsonValueKind.String => e.GetString(),
            JsonElement e => e.GetRawText(),
            _ => frc.Result.ToString()
        };
    }

    private static bool IsErrorResult(string? result, ResultContentType contentType)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            return false;
        }

        // Don't apply error keyword detection to search results or other structured content
        // as they may contain these words in their actual content
        if (contentType == ResultContentType.SearchCitations || 
            contentType == ResultContentType.Json ||
            contentType == ResultContentType.Image)
        {
            return false;
        }

        return result.Contains("error", StringComparison.OrdinalIgnoreCase) ||
               result.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
               result.Contains("failed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseJson(string? text, out JsonDocument? document)
    {
        document = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            document = JsonDocument.Parse(text);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
