namespace RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Models;

public sealed class StaticContentItem
{
    public required string SourceName { get; set; }
    public required string RelativePath { get; set; }
    public required long Length  { get; set; } 
    public required DateTime LastModified { get; set; }
    public string? Extension { get; set; }
    public ContentType? ContentType { get => ToContentType(Extension); }
    
    public static ContentType ToContentType(string? extension)
    {
        return extension?.Trim('.') switch
        {
            "groovy" => Models.ContentType.Script,
            "grvy" => Models.ContentType.Script,
            "js"=> Models.ContentType.Script,
            "md" => Models.ContentType.Text,
            "txt" => Models.ContentType.Text,
            "html" => Models.ContentType.Html,
            "cf" => Models.ContentType.Config,
            "jpg" => Models.ContentType.Image,
            "jpeg" => Models.ContentType.Image,
            "properties" => Models.ContentType.Config,
            _ => Models.ContentType.Undefined
        };

    }
};


public enum ContentType
{
    Undefined = -1,
    Text,
    Script,
    Config,
    Image,
    Html
    
}
public sealed record StaticContentQuery(
    string? Prefix = null,
    string? Contains = null,
    string? Extension = null,
    ContentType? ContentType = null,
    int? Limit = 200
);
