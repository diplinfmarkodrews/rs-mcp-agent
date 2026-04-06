using System.Text.Json;
using Microsoft.SemanticKernel;
using RSChatApp.Shared.Infrastructure.Mcp.MetaData;
using RSChatApp.Web.Models.Chat.ToolCalls;
using FunctionResultContent = Microsoft.Extensions.AI.FunctionResultContent;

namespace RSChatApp.Web.Services.Chat.Tools;

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
        bool isError = invocation is null 
                       || IsErrorResult(rawResult, invocation.ResultContentType);
        
        return new ToolResult(
            CallId: frc.CallId,
            IsSuccess: !isError,
            ContentType: isError ? ResultContentType.Error : invocation!.ResultContentType,
            Data: rawResult,  // Use the string version, not the raw object
            ErrorMessage: isError ? rawResult : null,
            CompletedAt: DateTime.UtcNow
        );
    }
    public ToolResult Create(FunctionResult frc, ToolInvocation invocation)
    {
        var rawResult = GetResultAsString(frc);
        var isError = IsErrorResult(rawResult, invocation.ResultContentType);

        return new ToolResult(
            CallId: invocation.CallId,
            IsSuccess: !isError,
            ContentType: isError ? ResultContentType.Error : invocation.ResultContentType,
            Data: rawResult,  // Use the string version, not the raw object
            ErrorMessage: isError ? rawResult : null,
            CompletedAt: DateTime.UtcNow
        );
    }
    
    
    private static string? GetResultAsString(FunctionResultContent functionResultContent)
    {
        return functionResultContent.Result switch
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
            _ => functionResultContent.Result.ToString()
        };
    }
    
    /// <summary>
    /// semantic kernel's FunctionResult, need to access contents array[0].text if it's a json element
    /// </summary>
    /// <param name="functionResult"></param>
    /// <returns></returns>
    private static string? GetResultAsString(FunctionResult functionResult)
    { 
        string? result;
        switch (functionResult.GetValue<object>())
        {
            case null: result = null; break;
            case string s: result = s; break;
            case JsonElement e:
                if (e.GetProperty("content")[0].TryGetProperty("text", out var value))
                {
                    result = value.GetString();
                    break;
                }

                result = e.GetString();
                break;
            default:
                result = functionResult.ToString() ?? string.Empty;
                break;
        }
        return result;

    }
    private static bool IsErrorResult(string? result, ResultContentType contentType)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            return false;
        }

        // Don't apply error keyword detection to search results or other structured content
        // as they may contain these words in their actual content
        // Todo: refine error detection and resolve these dependencies on content type (openclosed p.)
        if (contentType == ResultContentType.SearchCitations || 
            contentType == ResultContentType.Json ||
            contentType == ResultContentType.Image ||
            contentType == ResultContentType.Terminal)
        {
            return false;
        }

        return result.Contains("error", StringComparison.OrdinalIgnoreCase) ||
               result.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
               result.Contains("failed", StringComparison.OrdinalIgnoreCase);
    }

}