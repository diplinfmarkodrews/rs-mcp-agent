using RSChatApp.Shared.Infrastructure.Mcp.MetaData;
using RSChatApp.Web.Models.Chat.ToolCalls;

namespace RSChatApp.Web.Services.Chat.Tools.Descriptors;

public class DocumentLookupToolDescriptor : IToolDescriptor
{
    public ToolType Type => ToolType.DocumentLookup;

    public ResultContentType ResultContentType => ResultContentType.DocumentPage;
    public string GetDisplayName(IReadOnlyDictionary<string, object?> parameters)
    {
        var documentId = FormatValue(parameters.GetValueOrDefault("documentId"));
        var page = FormatValue(parameters.GetValueOrDefault("page"));

        if (!string.IsNullOrWhiteSpace(page))
        {
            return $"Document: {Truncate(documentId, 40)} (page {page})";
        }

        return $"Document: {Truncate(documentId, 50)}";
    }

    public ToolPermissions GetPermissions(IReadOnlyDictionary<string, object?> parameters)
    {
        return new ToolPermissions(
            CanRerun: true,
            CanEditResult: false,
            CanCopy: true,
            CanExpand: true
        );
    }

    public ToolMetadata ExtractMetadata(IReadOnlyDictionary<string, object?> parameters)
    {
        var documentId = FormatValue(parameters.GetValueOrDefault("documentId"));

        return new ToolMetadata(
            SessionId: null,
            Timestamp: DateTime.UtcNow,
            TargetInfo: documentId
        );
    }

    public ToolUiHints GetUiHints(IReadOnlyDictionary<string, object?> parameters)
    {
        return new ToolUiHints(DefaultExpanded: true);
    }

    public ToolUserConfirmation GetUserConfirmation(string? functionName = null)
    {
        return ToolUserConfirmation.None;
    }

    public string GetIconSvg()
    {
        return """
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
            </svg>
        """;
    }

    public IEnumerable<string> ToolNames
    {
        get => ["DocumentLookup"];
    }

    public string GetColorClass() => "tool-document";

    private static string? FormatValue(object? value)
    {
        return value switch
        {
            null => null,
            string s => s,
            System.Text.Json.JsonElement e when e.ValueKind == System.Text.Json.JsonValueKind.String => e.GetString(),
            _ => value.ToString()
        };
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= max ? value : value[..max] + "…";
    }
}
