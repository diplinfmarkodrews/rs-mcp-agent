using System.ComponentModel;

namespace RSChatApp.Web.Services.SemanticSearch;

public class SemanticSearchMcp
{
    private readonly SemanticSearch _semanticSearch;

    public SemanticSearchMcp(SemanticSearch semanticSearch)
    {
        _semanticSearch = semanticSearch;
    }
    
    [Description("Searches for information using a phrase or keyword. Always provide sources with documentId and pages.")]
    private async Task<IEnumerable<string>> SearchAsync(
        [Description("The phrase to search for.")] string searchPhrase,
        [Description("If possible, specify the filename to search that file only. If not provided or empty, the search includes all files.")] string? filenameFilter = null,
        [Description("The maximum number of results to return. Default is 17.")] int maxResults = 17)
    {
        
        var results = await _semanticSearch.SearchAsync(searchPhrase, filenameFilter, maxResults);
        return results.Select(result =>
            $"<result filename=\"{result.DocumentId}\" page_number=\"{result.PageNumber}\">{result.Text}</result>");
    }
}