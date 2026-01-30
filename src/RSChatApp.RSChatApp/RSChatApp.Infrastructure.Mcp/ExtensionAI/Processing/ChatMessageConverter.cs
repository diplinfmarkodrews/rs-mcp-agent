using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace RSChatApp.Mcp.ExtensionAI.Processing;

/// <summary>
/// Custom JSON converter for ChatMessage that preserves polymorphic AIContent types
/// </summary>
public class ChatMessageConverter : JsonConverter<ChatMessage>
{
    public override ChatMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        
        // Read basic properties
        var roleString = root.GetProperty("Role").GetString() ?? "Assistant";
        var role = roleString switch
        {
            "User" => ChatRole.User,
            "Assistant" => ChatRole.Assistant,
            "System" => ChatRole.System,
            "Tool" => ChatRole.Tool,
            _ => ChatRole.Assistant
        };
        
        // Read Contents array
        var contents = new List<AIContent>();
        if (root.TryGetProperty("Contents", out var contentsArray) && contentsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var contentElement in contentsArray.EnumerateArray())
            {
                if (!contentElement.TryGetProperty("$type", out var typeProperty))
                    continue;
                
                var typeName = typeProperty.GetString();
                
                AIContent? content = typeName switch
                {
                    string t when t.Contains("TextContent") => 
                        new TextContent(contentElement.GetProperty("Text").GetString() ?? ""),
                    
                    string t when t.Contains("FunctionCallContent") => 
                        CreateFunctionCallContent(contentElement),
                    
                    string t when t.Contains("FunctionResultContent") => 
                        CreateFunctionResultContent(contentElement),
                    
                    string t when t.Contains("UsageContent") => 
                        CreateUsageContent(contentElement),
                    
                    // Skip other unknown types
                    _ => null
                };
                
                if (content != null)
                {
                    contents.Add(content);
                }
            }
        }
        
        return new ChatMessage(role, contents);
    }
    
    private static FunctionCallContent CreateFunctionCallContent(JsonElement contentElement)
    {
        var callId = contentElement.GetProperty("CallId").GetString() ?? "";
        var name = contentElement.GetProperty("Name").GetString() ?? "";
        var arguments = new Dictionary<string, object?>();
        
        if (contentElement.TryGetProperty("Arguments", out var argsElement))
        {
            foreach (var arg in argsElement.EnumerateObject())
            {
                arguments[arg.Name] = arg.Value.ValueKind switch
                {
                    JsonValueKind.String => arg.Value.GetString(),
                    JsonValueKind.Number => arg.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => arg.Value.GetRawText()
                };
            }
        }
        
        return new FunctionCallContent(callId, name, arguments);
    }
    
    private static FunctionResultContent CreateFunctionResultContent(JsonElement contentElement)
    {
        var callId = contentElement.GetProperty("CallId").GetString() ?? "";
        object? result = null;

        if (contentElement.TryGetProperty("Result", out var resultElement))
        {
            result = resultElement.ValueKind == JsonValueKind.String
                ? resultElement.GetString()
                : resultElement.Clone();
        }

        return new FunctionResultContent(callId, result);
    }
    
    private static UsageContent CreateUsageContent(JsonElement contentElement)
    {
        var inputTokenCount = contentElement.TryGetProperty("InputTokenCount", out var inputTokens)
            ? inputTokens.GetInt32()
            : 0;
        var outputTokenCount = contentElement.TryGetProperty("OutputTokenCount", out var outputTokens)
            ? outputTokens.GetInt32()
            : 0;
        var totalTokenCount = contentElement.TryGetProperty("TotalTokenCount", out var totalTokens)
            ? totalTokens.GetInt32()
            : 0;
        
        return new UsageContent(new UsageDetails
        {
            InputTokenCount = inputTokenCount,
            OutputTokenCount = outputTokenCount,
            TotalTokenCount = totalTokenCount
        });
    }
    
    public override void Write(Utf8JsonWriter writer, ChatMessage value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        // Write role name correctly
        var roleName = value.Role == ChatRole.User ? "User" :
                       value.Role == ChatRole.Assistant ? "Assistant" :
                       value.Role == ChatRole.System ? "System" :
                       value.Role == ChatRole.Tool ? "Tool" : "Assistant";
        writer.WriteString("Role", roleName);
        
        writer.WritePropertyName("Contents");
        writer.WriteStartArray();
        
        if (value.Contents != null)
        {
            foreach (var content in value.Contents)
            {
                writer.WriteStartObject();
                
                switch (content)
                {
                    case TextContent textContent:
                        writer.WriteString("$type", "TextContent");
                        writer.WriteString("Text", textContent.Text);
                        break;
                    
                    case FunctionCallContent fcc:
                        writer.WriteString("$type", "FunctionCallContent");
                        writer.WriteString("CallId", fcc.CallId);
                        writer.WriteString("Name", fcc.Name);
                        
                        if (fcc.Arguments != null)
                        {
                            writer.WritePropertyName("Arguments");
                            writer.WriteStartObject();
                            foreach (var arg in fcc.Arguments)
                            {
                                writer.WritePropertyName(arg.Key);
                                JsonSerializer.Serialize(writer, arg.Value, options);
                            }
                            writer.WriteEndObject();
                        }
                        break;
                    
                    case FunctionResultContent frc:
                        writer.WriteString("$type", "FunctionResultContent");
                        writer.WriteString("CallId", frc.CallId);
                        if (frc.Result != null)
                        {
                            writer.WritePropertyName("Result");
                            switch (frc.Result)
                            {
                                case JsonElement json:
                                    json.WriteTo(writer);
                                    break;
                                default:
                                    // Backward compatible: store as string
                                    writer.WriteStringValue(frc.Result.ToString() ?? "");
                                    break;
                            }
                        }
                        break;
                    
                    case UsageContent usageContent:
                        writer.WriteString("$type", "UsageContent");
                        if (usageContent.Details != null)
                        {
                            writer.WriteNumber("InputTokenCount", usageContent.Details.InputTokenCount ?? 0);
                            writer.WriteNumber("OutputTokenCount", usageContent.Details.OutputTokenCount ?? 0);
                            writer.WriteNumber("TotalTokenCount", usageContent.Details.TotalTokenCount ?? 0);
                        }
                        break;
                    
                    // Skip other unknown types
                    default:
                        break;
                }
                
                writer.WriteEndObject();
            }
        }
        
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
