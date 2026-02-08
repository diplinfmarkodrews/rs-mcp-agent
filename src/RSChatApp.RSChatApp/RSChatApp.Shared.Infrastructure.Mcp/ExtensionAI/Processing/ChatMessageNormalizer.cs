using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.Processing;

public static class ChatMessageNormalizer
{
    /// <summary>
    /// Normalizes messages for the API by splitting assistant messages that contain both FunctionCallContent and FunctionResultContent.
    /// Claude API requires: Assistant (with tool calls) -> Tool (with results) -> Assistant (with response)
    /// But we store them combined for display purposes.
    /// </summary>
    public static List<ChatMessage> NormalizeMessagesForApi(this List<ChatMessage> messages)
    {
        var normalized = new List<ChatMessage>();
        
        foreach (var message in messages)
        {
            // Only process assistant messages that might have tool calls
            if (message.Role != ChatRole.Assistant || message.Contents is null || message.Contents.Count <= 1)
            {
                normalized.Add(message);
                continue;
            }
            
            // Check if this message has both FunctionCallContent and FunctionResultContent
            var functionCalls = message.Contents.OfType<FunctionCallContent>().ToList();
            var functionResults = message.Contents.OfType<FunctionResultContent>().ToList();
            
            if (functionCalls.Count == 0 || functionResults.Count == 0)
            {
                // No splitting needed - either no tool calls or they're already separate
                normalized.Add(message);
                continue;
            }
            
            // Split into multiple messages:
            // 1. Assistant message with text (if any) + FunctionCallContent
            var assistantContents = new List<AIContent>();
            var textContent = message.Contents.OfType<TextContent>().FirstOrDefault();
            if (textContent != null && !string.IsNullOrWhiteSpace(textContent.Text))
            {
                assistantContents.Add(textContent);
            }
            assistantContents.AddRange(functionCalls.Cast<AIContent>());
            
            if (assistantContents.Count > 0)
            {
                normalized.Add(new ChatMessage(ChatRole.Assistant, assistantContents));
            }
            
            // 2. Tool message(s) with FunctionResultContent
            foreach (var result in functionResults)
            {
                normalized.Add(new ChatMessage(ChatRole.Tool, new List<AIContent> { result }));
            }
        }
        
        return normalized;
    }
    public static ChatMessage NormalizeChatMessageContents(this ChatMessage message)
    {
        if (message.Contents is null || message.Contents.Count == 0)
        {
            return message;
        }

        var normalizedContents = NormalizeAssistantContents(message.Contents);
        return new ChatMessage(message.Role, normalizedContents);
    }

    public static List<AIContent> NormalizeAssistantContents(this IEnumerable<AIContent> contents)
    {
        var normalized = contents is ICollection<AIContent> collection
            ? new List<AIContent>(collection.Count)
            : new List<AIContent>();

        foreach (var content in contents)
        {
            if (content is FunctionResultContent frc && ShouldSkipNormalization(frc) == false)
            {
                var normalizedResult = NormalizeToolResultObject(frc.Result);
                normalized.Add(new FunctionResultContent(frc.CallId, normalizedResult));
                continue;
            }

            normalized.Add(content);
        }

        return normalized;
    }

    private static bool ShouldSkipNormalization(FunctionResultContent frc)
    {
        // Or check if result contains <citation> tags
        if (frc.Result is JsonElement s 
            && s.ValueKind == JsonValueKind.String 
            && s.GetString().Contains("<citation", StringComparison.Ordinal))
            return true;
        return false;
    }
    private static object? NormalizeToolResultObject(object? result)
    {
        return result switch
        {
            null => null,
            JsonDocument doc => NormalizeJsonElement(doc.RootElement),
            JsonElement element => NormalizeJsonElement(element),
            string s => NormalizeFromString(s),
            _ => result
        };
    }

    private static object NormalizeJsonElement(JsonElement element)
    {
        // If this is an MCP envelope, unwrap the inner text and try parse it.
        if (TryExtractMcpText(element, out var mcpText))
        {
            return NormalizeFromString(mcpText);
        }

        // If it's a JSON string value, treat it like a string payload.
        if (element.ValueKind == JsonValueKind.String)
        {
            return NormalizeFromString(element.GetString() ?? string.Empty);
        }

        // Already a structured JSON value.
        return element.Clone();
    }

    private static object NormalizeFromString(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return value;
        }

        // 1) Try parse directly.
        if (TryParseJsonValue(trimmed, out var parsed))
        {
            return parsed;
        }

        // 2) If parsing fails, attempt a single decode pass (handles literal \u0022 etc.).
        var decoded = TryDecodeJsonEscapesOnce(trimmed);
        if (!string.IsNullOrEmpty(decoded) && decoded != trimmed && TryParseJsonValue(decoded, out parsed))
        {
            return parsed;
        }

        return value;
    }

    private static bool TryParseJsonValue(string text, out JsonElement parsed)
    {
        parsed = default;

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            // If the root is a JSON string that itself contains JSON, parse that too.
            if (root.ValueKind == JsonValueKind.String)
            {
                var inner = root.GetString();
                if (!string.IsNullOrWhiteSpace(inner))
                {
                    try
                    {
                        using var innerDoc = JsonDocument.Parse(inner);
                        parsed = innerDoc.RootElement.Clone();
                        return true;
                    }
                    catch
                    {
                        // fall through and return root string as non-JSON by failing
                    }
                }

                return false;
            }

            parsed = root.Clone();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? TryDecodeJsonEscapesOnce(string text)
    {
        // Interpret escape sequences like \u0022 by parsing the text as a JSON string literal.
        // We must escape quotes and control characters, but keep backslashes intact.
        try
        {
            var escaped = EscapeForJsonStringLiteral(text);
            using var doc = JsonDocument.Parse("\"" + escaped + "\"");
            return doc.RootElement.GetString();
        }
        catch
        {
            return null;
        }
    }

    private static string EscapeForJsonStringLiteral(string text)
    {
        var sb = new StringBuilder(text.Length + 16);

        foreach (var c in text)
        {
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (c < 0x20)
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }

        return sb.ToString();
    }

    private static bool TryExtractMcpText(JsonElement element, out string text)
    {
        text = string.Empty;

        // Common MCP shape: { "content": [ { "type": "text", "text": "..." } ] }
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.Array)
        {
            var parts = new List<string>();
            foreach (var item in content.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (item.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String)
                {
                    var type = typeEl.GetString();
                    if (!string.Equals(type, "text", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                if (item.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                {
                    var part = textEl.GetString();
                    if (!string.IsNullOrEmpty(part))
                    {
                        parts.Add(part);
                    }
                }
            }

            if (parts.Count > 0)
            {
                text = string.Join("\n", parts);
                return true;
            }
        }

        return false;
    }
}