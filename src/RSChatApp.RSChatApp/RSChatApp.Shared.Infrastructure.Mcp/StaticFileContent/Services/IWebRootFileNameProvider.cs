using Microsoft.AspNetCore.Hosting;

namespace RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Services;

public interface IWebRoolFileNameProvider
{
    IEnumerable<string> GetFileNames(string path);
}

public class WebRoolFileNameProvider : IWebRoolFileNameProvider
{
    private readonly IWebHostEnvironment _environment;

    public WebRoolFileNameProvider(IWebHostEnvironment environment)
    {
        _environment = environment;
    }
    
    public IEnumerable<string> GetFileNames(string path)
    {
        var fullPath = Path.Combine(_environment.WebRootPath, path);
        if (!Directory.Exists(fullPath))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.EnumerateFiles(fullPath)
            .Select(Path.GetFileName);
    }
}