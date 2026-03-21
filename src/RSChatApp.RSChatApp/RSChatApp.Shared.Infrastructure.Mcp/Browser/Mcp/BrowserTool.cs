using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using RSChatApp.Shared.Infrastructure.Mcp.Browser.Interfaces;
using RSChatApp.Shared.Infrastructure.Mcp.MetaData.Attributes;

namespace RSChatApp.Shared.Infrastructure.Mcp.Browser.Mcp;

public class BrowserTool 
{
    private readonly ILogger<BrowserTool> _logger;
    private readonly IBrowserInstance _browserInstance;

    public BrowserTool(ILogger<BrowserTool> logger, IBrowserInstanceProvider browserProvider)
    {
        _logger = logger;
        _browserInstance = browserProvider?.GetBrowserInstance()!;
       
    }

    /// <summary>
    /// Navigate to a specific URL
    /// </summary>
    [KernelFunction, Description("Navigate to a specific URL.")]
    public async Task<string> NavigateToUrlAsync(
        [Description("The URL to navigate to")] string url,
        [Description("Optional timeout in milliseconds (default: 30000)")] int timeoutMs = 30000)
    {
        _logger.LogDebug("NavigateToUrlAsync called with URL: {Url}, Timeout: {TimeoutMs}ms", url, timeoutMs);
        
        try
        {
            await _browserInstance.NavigateAsync(url);
            
            var currentUrl = _browserInstance.CurrentUrl;
            var title = await _browserInstance.GetTitleAsync();
            
            _logger.LogInformation("Successfully navigated to {Url}. Current URL: {CurrentUrl}, Title: {Title}", url, currentUrl, title);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                url = currentUrl,
                title = title
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to URL: {Url}", url);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Click on an element by selector
    /// </summary>
    [KernelFunction, Description("Click on an element by CSS selector")]
    public async Task<string> ClickElementAsync(
        [Description("CSS selector for the element to click")] string selector,
        [Description("Optional timeout in milliseconds (default: 30000)")] int timeoutMs = 30000)
    {
        _logger.LogInformation("ClickElementAsync called with selector: {Selector}, Timeout: {TimeoutMs}ms", selector, timeoutMs);
        
        try
        {
            await _browserInstance.ClickElementAsync(selector, timeoutMs);

            var currentUrl = _browserInstance.CurrentUrl;
            _logger.LogInformation("Successfully clicked element: {Selector}. Current URL: {CurrentUrl}", selector, currentUrl);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Successfully clicked element: {selector}",
                url = currentUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to click element: {Selector}", selector);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Type text into an input field
    /// </summary>
    [KernelFunction, Description("Type text into an input field by CSS selector")]
    public async Task<string> TypeTextAsync(
        [Description("CSS selector for the input field")] string selector,
        [Description("Text to type")] string text,
        [Description("Clear field before typing (default: true)")] bool clearFirst = true,
        [Description("Optional timeout in milliseconds (default: 30000)")] int timeoutMs = 30000)
    {
        _logger.LogInformation("TypeTextAsync called with selector: {Selector}, Text length: {TextLength}, ClearFirst: {ClearFirst}, Timeout: {TimeoutMs}ms", 
            selector, text?.Length ?? 0, clearFirst, timeoutMs);
        
        try
        {
            if (clearFirst)
            {
                await _browserInstance.FillElementAsync(selector, text ?? "", timeoutMs);
                _logger.LogDebug("Filled element {Selector} with new text", selector);
            }
            else
            {
                // Get current value and append the new text
                var currentValue = await _browserInstance.GetElementValueAsync(selector, timeoutMs) ?? "";
                var newText = text ?? "";
                await _browserInstance.FillElementAsync(selector, currentValue + newText, timeoutMs);
                _logger.LogDebug("Appended text to element {Selector}. Previous length: {PreviousLength}, New length: {NewLength}", 
                    selector, currentValue.Length, (currentValue + newText).Length);
            }

            _logger.LogInformation("Successfully typed text into element: {Selector}", selector);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Successfully typed text into: {selector}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to type text into element: {Selector}", selector);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get the text content of an element
    /// </summary>
    [KernelFunction, Description("Get the text content of an element by CSS selector")]
    public async Task<string> GetElementTextAsync(
        [Description("CSS selector for the element")] string selector,
        [Description("Optional timeout in milliseconds (default: 30000)")] int timeoutMs = 30000)
    {
        _logger.LogInformation("GetElementTextAsync called with selector: {Selector}, Timeout: {TimeoutMs}ms", selector, timeoutMs);
        
        try
        {
            var text = await _browserInstance.GetElementTextAsync(selector, timeoutMs);

            _logger.LogInformation("Successfully retrieved text from element: {Selector}. Text length: {TextLength}", selector, text?.Length ?? 0);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                text = text ?? ""
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get text from element: {Selector}", selector);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Take a screenshot of the current page
    /// </summary>
    [KernelFunction, 
     McpServerTool, 
     Description("Take a screenshot of the current page")
    ]
    public async Task<string> TakeScreenshotAsync(
        [Description("Screenshot format (png, jpeg) - default: png")] string format = "png",
        [Description("Full page screenshot (default: true)")] bool fullPage = true,
        [Description("Image quality for jpeg (1-100, default: 80)")] int quality = 80)
    {
        _logger.LogInformation("TakeScreenshotAsync called with format: {Format}, FullPage: {FullPage}, Quality: {Quality}", format, fullPage, quality);
        
        try
        {
            var screenshot = await _browserInstance.TakeScreenshotAsync();
            var base64Image = Convert.ToBase64String(screenshot);

            _logger.LogInformation("Successfully took screenshot. Size: {Size} bytes, Base64 length: {Base64Length}", screenshot.Length, base64Image.Length);

            return JsonSerializer.Serialize(new
            {
                success = true,
                image = base64Image,
                format = format,
                size = screenshot.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to take screenshot");
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get the current page content as HTML
    /// </summary>
    [KernelFunction, 
     McpServerTool, 
     Description("Get the current page content as HTML")
    ]
    public async Task<string> GetPageContentAsync()
    {
        _logger.LogInformation("GetPageContentAsync called");
        
        try
        {
            var content = await _browserInstance.GetHtmlContentAsync();
            var currentUrl = _browserInstance.CurrentUrl;
            var title = await _browserInstance.GetTitleAsync();

            _logger.LogInformation("Successfully retrieved page content. URL: {Url}, Title: {Title}, Content length: {ContentLength}", 
                currentUrl, title, content?.Length ?? 0);

            return JsonSerializer.Serialize(new
            {
                success = true,
                html = content,
                url = currentUrl,
                title = title
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get page content");
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Wait for an element to appear on the page
    /// </summary>
    [KernelFunction, Description("Wait for an element to appear on the page")]
    public async Task<string> WaitForElementAsync(
        [Description("CSS selector for the element to wait for")] string selector,
        [Description("Optional timeout in milliseconds (default: 30000)")] int timeoutMs = 30000)
    {
        _logger.LogInformation("WaitForElementAsync called with selector: {Selector}, Timeout: {TimeoutMs}ms", selector, timeoutMs);
        
        try
        {
            await _browserInstance.WaitForElementAsync(selector, timeoutMs);

            _logger.LogInformation("Successfully found element: {Selector}", selector);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Element found: {selector}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find element: {Selector} within {TimeoutMs}ms", selector, timeoutMs);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Scroll the page
    /// </summary>
    [KernelFunction, Description("Scroll the page by specified amount or to element")]
    public async Task<string> ScrollPageAsync(
        [Description("Pixels to scroll vertically (positive = down, negative = up)")] int deltaY = 0,
        [Description("Pixels to scroll horizontally (positive = right, negative = left)")] int deltaX = 0,
        [Description("Optional CSS selector to scroll to element")] string? scrollToElement = null)
    {
        _logger.LogInformation("ScrollPageAsync called with deltaX: {DeltaX}, deltaY: {DeltaY}, scrollToElement: {ScrollToElement}", 
            deltaX, deltaY, scrollToElement);
        
        try
        {
            if (!string.IsNullOrEmpty(scrollToElement))
            {
                await _browserInstance.ScrollToElementAsync(scrollToElement);
                _logger.LogInformation("Successfully scrolled to element: {ScrollToElement}", scrollToElement);
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = $"Scrolled to element: {scrollToElement}"
                });
            }
            else
            {
                await _browserInstance.ScrollAsync(deltaX, deltaY);
                _logger.LogInformation("Successfully scrolled by deltaX: {DeltaX}, deltaY: {DeltaY}", deltaX, deltaY);
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = $"Scrolled by deltaX: {deltaX}, deltaY: {deltaY}"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scroll. DeltaX: {DeltaX}, DeltaY: {DeltaY}, ScrollToElement: {ScrollToElement}", 
                deltaX, deltaY, scrollToElement);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Fill out a form field
    /// </summary>
    [KernelFunction, Description("Fill out a form field by CSS selector")]
    public async Task<string> FillFormFieldAsync(
        [Description("CSS selector for the form field")] string selector,
        [Description("Value to fill")] string value,
        [Description("Optional timeout in milliseconds (default: 30000)")] int timeoutMs = 30000)
    {
        _logger.LogInformation("FillFormFieldAsync called with selector: {Selector}, Value length: {ValueLength}, Timeout: {TimeoutMs}ms", 
            selector, value?.Length ?? 0, timeoutMs);
        
        try
        {
            await _browserInstance.FillElementAsync(selector, value ?? "", timeoutMs);

            _logger.LogInformation("Successfully filled form field: {Selector}", selector);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Successfully filled form field: {selector}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fill form field: {Selector}", selector);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Select option from dropdown
    /// </summary>
    [KernelFunction, Description("Select option from dropdown by CSS selector")]
    public async Task<string> SelectDropdownAsync(
        [Description("CSS selector for the dropdown/select element")] string selector,
        [Description("Value to select")] string value,
        [Description("Selection method: value, label, or index")] string method = "value",
        [Description("Optional timeout in milliseconds (default: 30000)")] int timeoutMs = 30000)
    {
        _logger.LogInformation("SelectDropdownAsync called with selector: {Selector}, Value: {Value}, Method: {Method}, Timeout: {TimeoutMs}ms", 
            selector, value, method, timeoutMs);
        
        try
        {
            await _browserInstance.SelectOptionAsync(selector, value ?? "", method, timeoutMs);

            _logger.LogInformation("Successfully selected {Method}: {Value} from dropdown: {Selector}", method, value, selector);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Successfully selected {method}: {value} from dropdown: {selector}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select {Method}: {Value} from dropdown: {Selector}", method, value, selector);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Execute JavaScript on the page
    /// </summary>
    [KernelFunction, 
     Description("Execute JavaScript code on the page. " +
                 "The script can return a value which will be included in the result. ")
    ]
    public async Task<string> ExecuteJavaScriptAsync(
        [Description("JavaScript code to execute")] string script)
    {
        _logger.LogInformation("ExecuteJavaScriptAsync called with script length: {ScriptLength}", script?.Length ?? 0);
        _logger.LogDebug("JavaScript to execute: {Script}", script);
        
        try
        {
            var result = await _browserInstance.ExecuteScriptAsync(script!);

            _logger.LogInformation("Successfully executed JavaScript. Result type: {ResultType}", result?.GetType().Name ?? "null");
            _logger.LogDebug("JavaScript execution result: {Result}", result);

            return JsonSerializer.Serialize(new
            {
                success = true,
                result = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute JavaScript: {Script}", script);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get current page information
    /// </summary>
    [KernelFunction, Description("Get current page information (URL, title, etc.)")]
    public async Task<string> GetPageInfoAsync()
    {
        _logger.LogInformation("GetPageInfoAsync called");
        
        try
        {
            var currentUrl = _browserInstance.CurrentUrl;
            var title = await _browserInstance.GetTitleAsync();
            var canGoBack = _browserInstance.CanGoBack;
            var canGoForward = _browserInstance.CanGoForward;
            var isLoading = _browserInstance.IsLoading;

            _logger.LogInformation("Retrieved page info - URL: {Url}, Title: {Title}, CanGoBack: {CanGoBack}, CanGoForward: {CanGoForward}, IsLoading: {IsLoading}", 
                currentUrl, title, canGoBack, canGoForward, isLoading);

            return JsonSerializer.Serialize(new
            {
                success = true,
                url = currentUrl,
                title = title,
                canGoBack = canGoBack,
                canGoForward = canGoForward,
                isLoading = isLoading
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get page information");
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Go back in browser history
    /// </summary>
    [KernelFunction, Description("Go back in browser history")]
    public async Task<string> GoBackAsync(
        [Description("Optional timeout in milliseconds (default: 30000)")] int timeoutMs = 30000)
    {
        _logger.LogInformation("GoBackAsync called with timeout: {TimeoutMs}ms", timeoutMs);
        
        try
        {
            await _browserInstance.GoBackAsync();

            var currentUrl = _browserInstance.CurrentUrl;
            _logger.LogInformation("Successfully navigated back to: {Url}", currentUrl);

            return JsonSerializer.Serialize(new
            {
                success = true,
                url = currentUrl,
                message = "Successfully navigated back"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate back");
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Go forward in browser history
    /// </summary>
    [KernelFunction, Description("Go forward in browser history")]
    public async Task<string> GoForwardAsync(
        [Description("Optional timeout in milliseconds (default: 30000)")] int timeoutMs = 30000)
    {
        _logger.LogInformation("GoForwardAsync called with timeout: {TimeoutMs}ms", timeoutMs);
        
        try
        {
            await _browserInstance.GoForwardAsync();

            var currentUrl = _browserInstance.CurrentUrl;
            _logger.LogInformation("Successfully navigated forward to: {Url}", currentUrl);

            return JsonSerializer.Serialize(new
            {
                success = true,
                url = currentUrl,
                message = "Successfully navigated forward"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate forward");
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Refresh the current page
    /// </summary>
    [KernelFunction, Description("Refresh the current page")]
    public async Task<string> RefreshPageAsync(
        [Description("Optional timeout in milliseconds (default: 30000)")] int timeoutMs = 30000)
    {
        _logger.LogInformation("RefreshPageAsync called with timeout: {TimeoutMs}ms", timeoutMs);
        
        try
        {
            await _browserInstance.RefreshAsync();

            var currentUrl = _browserInstance.CurrentUrl;
            _logger.LogInformation("Successfully refreshed page: {Url}", currentUrl);

            return JsonSerializer.Serialize(new
            {
                success = true,
                url = currentUrl,
                message = "Successfully refreshed page"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh page");
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}