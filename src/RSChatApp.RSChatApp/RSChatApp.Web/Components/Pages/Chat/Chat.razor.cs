using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using RSChatApp.Infrastructure.Prompt;
using RSChatApp.Infrastructure.UserInteraction;
using RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.Processing;
using RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Mcp;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Mcp;
using RSChatApp.Web.Components.Pages.Terminal;
using RSChatApp.Web.Mcp.Tools;
using RSChatApp.Web.Services.UserConfirmation;
using RSChatApp.Web.Storage;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using TextContent = Microsoft.Extensions.AI.TextContent;

namespace RSChatApp.Web.Components.Pages.Chat;

public partial class Chat(
    IChatClient chatClient,
    Kernel kernel,
    IStorage<List<ChatMessage>> chatHistoryStorage,
    SemanticSearchTool semanticSearchTool, 
    DocumentLookupTool documentLookupTool,
    ScriptStoreTool scriptStoreTool,
    AuthenticationTool authenticationTool,
    UserConfirmedTerminalTool userConfirmedTerminalTool,
    IPromptService promptService,
    ILogger<Chat> logger,
    IWaitForUserInteraction<TerminalConfirmRequest, UserConfirmationResult> terminalUserInteraction
    ) : ComponentBase, IDisposable
{
    private string SystemPrompt => promptService.GetPrompt(new SystemPromptRequest(AddFileNames: true));

    private readonly ChatOptions _chatOptions = new();
    private readonly List<ChatMessage> _messages = new();
    private CancellationTokenSource? _currentResponseCancellation;
    private ChatMessage? _currentResponseMessage;
    private ChatInput? _chatInput;
    private ChatSuggestions? _chatSuggestions;
    private bool _isOllama;
    private bool _terminalVisible = false;
    private TerminalManager? _terminalManager;
    private int _terminalHeight = 200;

    private TerminalConfirmRequest? _pendingTerminalRequest;
    private TaskCompletionSource<UserConfirmationResult>? _pendingTerminalTcs;
    
    [Experimental("SKEXP0001")]
    protected override async Task OnInitializedAsync()
    {
        terminalUserInteraction.UserInteractionRequested += OnTerminalUserInteractionRequested;

        // Since new ollama version supports stream & tool calls, will refactor to stream api only
        // TODO: Test first with ollama version that supports both streaming and tool calls
        _isOllama = false;
        
        // Debug logging to see what kernel plugins are available
        var kernelPlugins = kernel.Plugins.ToList();
        logger.LogDebug("Total kernel plugins available: {kernelPluginsCount}: \n{kernelPluginsNames} ", kernelPlugins.Count, 
            string.Join(", ", kernelPlugins.Select(p=> p.Name)));

        // Create a list of all tools (local search + kernel MCP tools)
        var allTools = new List<AITool>
        {
            AIFunctionFactory.Create(semanticSearchTool.SearchAsync,  "Search", "Search for information using a phrase or keyword"),
            AIFunctionFactory.Create(documentLookupTool.GetDocumentPage, "GetDocumentPage", "Lookup a page of a given document, optionally with all images."),
            AIFunctionFactory.Create(scriptStoreTool.GetAllScriptPaths, "GetAllScriptsPath", "Retrieve a list of all scripts path"),
            AIFunctionFactory.Create(scriptStoreTool.GetText, "GetTextFileFromPath","Get a text file of a given path. can be scripts or other text files")
            // AIFunctionFactory.Create(AuthenticationTool.IsAuthenticatedAsync, "IsAuthenticated", "Checks whether the user is authenticated against the ReportServer and can execute ReportServerMcp tools or not"),
            // AIFunctionFactory.Create(AuthenticationTool.LoginUserRequestedAsync, "RequestLogin", "Requests the user to login when they need to access ReportServer MCP tools but are not authenticated"),
            // AIFunctionFactory.Create(UserConfirmedTerminalTool.ExecuteCommandAsync, "MultiTerminalTool", "Executes commands in the terminal with user confirmation. Valid terminal types are ")
        };
        
        // Add MCP tools
        foreach (var plugin in kernelPlugins)
        {
            foreach (var aiFunction in plugin)
            {
                allTools.Add(aiFunction.AsAIFunction(kernel));
            }
        }
        logger.LogInformation("Total tools registered for chat: {allToolsCount}: \n{allTools} ", allTools.Count, 
            string.Join(", \n", allTools.Select(p=> p.Name)));
        
        _chatOptions.Tools = allTools;              
        // Load chat history
        await InitChatHistoryAsync();
    }

    private void OnTerminalUserInteractionRequested(object? sender,
        (TerminalConfirmRequest Request, TaskCompletionSource<UserConfirmationResult> TaskCompletionSource) args)
    {
        // Ensure only one pending interaction at a time.
        _pendingTerminalTcs?.TrySetResult(
            new UserConfirmationResult(
                UserConfirmationResultEnum.Cancelled));

        _pendingTerminalRequest = args.Request;
        _pendingTerminalTcs = args.TaskCompletionSource;
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task ResolveTerminalConfirmationAsync(UserConfirmationResult result)
    {
        if (_pendingTerminalTcs is null)
        {
            return;
        }
        logger.LogInformation("Resolving terminal confirmation: {result}", result);
        _pendingTerminalTcs.TrySetResult(result);
        _pendingTerminalTcs = null;
        _pendingTerminalRequest = null;
        await InvokeAsync(StateHasChanged);
    }
    
    private async Task StoreChatHistoryAsync()
    {
        await chatHistoryStorage.SaveAsync(_messages);
        logger.LogInformation("Chat history saved with {messageCount} messages", _messages.Count);
    }
    private async Task InitChatHistoryAsync()
    {
        // Try loading chat history from browser storage
        var chatHistory = await chatHistoryStorage.GetAsync()
            .ConfigureAwait(true);
        
        _messages.Clear();
        
        if (chatHistory.Success && chatHistory.Value!.Count > 0)
        {
            logger.LogInformation("Loaded {chatHistoryCount} messages from chat history", chatHistory.Value!.Count);
            _messages.AddRange(chatHistory.Value);
            _chatSuggestions?.Update(_messages);
            
            // Trigger UI update
            await InvokeAsync(StateHasChanged);
            
            // Focus the input after loading
            if (_chatInput is not null)
            {
                await _chatInput.FocusAsync();
            }
        }
        else
        {
            logger.LogInformation("No chat history found, starting new conversation");
            _messages.Add(new(ChatRole.System, SystemPrompt));
        }
    }
    
    private Task AddUserMessageAsync(ChatMessage userMessage)
    {
        if (_isOllama)
            return AddUserMessageSingleAsync(userMessage);
        
        return AddUserMessageStreamAsync(userMessage);

    }
    private async Task AddUserMessageStreamAsync(ChatMessage userMessage)
    {
        CancelAnyCurrentResponse();

        // Add the user message to the conversation
        _messages.Add(userMessage);
        _chatSuggestions?.Clear();
        await _chatInput!.FocusAsync();

        // Display a new response from the IChatClient with streaming
        _currentResponseCancellation = new();
        
        // Track text for display and all contents
        var contentBuilder = new StringBuilder();
        var allContents = new List<AIContent>();

        try
        {
            // Normalize messages for API (split FunctionCallContent and FunctionResultContent into separate messages)
            var normalizedMessages = _messages.NormalizeMessagesForApi();
            
            // Use streaming API to get progressive responses
            await foreach (var update in chatClient.GetStreamingResponseAsync(normalizedMessages, _chatOptions, _currentResponseCancellation.Token))
            {
                // Collect ALL content types from each update
                foreach (var content in update.Contents)
                {
                    allContents.Add(content);
                    
                    // Also build text for display during streaming
                    if (content is TextContent textContent)
                    {
                        contentBuilder.Append(textContent.Text);
                    }
                }
                
                // Update current message with ALL collected contents for live rendering
                var streamingContents = new List<AIContent>();
                if (contentBuilder.Length > 0)
                {
                    streamingContents.Add(new TextContent(contentBuilder.ToString()));
                }
                // Add all non-text contents (FunctionCallContent, FunctionResultContent, etc.)
                streamingContents.AddRange(allContents.Where(c => c is not TextContent));

                _currentResponseMessage = new ChatMessage(ChatRole.Assistant, streamingContents.NormalizeAssistantContents());
                
                // Trigger UI update to show streaming content
                await InvokeAsync(StateHasChanged);
                
                // Check for cancellation
                _currentResponseCancellation.Token.ThrowIfCancellationRequested();
            }

            // Add the complete message with all contents (tool calls, results, text)
            if (allContents.Count > 0)
            {
                // Combine multiple TextContent chunks into one, keep other content types as-is
                var consolidatedContents = new List<AIContent>();
                var hasText = contentBuilder.Length > 0;
                
                // Add consolidated text as single TextContent
                if (hasText)
                {
                    consolidatedContents.Add(new TextContent(contentBuilder.ToString()));
                }
                
                // Add all non-TextContent items (FunctionCallContent, FunctionResultContent, etc.)
                consolidatedContents.AddRange(allContents.Where(c => c is not TextContent));

                var responseMessage = new ChatMessage(ChatRole.Assistant, consolidatedContents.NormalizeAssistantContents());
                _messages.Add(responseMessage);
                
                logger.LogInformation("Added response message with {contentCount} contents", consolidatedContents.Count);
                foreach (var content in consolidatedContents)
                {
                    logger.LogInformation("  Content type: {contentType}", content.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation - add partial response if available
            if (allContents.Count > 0)
            {
                var consolidatedContents = new List<AIContent>();
                if (contentBuilder.Length > 0)
                {
                    consolidatedContents.Add(new TextContent(contentBuilder.ToString()));
                }
                consolidatedContents.AddRange(allContents.Where(c => c is not TextContent));

                var responseMessage = new ChatMessage(ChatRole.Assistant, consolidatedContents.NormalizeAssistantContents());
                _messages.Add(responseMessage);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during streaming chat response");
            
            // Add any partial response
            if (allContents.Count > 0)
            {
                var consolidatedContents = new List<AIContent>();
                if (contentBuilder.Length > 0)
                {
                    consolidatedContents.Add(new TextContent(contentBuilder.ToString()));
                }
                consolidatedContents.AddRange(allContents.Where(c => c is not TextContent));

                var responseMessage = new ChatMessage(ChatRole.Assistant, consolidatedContents.NormalizeAssistantContents());
                _messages.Add(responseMessage);
            }
            
            // Add error message
            var errorMessage = new ChatMessage(ChatRole.Assistant, 
                $"Sorry, I encountered an error while processing your request: {ex.Message}");
            _messages.Add(errorMessage);
        }
        finally
        {
            // Clear the in-progress message and update suggestions
            _currentResponseMessage = null;
            _chatSuggestions?.Update(_messages);
            _chatInput?.SetProcessing(false);
            await StoreChatHistoryAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    private void CancelAnyCurrentResponse()
    {
        // If a response was canceled while streaming, include it in the conversation so it's not lost
        if (_currentResponseMessage is not null)
            _messages.Add(_currentResponseMessage);
        

        // Cancel the current operation if it exists
        if (_currentResponseCancellation != null && !_currentResponseCancellation.Token.IsCancellationRequested)
            _currentResponseCancellation?.Cancel();
        
        _currentResponseMessage = null;
    }

    private async Task ResetConversationAsync()
    {
        CancelAnyCurrentResponse();
        _messages.Clear();
        await chatHistoryStorage.DeleteAsync();
        _chatSuggestions?.Clear();
        _messages.Add(new(ChatRole.System, SystemPrompt));
        await _chatInput!.FocusAsync();
    }

    private void ToggleTerminal()
    {
        _terminalVisible = !_terminalVisible;
    }
    
    public void Dispose()
    {
        terminalUserInteraction.UserInteractionRequested -= OnTerminalUserInteractionRequested;
        _currentResponseCancellation?.Cancel();
    }
    /// <summary>
    /// kept for compatibility
    /// </summary>
    /// <param name="userMessage"></param>
    private async Task AddUserMessageSingleAsync(ChatMessage userMessage)
    {
        CancelAnyCurrentResponse();

        // Add the user message to the conversation
        _messages.Add(userMessage);
        _chatSuggestions?.Clear();
        await _chatInput!.FocusAsync();

        try
        {
            // Display a new response from the IChatClient, streaming responses
            // aren't supported because Ollama will not support both streaming and using Tools
            _currentResponseCancellation = new();
            var response = await chatClient.GetResponseAsync(_messages, _chatOptions, _currentResponseCancellation.Token);

            // Store responses in the conversation, and begin getting suggestions
            var beforeCount = _messages.Count;
            _messages.AddMessages(response);

            // Normalize any newly-added assistant/tool messages so tool results are stored as JSON when possible
            for (var i = beforeCount; i < _messages.Count; i++)
            {
                _messages[i] = _messages[i].NormalizeChatMessageContents();
            }
            _chatSuggestions?.Update(_messages);
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation gracefully - conversation is preserved
            logger.LogDebug("Chat response was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during chat response");
            
            // Add error message to chat
            var errorMessage = new ChatMessage(ChatRole.Assistant, 
                $"Sorry, I encountered an error while processing your request: {ex.Message}");
            _messages.Add(errorMessage);
            
            // Update suggestions and UI
            _chatSuggestions?.Update(_messages);
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            _chatInput?.SetProcessing(false);
        }
    }
}