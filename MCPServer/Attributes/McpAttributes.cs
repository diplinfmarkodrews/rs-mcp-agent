using System;

namespace MCPServer.Attributes;

/// <summary>
/// Custom attribute to provide descriptions for MCP gRPC service methods
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class McpMethodAttribute : Attribute
{
    public string Description { get; }
    public string[] Parameters { get; set; } = Array.Empty<string>();
    public string ReturnDescription { get; set; } = string.Empty;
    public string[] Examples { get; set; } = Array.Empty<string>();

    public McpMethodAttribute(string description)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}

/// <summary>
/// Attribute to describe individual parameters of MCP methods
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class McpParameterAttribute : Attribute
{
    public string Description { get; }
    public bool Required { get; set; } = false;
    public string DefaultValue { get; set; } = string.Empty;
    public string[] AllowedValues { get; set; } = Array.Empty<string>();

    public McpParameterAttribute(string description)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}
