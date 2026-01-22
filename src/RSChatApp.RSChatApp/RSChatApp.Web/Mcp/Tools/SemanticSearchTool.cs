using System.ComponentModel;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using RSChatApp.Web.Services.SemanticSearch;

namespace RSChatApp.Web.Mcp.Tools;

public class SemanticSearchTool
{
    private readonly SemanticSearch _semanticSearch;

    public SemanticSearchTool(SemanticSearch semanticSearch)
    {
        _semanticSearch = semanticSearch;
    }
    
    [KernelFunction, McpServerTool,  Description("Searches for information concerning the reportserver, using a phrase or keyword.")]
    public async Task<IEnumerable<string>> SearchAsync(
        [Description("The phrase to search for.")] string searchPhrase,
        [Description("If possible, specify the filename to search that file only. If not provided or empty, the search includes all files.")] string? filenameFilter = null,
        [Description("The maximum number of results to return. Default is 17.")] int maxResults = 20)
    {
        
        var results = await _semanticSearch.SearchAsync(searchPhrase, filenameFilter, maxResults);
        return results.Select(result =>
            $"<result filename=\"{result.DocumentId}\" page_number=\"{result.PageNumber}\">{result.Text}</result>");
    }
}