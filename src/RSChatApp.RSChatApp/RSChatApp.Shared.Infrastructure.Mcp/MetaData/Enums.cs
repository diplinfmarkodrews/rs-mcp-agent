namespace RSChatApp.Shared.Infrastructure.Mcp.MetaData;

public enum ResultContentType
{
    Text,
    Json,
    Image,
    Html,
    Error,
    SearchCitations,
    Terminal,
    DocumentPage
}
public enum ToolType
{
    Unknown,
    Search,
    TerminalExecute,
    BrowserExecute,
    BrowserNavigate,
    BrowserScreenshot,
    FileRead,
    FileWrite,
    FileList,
    DocumentLookup,
    ApiRequest
}
