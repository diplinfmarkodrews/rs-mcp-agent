using System.ComponentModel;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Models;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Services;

namespace RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Mcp;

public class ScriptStoreTool
{
    private readonly IStaticContentIndexStore _fileIndexStore;
    private readonly IStaticContentFileStore _fileStore;
    private const string ScriptSourcePath = "rs-scripts";
    
    public ScriptStoreTool(IStaticContentIndexStore fileIndexStore,
        IStaticContentFileStore fileStore) 
    {
        _fileIndexStore = fileIndexStore;
        _fileStore = fileStore;
    }
    
    [KernelFunction, McpServerTool,  Description("List all script source pathes.")]
    public string GetAllScriptPaths()
    {
        var allResults = _fileIndexStore.GetAll(ScriptSourcePath)
            .Where(x => x.ContentType == ContentType.Script || x.ContentType == ContentType.Html || x.ContentType == ContentType.Text)
            .Select(x => x.RelativePath);
        
        return string.Join("\n", allResults);
    }
    
    [KernelFunction, McpServerTool,  Description("Use a scriptPath to retrieve the full script in textform.")]
    public async Task<string> GetText(string scriptPath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(scriptPath)) return "Error: Invalid script path.";
        
        var fileResult = await _fileStore.GetTextAsync(ScriptSourcePath, scriptPath, ct);
        
        if (string.IsNullOrWhiteSpace(fileResult)) return "Error: File not found.";
        return fileResult;
    } 
    
}