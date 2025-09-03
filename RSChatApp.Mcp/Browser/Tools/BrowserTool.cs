using System.ComponentModel;
using Microsoft.SemanticKernel;
using RSChatApp.Mcp.Browser.Interfaces;
using Microsoft.Playwright;
using System.Text.Json;

namespace RSChatApp.Mcp.Browser.Tools;

public class BrowserTool 
{
    private readonly IBrowserInstance _browserInstance;

    public BrowserTool(IBrowserInstanceProvider browserProvider)
    {
        _browserInstance = browserProvider.GetBrowserInstance();
    }

    /// <summary>
    /// Navigate to a specific URL
    /// </summary>
    [KernelFunction, Description("Navigate to a specific URL.")]
    public async Task<string> NavigateToUrlAsync(
        [Description("The URL to navigate to")] string url,
        [Description("Optional timeout in milliseconds (default: 30000)")] int timeoutMs = 30000)
    {
        try
        {
            
            var response = await _browserInstance.Page.GotoAsync(url, new PageGotoOptions
            {
                Timeout = timeoutMs,
                WaitUntil = WaitUntilState.Load
            });

            return JsonSerializer.Serialize(new
            {
                success = true,
                url = _browserInstance.Page.Url,
                title = await _browserInstance.Page.TitleAsync(),
                status = response?.Status ?? 0
            });
        }
        catch (Exception ex)
        {
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
        try
        {
            await _browserInstance.Page.ClickAsync(selector, new PageClickOptions
            {
                Timeout = timeoutMs
            });

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Successfully clicked element: {selector}"
            });
        }
        catch (Exception ex)
        {
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
        try
        {
            if (clearFirst)
            {
                await _browserInstance.Page.FillAsync(selector, text, new PageFillOptions
                {
                    Timeout = timeoutMs
                });
            }
            else
            {
                // Clear the field first, then add the new text
                var currentValue = await _browserInstance.Page.InputValueAsync(selector);
                await _browserInstance.Page.FillAsync(selector, currentValue + text, new PageFillOptions
                {
                    Timeout = timeoutMs
                });
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Successfully typed text into: {selector}"
            });
        }
        catch (Exception ex)
        {
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
        try
        {
            var text = await _browserInstance.Page.TextContentAsync(selector, new PageTextContentOptions
            {
                Timeout = timeoutMs
            });

            return JsonSerializer.Serialize(new
            {
                success = true,
                text = text ?? ""
            });
        }
        catch (Exception ex)
        {
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
    [KernelFunction, Description("Take a screenshot of the current page")]
    public async Task<string> TakeScreenshotAsync(
        [Description("Screenshot format (png, jpeg) - default: png")] string format = "png",
        [Description("Full page screenshot (default: true)")] bool fullPage = true,
        [Description("Image quality for jpeg (1-100, default: 80)")] int quality = 80)
    {
        try
        {
            var options = new PageScreenshotOptions
            {
                FullPage = fullPage
            };

            // Set quality only for JPEG format
            if (format.ToLower() == "jpeg")
            {
                options.Quality = quality;
                options.Type = ScreenshotType.Jpeg;
            }
            else
            {
                options.Type = ScreenshotType.Png;
            }
            
            var screenshot = await _browserInstance.Page.ScreenshotAsync(options);

            var base64Image = Convert.ToBase64String(screenshot);

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
    [KernelFunction, Description("Get the current page content as HTML")]
    public async Task<string> GetPageContentAsync()
    {
        try
        {
            var content = await _browserInstance.Page.ContentAsync();

            return JsonSerializer.Serialize(new
            {
                success = true,
                html = content,
                url = _browserInstance.Page.Url,
                title = await _browserInstance.Page.TitleAsync()
            });
        }
        catch (Exception ex)
        {
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
        try
        {
            await _browserInstance.Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
            {
                Timeout = timeoutMs,
                State = WaitForSelectorState.Visible
            });

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Element found: {selector}"
            });
        }
        catch (Exception ex)
        {
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
        try
        {
            if (!string.IsNullOrEmpty(scrollToElement))
            {
                await _browserInstance.Page.Locator(scrollToElement).ScrollIntoViewIfNeededAsync();
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = $"Scrolled to element: {scrollToElement}"
                });
            }
            else
            {
                await _browserInstance.Page.Mouse.WheelAsync(deltaX, deltaY);
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = $"Scrolled by deltaX: {deltaX}, deltaY: {deltaY}"
                });
            }
        }
        catch (Exception ex)
        {
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
        try
        {
            await _browserInstance.Page.FillAsync(selector, value, new PageFillOptions
            {
                Timeout = timeoutMs
            });

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Successfully filled form field: {selector}"
            });
        }
        catch (Exception ex)
        {
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
        try
        {
            switch (method.ToLower())
            {
                case "value":
                    await _browserInstance.Page.SelectOptionAsync(selector, value, new PageSelectOptionOptions
                    {
                        Timeout = timeoutMs
                    });
                    break;
                case "label":
                    await _browserInstance.Page.SelectOptionAsync(selector, new SelectOptionValue { Label = value }, new PageSelectOptionOptions
                    {
                        Timeout = timeoutMs
                    });
                    break;
                case "index":
                    if (int.TryParse(value, out var index))
                    {
                        await _browserInstance.Page.SelectOptionAsync(selector, new SelectOptionValue { Index = index }, new PageSelectOptionOptions
                        {
                            Timeout = timeoutMs
                        });
                    }
                    else
                    {
                        throw new ArgumentException("Invalid index value for dropdown selection");
                    }
                    break;
                default:
                    throw new ArgumentException("Invalid selection method. Use 'value', 'label', or 'index'");
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Successfully selected {method}: {value} from dropdown: {selector}"
            });
        }
        catch (Exception ex)
        {
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
    [KernelFunction, Description("Execute JavaScript code on the page")]
    public async Task<string> ExecuteJavaScriptAsync(
        [Description("JavaScript code to execute")] string script)
    {
        try
        {
            var result = await _browserInstance.Page.EvaluateAsync(script);

            return JsonSerializer.Serialize(new
            {
                success = true,
                result = result
            });
        }
        catch (Exception ex)
        {
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
        try
        {
            return JsonSerializer.Serialize(new
            {
                success = true,
                url = _browserInstance.Page.Url,
                title = await _browserInstance.Page.TitleAsync(),
                viewport = _browserInstance.Page.ViewportSize
            });
        }
        catch (Exception ex)
        {
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
        try
        {
            await _browserInstance.Page.GoBackAsync(new PageGoBackOptions
            {
                Timeout = timeoutMs,
                WaitUntil = WaitUntilState.Load
            });

            return JsonSerializer.Serialize(new
            {
                success = true,
                url = _browserInstance.Page.Url,
                message = "Successfully navigated back"
            });
        }
        catch (Exception ex)
        {
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
        try
        {
            await _browserInstance.Page.GoForwardAsync(new PageGoForwardOptions
            {
                Timeout = timeoutMs,
                WaitUntil = WaitUntilState.Load
            });

            return JsonSerializer.Serialize(new
            {
                success = true,
                url = _browserInstance.Page.Url,
                message = "Successfully navigated forward"
            });
        }
        catch (Exception ex)
        {
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
        try
        {
            await _browserInstance.Page.ReloadAsync(new PageReloadOptions
            {
                Timeout = timeoutMs,
                WaitUntil = WaitUntilState.Load
            });

            return JsonSerializer.Serialize(new
            {
                success = true,
                url = _browserInstance.Page.Url,
                message = "Successfully refreshed page"
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}