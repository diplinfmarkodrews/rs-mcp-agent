using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RSChatApp.Mcp.Browser.Configuration;
using RSChatApp.Mcp.Browser.Interfaces;
using System.Threading;

namespace RSChatApp.Mcp.Browser.Infrastructure;

public class PlayWrightBrowserInstance : IBrowserInstance
{
    public IBrowser Browser => _browser;
    public IBrowserContext BrowserContext
    {
        get => _context ?? throw new InvalidOperationException("Browser context not initialized. Call NewContextAsync first.");
        set => _context = value;
    }

    public IPage Page
    {
        get => _page ?? throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");
        set => _page = value;
    }

    public string SessionId
    {
        get => _config.SessionId;
    }

    public string CurrentUrl => _page?.Url ?? "about:blank";

    public bool CanGoBack => _canGoBack;

    public bool CanGoForward => _canGoForward;

    public bool IsLoading => _isLoading;

    // Debounce Configuration from config
    public int NavigationDebounceMs 
    { 
        get => _config.NavigationDebounceMs; 
        set => _config.NavigationDebounceMs = value; 
    }
    public int ContentRefreshDebounceMs 
    { 
        get => _config.ContentRefreshDebounceMs; 
        set => _config.ContentRefreshDebounceMs = value; 
    }
    public int UserInteractionDebounceMs 
    { 
        get => _config.UserInteractionDebounceMs; 
        set => _config.UserInteractionDebounceMs = value; 
    }

    public event EventHandler<IBrowserInstance> Disconnected = delegate { };
    public event NavigationStartedEventHandler NavigationStarted = delegate { return Task.CompletedTask; };
    public event NavigationCompletedEventHandler NavigationCompleted = delegate { return Task.CompletedTask; };
    public event NavigationFailedEventHandler NavigationFailed = delegate { return Task.CompletedTask; };
    public event UrlChangedEventHandler UrlChanged = delegate { return Task.CompletedTask; };
    public event TitleChangedEventHandler TitleChanged = delegate { return Task.CompletedTask; };
    public event LoadingStateChangedEventHandler LoadingStateChanged = delegate { return Task.CompletedTask; };
    public event PageErrorEventHandler PageError = delegate { return Task.CompletedTask; };

    private readonly ILogger<PlayWrightBrowserInstance> _logger;    
    private readonly IBrowser _browser;
    private readonly BrowserInstanceConfiguration _config;
    private IBrowserContext? _context;
    private IPage? _page;
    private bool _canGoBack;
    private bool _canGoForward;
    private bool _isLoading;
    private string _currentTitle = string.Empty;
    
    // Debounce timers
    private Timer? _navigationDebounceTimer;
    private Timer? _contentRefreshDebounceTimer;
    private Timer? _userInteractionDebounceTimer;
    private string? _pendingNavigationUrl;


    public PlayWrightBrowserInstance(
        ILogger<PlayWrightBrowserInstance> logger,
        BrowserInstanceConfiguration config,
        IBrowser browser)
    {
        _logger = logger;
        _config = config;   
        _browser = browser;        
        _browser.Disconnected += OnDisconnected;
    }

    // Helper method to safely invoke events with proper error handling
    private async Task SafeInvokeEventAsync(Func<Task> eventInvocation)
    {
        try
        {
            // Use ConfigureAwait(false) to avoid deadlocks and let Blazor handle threading
            await eventInvocation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking event: {Message}", ex.Message);
        }
    }

    // Helper method to safely get page title with fallback handling
    private async Task<string> SafeGetTitleAsync()
    {
        if (_page == null)
            return string.Empty;

        try
        {
            var title = await _page.TitleAsync();
            _currentTitle = title; // Cache the title for fallback
            return title;
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Execution context was destroyed"))
        {
            // Use the last known title during navigation
            var fallback = _currentTitle ?? "Loading...";
            _logger.LogDebug("Using fallback title during navigation: {Title}", fallback);
            return fallback;
        }
        catch (Exception ex)
        {
            // Other title-related errors
            var fallback = _currentTitle ?? "Unknown";
            _logger.LogWarning(ex, "Failed to get page title, using fallback: {Title}", fallback);
            return fallback;
        }
    }

    event DisconnectedEventHandler IBrowserInstance.Disconnected
    {
        add => _disconnectedHandlers += value;
        remove => _disconnectedHandlers -= value;
    }

    private DisconnectedEventHandler? _disconnectedHandlers;

    private async void OnDisconnected(object? sender, IBrowser browser)
    {
        _logger.LogWarning("Browser disconnected");
        Disconnected?.Invoke(this, this);
        if (_disconnectedHandlers != null)
        {
            await SafeInvokeEventAsync(() => _disconnectedHandlers.Invoke());
        }
    }
    public Task LoginAsync(string username, string password)
    {
        // Implementation depends on specific login requirements
        // This is a placeholder for the actual login logic
        _logger.LogInformation("Login method called for user: {Username}", username);
        return Task.CompletedTask;
    }

    public async Task NewContextAsync()
    {
        if (_page != null)
        {
            await _page.CloseAsync();
        }
        if (_context != null)
        {
            await _context.DisposeAsync();
        }
        
        _logger.LogInformation("Creating new browser context with debounce settings - Navigation: {NavMs}ms, Content: {ContentMs}ms, Interaction: {InteractionMs}ms",
            NavigationDebounceMs, ContentRefreshDebounceMs, UserInteractionDebounceMs);
            
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = _config.UserAgent,
            Locale = _config.Language,
            ViewportSize = new ViewportSize { Width = _config.Width_Viewport, Height = _config.Height_Viewport }
        });
        _page = await _context.NewPageAsync();
        
        // Set up request interception to block tracking
        await SetupRequestInterception();
        
        // Set up page event handlers
        SetupPageEventHandlers();
        
        try
        {
            await _page.GotoAsync(_config.BaseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to {url}", _config.BaseUrl);
        }
    }

    public Task LogoutAsync()
    {
        _logger.LogInformation("Logout method called");
        return Task.CompletedTask;
    }
    
    public async ValueTask DisposeAsync()
    {
        _browser.Disconnected -= OnDisconnected;
        
        // Dispose debounce timers
        _navigationDebounceTimer?.Dispose();
        _contentRefreshDebounceTimer?.Dispose();
        _userInteractionDebounceTimer?.Dispose();
        
        if (_page != null)
        {
            await _page.CloseAsync();
        }
        
        if (_context != null)
        {
            await _context.CloseAsync();
            await _context.DisposeAsync();
        }

        await _browser.CloseAsync();
        await _browser.DisposeAsync();
    }

    public async Task NavigateAsync(string url)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        var normalizedUrl = NormalizeUrl(url);
        _logger.LogDebug("Navigation requested to: {Url} (debounce: {DebounceMs}ms)", normalizedUrl, NavigationDebounceMs);

        // Store the pending URL
        _pendingNavigationUrl = normalizedUrl;

        // Debounce navigation calls
        _navigationDebounceTimer?.Dispose();
        _navigationDebounceTimer = new Timer(async _ =>
        {
            if (_pendingNavigationUrl != null)
            {
                await PerformNavigationAsync(_pendingNavigationUrl);
                _pendingNavigationUrl = null;
            }
        }, null, NavigationDebounceMs, Timeout.Infinite);

        await Task.CompletedTask; // Make method async
    }
    
    private async Task PerformNavigationAsync(string normalizedUrl)
    {
        if (_page == null) return;

        try
        {
            _logger.LogDebug("Performing navigation to: {Url}", normalizedUrl);
            
            // Fire navigation started event
            await SafeInvokeEventAsync(() => NavigationStarted.Invoke(normalizedUrl));
            
            _isLoading = true;
            await SafeInvokeEventAsync(() => LoadingStateChanged.Invoke(_isLoading));

            var response = await _page.GotoAsync(normalizedUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = 30000
            });

            // Update navigation state
            await UpdateNavigationState();
            
            var title = await SafeGetTitleAsync();
            
            // Fire completion events
            await SafeInvokeEventAsync(() => NavigationCompleted.Invoke(normalizedUrl, title));
            await SafeInvokeEventAsync(() => UrlChanged.Invoke(normalizedUrl));
            await SafeInvokeEventAsync(() => TitleChanged.Invoke(title));

            _logger.LogDebug("Navigation completed. URL: {Url}, Title: {Title}", normalizedUrl, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation failed to {Url}: {Message}", normalizedUrl, ex.Message);
            await SafeInvokeEventAsync(() => NavigationFailed.Invoke(normalizedUrl, ex.Message));
        }
        finally
        {
            _isLoading = false;
            await SafeInvokeEventAsync(() => LoadingStateChanged.Invoke(_isLoading));
        }
    }

    public async Task RefreshAsync()
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        try
        {
            _logger.LogDebug("Refreshing page");
            
            _isLoading = true;
            await SafeInvokeEventAsync(() => LoadingStateChanged.Invoke(_isLoading));

            await _page.ReloadAsync(new PageReloadOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = 30000
            });

            await UpdateNavigationState();
            
            var url = _page.Url;
            var title = await SafeGetTitleAsync();
            
            await SafeInvokeEventAsync(() => UrlChanged.Invoke(url));
            await SafeInvokeEventAsync(() => TitleChanged.Invoke(title));

            _logger.LogDebug("Page refreshed. URL: {Url}, Title: {Title}", url, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh failed: {Message}", ex.Message);
            throw;
        }
        finally
        {
            _isLoading = false;
            await SafeInvokeEventAsync(() => LoadingStateChanged.Invoke(_isLoading));
        }
    }

    public async Task GoBackAsync()
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        try
        {
            _logger.LogDebug("Going back in browser history");
            
            _isLoading = true;
            await SafeInvokeEventAsync(() => LoadingStateChanged.Invoke(_isLoading));

            await _page.GoBackAsync(new PageGoBackOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = 30000
            });

            await UpdateNavigationState();
            
            var url = _page.Url;
            var title = await SafeGetTitleAsync();
            
            await SafeInvokeEventAsync(() => UrlChanged.Invoke(url));
            await SafeInvokeEventAsync(() => TitleChanged.Invoke(title));

            _logger.LogDebug("Navigated back. URL: {Url}, Title: {Title}", url, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Go back failed: {Message}", ex.Message);
            throw;
        }
        finally
        {
            _isLoading = false;
            await SafeInvokeEventAsync(() => LoadingStateChanged.Invoke(_isLoading));
        }
    }

    public async Task GoForwardAsync()
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        try
        {
            _logger.LogDebug("Going forward in browser history");
            
            _isLoading = true;
            await SafeInvokeEventAsync(() => LoadingStateChanged.Invoke(_isLoading));

            await _page.GoForwardAsync(new PageGoForwardOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = 30000
            });

            await UpdateNavigationState();
            
            var url = _page.Url;
            var title = await SafeGetTitleAsync();
            
            await SafeInvokeEventAsync(() => UrlChanged.Invoke(url));
            await SafeInvokeEventAsync(() => TitleChanged.Invoke(title));

            _logger.LogDebug("Navigated forward. URL: {Url}, Title: {Title}", url, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Go forward failed: {Message}", ex.Message);
            throw;
        }
        finally
        {
            _isLoading = false;
            await SafeInvokeEventAsync(() => LoadingStateChanged.Invoke(_isLoading));
        }
    }

    public async Task<string> GetTitleAsync()
    {
        return await SafeGetTitleAsync();
    }

    public async Task<string> GetHtmlContentAsync()
    {
        if (_page == null)
            return string.Empty;

        _logger.LogInformation("🚀 TRACE: GetHtmlContentAsync called");
        _logger.LogInformation("Content refresh requested (debounce: {DebounceMs}ms)", ContentRefreshDebounceMs);

        return await DebouncedContentRefreshAsync();
    }

    private async Task<string> DebouncedContentRefreshAsync()
    {
        var tcs = new TaskCompletionSource<string>();

        _contentRefreshDebounceTimer?.Dispose();
        _contentRefreshDebounceTimer = new Timer(async _ =>
        {
            try
            {
                if (_page != null)
                {
                    _logger.LogInformation("🔧 TRACE: Performing content refresh");
                    var content = await _page.ContentAsync();
                    _logger.LogInformation("🔧 TRACE: Retrieved page content, length: {Length}", content.Length);
                    
                    tcs.SetResult(content);
                }
                else
                {
                    tcs.SetResult(string.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get page content: {Message}", ex.Message);
                tcs.SetResult(string.Empty);
            }
        }, null, ContentRefreshDebounceMs, Timeout.Infinite);

        return await tcs.Task;
    }

    public async Task<byte[]> TakeScreenshotAsync()
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        try
        {
            return await _page.ScreenshotAsync(new PageScreenshotOptions
            {
                FullPage = true,
                Type = ScreenshotType.Png
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to take screenshot: {Message}", ex.Message);
            throw;
        }
    }

    public async Task ClickAsync(double x, double y)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        _logger.LogInformation("🎯 PLAYWRIGHT CLICK: Executing click at ({X}, {Y}) (debounce: {DebounceMs}ms)", x, y, UserInteractionDebounceMs);

        await DebouncedUserInteractionAsync(async () =>
        {
            try
            {
                // First try to wait for the page to be ready
                await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = 1000 });
                
                // Try to click with a more robust approach
                await _page.Mouse.ClickAsync((float)x, (float)y, new MouseClickOptions
                {
                    Button = MouseButton.Left,
                    ClickCount = 1,
                    Delay = 50 // Small delay between mousedown and mouseup
                });
                
                _logger.LogInformation("🎯 PLAYWRIGHT CLICK COMPLETED: Successfully clicked at coordinates ({X}, {Y})", x, y);
                
                // Wait a bit for any navigation or changes to start
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("🎯 PLAYWRIGHT CLICK WARNING: Click at ({X}, {Y}) completed with warning: {Message}", x, y, ex.Message);
                
                // Fallback to basic click if enhanced click fails
                await _page.Mouse.ClickAsync((float)x, (float)y);
                _logger.LogInformation("🎯 PLAYWRIGHT CLICK FALLBACK: Used basic click at coordinates ({X}, {Y})", x, y);
            }
        });
    }

    public async Task HoverAsync(double x, double y)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        _logger.LogDebug("Hover requested at ({X}, {Y}) - no debounce for responsive hover", x, y);

        // Don't debounce hover for better responsiveness with popups
        try
        {
            await _page.Mouse.MoveAsync((float)x, (float)y);
            _logger.LogDebug("Hovered at coordinates ({X}, {Y})", x, y);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hover at coordinates ({X}, {Y}): {Message}", x, y, ex.Message);
            throw;
        }
    }

    public async Task TypeAsync(string text)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        _logger.LogDebug("Type requested: {Text} (debounce: {DebounceMs}ms)", text, UserInteractionDebounceMs);

        await DebouncedUserInteractionAsync(async () =>
        {
            await _page.Keyboard.TypeAsync(text);
            _logger.LogDebug("Typed text: {Text}", text);
        });
    }

    public async Task KeyPressAsync(string key)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        _logger.LogDebug("Key press requested: {Key} (debounce: {DebounceMs}ms)", key, UserInteractionDebounceMs);

        await DebouncedUserInteractionAsync(async () =>
        {
            await _page.Keyboard.PressAsync(key);
            _logger.LogDebug("Pressed key: {Key}", key);
        });
    }

    public async Task KeyDownAsync(string key)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        _logger.LogDebug("Key down requested: {Key}", key);

        await DebouncedUserInteractionAsync(async () =>
        {
            await _page.Keyboard.DownAsync(key);
            _logger.LogDebug("Key down: {Key}", key);
        });
    }

    public async Task KeyUpAsync(string key)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        _logger.LogDebug("Key up requested: {Key}", key);

        await DebouncedUserInteractionAsync(async () =>
        {
            await _page.Keyboard.UpAsync(key);
            _logger.LogDebug("Key up: {Key}", key);
        });
    }

    private async Task DebouncedUserInteractionAsync(Func<Task> action)
    {
        var tcs = new TaskCompletionSource();

        _userInteractionDebounceTimer?.Dispose();
        _userInteractionDebounceTimer = new Timer(async _ =>
        {
            try
            {
                _logger.LogDebug("Performing debounced user interaction");
                await action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User interaction failed: {Message}", ex.Message);
                tcs.SetException(ex);
            }
        }, null, UserInteractionDebounceMs, Timeout.Infinite);

        await tcs.Task;
    }

    private string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "about:blank";

        url = url.Trim();

        // If already has a protocol, return as-is
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        // Handle localhost and IP addresses
        if (url.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("127.0.0.1") ||
            System.Text.RegularExpressions.Regex.IsMatch(url, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
        {
            return "http://" + url;
        }

        // Default to HTTPS for domain names
        return "https://" + url;
    }

    public async Task WaitForLoadAsync()
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        try
        {
            await _page.WaitForLoadStateAsync(LoadState.Load, new PageWaitForLoadStateOptions
            {
                Timeout = 30000
            });
            _logger.LogDebug("Page load completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to wait for page load: {Message}", ex.Message);
            throw;
        }
    }

    public async Task WaitForStablePageAsync()
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        try
        {
            // Wait for network to be mostly idle (useful for cookie banners and dynamic content)
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
            {
                Timeout = 10000
            });
            _logger.LogDebug("Page network activity settled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to wait for network idle state, continuing anyway: {Message}", ex.Message);
            // Don't throw here as this is a best-effort attempt
        }
    }

    private async Task UpdateNavigationState()
    {
        if (_page == null) return;

        try
        {
            // Simple heuristic for navigation state
            // Note: Playwright doesn't provide direct canGoBack/canGoForward APIs
            // This is a simplified implementation
            
            // Try to determine if we can go back/forward by checking history
            var canGoBackResult = await _page.EvaluateAsync<bool>(@"
                () => window.history.length > 1
            ");
            
            _canGoBack = canGoBackResult;
            _canGoForward = false; // Simplified - would need more complex logic
            
            _logger.LogDebug("Navigation state updated. CanGoBack: {CanGoBack}, CanGoForward: {CanGoForward}", 
                _canGoBack, _canGoForward);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update navigation state: {Message}", ex.Message);
            // Set defaults on error
            _canGoBack = false;
            _canGoForward = false;
        }
    }

    private async Task SetupRequestInterception()
    {
        if (_page == null) return;

        try
        {
            // Enable request interception
            await _page.RouteAsync("**/*", async route =>
            {
                var request = route.Request;
                var url = request.Url;

                // Block tracking and analytics requests
                if (url.Contains("google.com/ccm/collect") ||
                    url.Contains("googletagmanager.com") ||
                    url.Contains("google-analytics.com") ||
                    url.Contains("analytics.google.com") ||
                    url.Contains("doubleclick.net") ||
                    url.Contains("googlesyndication.com") ||
                    url.Contains("/gtag/") ||
                    url.Contains("/ga.js") ||
                    url.Contains("/analytics.js"))
                {
                    _logger.LogDebug("Blocking tracking request: {Url}", url);
                    await route.AbortAsync();
                    return;
                }

                // Allow all other requests
                await route.ContinueAsync();
            });

            _logger.LogDebug("Request interception setup complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup request interception: {Message}", ex.Message);
        }
    }

    private void SetupPageEventHandlers()
    {
        if (_page == null) return;

        // Wire up Playwright events to our typed events
        _page.Response += async (sender, response) =>
        {
            try
            {
                var url = response.Url;
                var title = await SafeGetTitleAsync();
                
                // Execute event callbacks safely to avoid dispatcher issues
                await SafeInvokeEventAsync(() => NavigationCompleted.Invoke(url, title));
                await SafeInvokeEventAsync(() => UrlChanged.Invoke(url));
                await SafeInvokeEventAsync(() => TitleChanged.Invoke(title));
                
                await UpdateNavigationState();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in response event handler: {Message}", ex.Message);
            }
        };

        _page.PageError += async (sender, error) =>
        {
            try
            {
                await SafeInvokeEventAsync(() => PageError.Invoke(error, _page.Url));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in page error event handler: {Message}", ex.Message);
            }
        };

        _page.Load += async (sender, page) =>
        {
            try
            {
                _isLoading = false;
                await SafeInvokeEventAsync(() => LoadingStateChanged.Invoke(_isLoading));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in load event handler: {Message}", ex.Message);
            }
        };
    }

    // Element-based interactions for BrowserTool
    public async Task ClickElementAsync(string selector, int timeoutMs = 30000)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        await DebouncedUserInteractionAsync(async () =>
        {
            await _page.ClickAsync(selector, new PageClickOptions { Timeout = timeoutMs });
            _logger.LogDebug("Clicked element: {Selector}", selector);
        });
    }

    public async Task FillElementAsync(string selector, string value, int timeoutMs = 30000)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        await DebouncedUserInteractionAsync(async () =>
        {
            await _page.FillAsync(selector, value, new PageFillOptions { Timeout = timeoutMs });
            _logger.LogDebug("Filled element {Selector} with value: {Value}", selector, value);
        });
    }

    public async Task<string?> GetElementTextAsync(string selector, int timeoutMs = 30000)
    {
        if (_page == null)
            return null;

        try
        {
            var text = await _page.TextContentAsync(selector, new PageTextContentOptions { Timeout = timeoutMs });
            _logger.LogDebug("Got text from element {Selector}: {Text}", selector, text);
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get text from element {Selector}: {Message}", selector, ex.Message);
            return null;
        }
    }

    public async Task<string?> GetElementValueAsync(string selector, int timeoutMs = 30000)
    {
        if (_page == null)
            return null;

        try
        {
            var value = await _page.InputValueAsync(selector, new PageInputValueOptions { Timeout = timeoutMs });
            _logger.LogDebug("Got value from element {Selector}: {Value}", selector, value);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get value from element {Selector}: {Message}", selector, ex.Message);
            return null;
        }
    }

    public async Task WaitForElementAsync(string selector, int timeoutMs = 30000)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        try
        {
            await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions 
            { 
                Timeout = timeoutMs,
                State = WaitForSelectorState.Visible 
            });
            _logger.LogDebug("Element found: {Selector}", selector);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to wait for element {Selector}: {Message}", selector, ex.Message);
            throw;
        }
    }

    public async Task ScrollToElementAsync(string selector, int timeoutMs = 30000)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        await DebouncedUserInteractionAsync(async () =>
        {
            await _page.Locator(selector).ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions { Timeout = timeoutMs });
            _logger.LogDebug("Scrolled to element: {Selector}", selector);
        });
    }

    public async Task SelectOptionAsync(string selector, string value, string method = "value", int timeoutMs = 30000)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        await DebouncedUserInteractionAsync(async () =>
        {
            switch (method.ToLower())
            {
                case "value":
                    await _page.SelectOptionAsync(selector, value, new PageSelectOptionOptions { Timeout = timeoutMs });
                    break;
                case "label":
                    await _page.SelectOptionAsync(selector, new SelectOptionValue { Label = value }, new PageSelectOptionOptions { Timeout = timeoutMs });
                    break;
                case "index":
                    if (int.TryParse(value, out var index))
                    {
                        await _page.SelectOptionAsync(selector, new SelectOptionValue { Index = index }, new PageSelectOptionOptions { Timeout = timeoutMs });
                    }
                    else
                    {
                        throw new ArgumentException("Invalid index value for dropdown selection");
                    }
                    break;
                default:
                    throw new ArgumentException("Invalid selection method. Use 'value', 'label', or 'index'");
            }
            _logger.LogDebug("Selected option {Method}: {Value} from dropdown: {Selector}", method, value, selector);
        });
    }

    public async Task<object?> ExecuteScriptAsync(string script)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        try
        {
            var result = await _page.EvaluateAsync(script);
            _logger.LogDebug("Executed script: {Script}", script);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute script: {Message}", ex.Message);
            throw;
        }
    }

    public async Task ScrollAsync(int deltaX, int deltaY)
    {
        if (_page == null)
            throw new InvalidOperationException("Browser page not initialized. Call NewContextAsync first.");

        await DebouncedUserInteractionAsync(async () =>
        {
            await _page.Mouse.WheelAsync(deltaX, deltaY);
            _logger.LogDebug("Scrolled by deltaX: {DeltaX}, deltaY: {DeltaY}", deltaX, deltaY);
        });
    }
}