using System.Text.Json;
using Microsoft.SemanticKernel;
using RSChatApp.Web.Models.Chat.ToolCalls;
using FunctionResultContent = Microsoft.Extensions.AI.FunctionResultContent;

namespace RSChatApp.Web.Services.Chat;

public class ToolResultFactory
{
    private readonly ILogger<ToolResultFactory> _logger;
    
    public ToolResultFactory(ILogger<ToolResultFactory> logger)
    {
        _logger = logger;
    }
    
    public ToolResult Create(FunctionResultContent frc, List<ToolInvocation> invocations)
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
    public ToolResult Create(FunctionResult frc, ToolInvocation invocation)
    {
        var rawResult = frc.GetValue<string>();
        var contentType = DetectContentType(invocation, rawResult);
        var isError = IsErrorResult(rawResult, contentType);

        return new ToolResult(
            CallId: invocation.CallId,
            IsSuccess: !isError,
            ContentType: isError ? ResultContentType.Error : contentType,
            Data: rawResult,  // Use the string version, not the raw object
            ErrorMessage: isError ? rawResult : null,
            CompletedAt: DateTime.UtcNow
        );
    }
    private ResultContentType DetectContentType(ToolInvocation? invocation, string? rawResult)
    {

        // Search results are always SearchCitations type (even if empty)
        if (invocation?.Type == ToolType.Search)
        {
            return ResultContentType.SearchCitations;
        }

        // Document lookup results
        if (invocation?.Type == ToolType.DocumentLookup)
        {
            return ResultContentType.DocumentPage;
        }

        if (string.IsNullOrWhiteSpace(rawResult))
        {
            return ResultContentType.Text;
        }

        // Terminal command results
        if (invocation?.Type == ToolType.TerminalExecute 
            || invocation?.Type == ToolType.BrowserExecute)
        {
            if (TryParseJson(rawResult, out var json))
            {
                var root = json.RootElement;
                var hasSessionId = TryGetPropertyIgnoreCase(root, "SessionId", out _);
                var hasCmdResult = TryGetPropertyIgnoreCase(root, "CmdResult", out _);
                
                if (hasSessionId && hasCmdResult)
                {
                    return ResultContentType.Terminal;
                }
            }
            else
            {
                _logger.LogDebug("Failed to parse JSON for terminal detection. First 100 chars: {Preview}", 
                    rawResult?.Length > 100 ? rawResult[..100] : rawResult);
            }
        }

        // Browser screenshot results
        if (invocation?.Type == ToolType.BrowserScreenshot)
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

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        return false;
    }
    private static string? GetResultAsString(FunctionResultContent frc)
    {
        return frc.Result switch
        {
            null => null,
            string s => s,
            JsonElement e when e.ValueKind == JsonValueKind.String => e.GetString(),
            // For compatibility reason support array types. usually ToolCallResult are string
            JsonElement e when e.ValueKind == JsonValueKind.Array =>
                string.Join(",", e.EnumerateArray().Select(item =>
                    item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText())),
            IEnumerable<string> enumerable => string.Join(",", enumerable),
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
            contentType == ResultContentType.Image ||
            contentType == ResultContentType.Terminal ||
            contentType == ResultContentType.DocumentPage)
        {
            return false;
        }

        return result.Contains("error", StringComparison.OrdinalIgnoreCase) ||
               result.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
               result.Contains("failed", StringComparison.OrdinalIgnoreCase);
    }

}