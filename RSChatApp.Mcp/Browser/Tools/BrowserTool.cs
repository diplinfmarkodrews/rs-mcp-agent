using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using RSChatApp.Mcp.Browser.Core;

namespace RSChatApp.Mcp.Browser.Tools;

public class BrowserTool 
{
    private readonly IBrowserInstance _browserInstance;

    public BrowserTool(IBrowserInstanceProvider browserProvider)
    {
        _browserInstance = browserProvider.GetBrowserInstance();

    }
    [KernelFunction, McpServerTool, Description("start a new browser context")]
    public async Task StartAsync()
    {
        await _browserInstance.NewContextAsync();
    }
    
}