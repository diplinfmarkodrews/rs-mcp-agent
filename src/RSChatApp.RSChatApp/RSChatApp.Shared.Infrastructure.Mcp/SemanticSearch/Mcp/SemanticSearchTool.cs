using System.ComponentModel;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;

namespace RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Mcp;

public class SemanticSearchTool
{
    private readonly SemanticSearch _semanticSearch;

    public SemanticSearchTool(SemanticSearch semanticSearch)
    {
        _semanticSearch = semanticSearch;
    }
    
    [KernelFunction, McpServerTool,  Description("Searches for information concerning the reportserver, using a phrase or keyword.")]
    public async Task<string> SearchAsync(
        [Description("The phrase to search for.")] string searchPhrase,
        [Description("If possible, specify the filename to search that file only. If not provided or empty, the search includes all files.")] string? filenameFilter = null,
        [Description("The maximum number of results to return. Default is 25.")] int maxResults = 25)
    {
        
        var results = await _semanticSearch.SearchAsync(searchPhrase, filenameFilter, maxResults);
        return string.Join("", results.Select(result =>
            $"<citation filename=\"{result.DocumentId}\" page_number=\"{result.PageNumber}\">{result.Text}</citation>"));
    }
}