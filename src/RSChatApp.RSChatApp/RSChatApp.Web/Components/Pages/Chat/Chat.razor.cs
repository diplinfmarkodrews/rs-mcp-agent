using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using RSChatApp.Infrastructure.UserInteraction;
using RSChatApp.Web.Components.Pages.Terminal;
using RSChatApp.Web.Mcp.Tools;
using RSChatApp.Web.Models.Chat;
using RSChatApp.Web.Services.UserConfirmation;
using RSChatApp.Web.Storage;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using TextContent = Microsoft.Extensions.AI.TextContent;
using RSChatApp.Mcp.ExtensionAI.Processing;
namespace RSChatApp.Web.Components.Pages.Chat;

public partial class Chat(
    IChatClient ChatClient,
    Kernel Kernel,
    NavigationManager Nav,
    IStorage<List<ChatMessage>> ChatHistoryStorage,
    SemanticSearchTool SemanticSearchTool, 
    AuthenticationTool AuthenticationTool,
    UserConfirmedTerminalTool UserConfirmedTerminalTool,
    ILogger<Chat> Logger,
    IWaitForUserInteraction<TerminalConfirmRequest, UserConfirmationResult> TerminalUserInteraction
    ) : ComponentBase, IDisposable
{
    //TODO: refactor into SystemPromptService
    private const string SystemPrompt = @"
        You are an assistant for the ReportServer and a omnipotent groovy script developer. 

        Use the search tool to find relevant information in documentation, 
        scripts and terminal commands. 
        The documentation consists of the following files:
        - ReportServer-5_0-ConfigurationGuide-En.pdf
        - ReportServer-5_0-UserGuide-De.pdf
        - ReportServer-5_0-AdminGuide-En.pdf
        - ReportServer-5_0-ScriptGuide-En.pdf
                 
        You have a browsertool to have full control of the Reportserver front end. 
        The official reportserver documentation is available at 'https://reportserver.net/de/dokumentation'
        Its the content of your knowledgebase, found in Searchtool.
        
        Here are some rs terminal commands
        Dateisystem & Navigation
        cd - Verzeichnis wechseln (z.B. cd fileserver/bin)
        mkdir - Verzeichnis erstellen (z.B. mkdir tmp)
        ls - Verzeichnisinhalt anzeigen
        pwd - aktuelles Verzeichnis anzeigen
        rm - Dateien/Verzeichnisse löschen
        mv - Dateien/Verzeichnisse verschieben
        cp - Dateien/Verzeichnisse kopieren
        Dateibearbeitung
        createTextFile - Neue Textdatei erstellen (z.B. createTextFile helloworld.groovy)
        editTextFile - Textdatei bearbeiten (z.B. editTextFile helloworld.groovy)
        cat - Dateiinhalt im Terminal anzeigen (z.B. cat file.txt)
        echo - Text ausgeben und in Dateien schreiben (z.B. echo foobar > file.txt oder echo more >> file.txt)
        Script-Ausführung & Monitoring
        exec - Script ausführen (z.B. exec helloworld.groovy)
        Flags: -s (silent/Hintergrund), -w (neues Fenster), -n (kein eigener Thread)
        ps - Liste der laufenden Scripts anzeigen
        kill - Script-Ausführung beenden
        kill ID - Script unterbrechen
        kill -f ID - Script hart beenden (force)
        Konfiguration
        config reload - Konfiguration neu laden (nach Änderungen an Config-Dateien)
        diffconfigfiles - Hilfe bei fehlenden Config-Dateien nach Upgrades
        Objekt-Informationen
        desc - Objekt-Beschreibung anzeigen (z.B. desc User id:User:3)
        Flag: -w (in neuem Fenster anzeigen)
        Scheduler
        scheduleScript - Scripts zeitgesteuert ausführen
        scheduleScript list - geplante Scripts auflisten
        scheduleScript execute - Script planen (z.B. scheduleScript execute myScript.groovy """" every day at 15:23)
        scheduler - Scheduler-Verwaltung
        scheduler listFireTimes - nächste Ausführungszeiten anzeigen
        scheduler remove - geplante Aufgabe entfernen
        scheduler daemon start/stop - Scheduler aktivieren/deaktivieren
        LDAP-Verwaltung
        ldaptest - LDAP-Konfiguration testen
        ldaptest users - Benutzer testen
        ldaptest groups - Gruppen testen
        ldaptest organizationalUnits - OUs testen
        ldaptest filter - Filter testen
        Flag: -s (Schema anzeigen)
        ldapfilter - LDAP-Filter analysieren
        ldapschema - LDAP-Schema erkunden (z.B. ldapschema objectClassInfo organizationalPerson)
        ldapguid - LDAP GUID-Informationen
        ldapinfo - LDAP-Informationen
        ldapimport - LDAP-Import durchführen
        ssltest - SSL-Konfiguration für LDAP testen
        Logging
        listlogfiles - Log-Dateien auflisten
        Flag: -e (per Email versenden)
        Flag: -f (Filter)
        Pakete & Installation
        pkg install - Pakete installieren (z.B. pkg install -d demobuilder -VERSION_NR)        
        Besondere Hinweise:
        Tab-Vervollständigung: Das Terminal unterstützt Autocomplete mit der TAB-Taste

        Pipes und Weiterleitungen:

        > - Ausgabe in Datei umleiten (überschreiben)
        >> - Ausgabe an Datei anhängen
        Rückgabewerte: Die letzte Zeile eines Scripts wird als Rückgabewert interpretiert und im Terminal angezeigt

        Terminal-Output: Das tout-Objekt kann für Ausgaben während der Script-Ausführung verwendet werden:

        tout.println('Hello World')
        ";

    private readonly ChatOptions chatOptions = new();
    private readonly List<ChatMessage> messages = new();
    private CancellationTokenSource? currentResponseCancellation;
    private ChatMessage? currentResponseMessage;
    private ChatInput? chatInput;
    private ChatSuggestions? chatSuggestions;
    private bool _isOllama;
    private bool _terminalVisible = false;
    private TerminalManager? _terminalManager;
    private int _terminalHeight = 200;

    private TerminalConfirmRequest? _pendingTerminalRequest;
    private TaskCompletionSource<UserConfirmationResult>? _pendingTerminalTcs;
    
    // [Experimental("SKEXP0001")]
    protected override async Task OnInitializedAsync()
    {
        TerminalUserInteraction.UserInteractionRequested += OnTerminalUserInteractionRequested;

        // Since new ollama version supports stream & toolcalls, will refactor to stream api only
        // TODO: Test first with ollama version that supports both streaming and toolcalls
        _isOllama = false;
        
        // Debug logging to see what kernel plugins are available
        var kernelPlugins = Kernel.Plugins.ToList();
        Logger.LogDebug("Total kernel plugins available: {kernelPluginsCount}: \n{kernelPluginsNames} ", kernelPlugins.Count, 
            string.Join(", ", kernelPlugins.Select(p=> p.Name)));

        // Create a list of all tools (local search + kernel MCP tools)
        var allTools = new List<AITool>
        {
            AIFunctionFactory.Create(SemanticSearchTool.SearchAsync,  "Search", "Search for information using a phrase or keyword"),
            // AIFunctionFactory.Create(AuthenticationTool.IsAuthenticatedAsync, "IsAuthenticated", "Checks whether the user is authenticated against the ReportServer and can execute ReportServerMcp tools or not"),
            // AIFunctionFactory.Create(AuthenticationTool.LoginUserRequestedAsync, "RequestLogin", "Requests the user to login when they need to access ReportServer MCP tools but are not authenticated"),
            // AIFunctionFactory.Create(UserConfirmedTerminalTool.ExecuteCommandAsync, "MultiTerminalTool", "Executes commands in the terminal with user confirmation. Valid terminal types are ")
        };
        
        // Add MCP tools
        foreach (var plugin in kernelPlugins)
        {
            foreach (var aiFunction in plugin)
            {
                allTools.Add(
                    aiFunction.AsKernelFunction()
                        .WithKernel(Kernel));
            }
        }
        Logger.LogInformation("Total tools registered for chat: {allToolsCount}: \n{kernelPluginsNames} ", allTools.Count, 
            string.Join(", ", kernelPlugins.Select(p=> p.Name)));
        
        chatOptions.Tools = allTools;
        
        // var pluginD =
        //     Kernel.Plugins.FirstOrDefault(p => p.Name.Contains("rsmcpserver", StringComparison.InvariantCultureIgnoreCase));
        // if (pluginD != null)
        // {
        //     var execFunc = pluginD.AsAIFunctions()
        //         .FirstOrDefault(f => f.Name.Contains("execute", StringComparison.InvariantCultureIgnoreCase));

        //     var argDict = new Dictionary<string, object?>
        //     {
        //         { "sessionId", null },
        //         { "command", "ls" }
        //     };
        //     var aiArgs = new AIFunctionArguments(argDict);
        //     var funcResult = await execFunc.InvokeAsync(aiArgs);
        //     Logger.LogInformation(funcResult.ToString());
        // }
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

    private async Task ResolveTerminalConfirmationAsync(UserConfirmationResultEnum result)
    {
        if (_pendingTerminalTcs is null)
        {
            return;
        }

        _pendingTerminalTcs.TrySetResult(new UserConfirmationResult(result));
        _pendingTerminalTcs = null;
        _pendingTerminalRequest = null;
        await InvokeAsync(StateHasChanged);
    }
    
    public async Task StoreChatHistoryAsync()
    {
        try
        {
            // Debug: Log what we're about to save
            Logger.LogDebug("Saving {messageCount} messages to chat history", messages.Count);
            foreach (var msg in messages)
            {
                if (msg.Role == ChatRole.Assistant && msg.Contents != null && msg.Contents.Count > 1)
                {
                    Logger.LogDebug("Saving Assistant message with {contentCount} contents:", msg.Contents.Count);
                    foreach (var content in msg.Contents)
                    {
                        Logger.LogDebug("  - Content type: {contentType}", content.GetType().Name);
                    }
                }
            }
            
            await ChatHistoryStorage.SaveAsync(messages);
            Logger.LogInformation("Chat history saved with {messageCount} messages", messages.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving chat history");
        }
    }
    private async Task InitChatHistoryAsync(int retryCount = 0)
    {
        // await Task.Delay(500);
        try
        {
            var chatHistory = await ChatHistoryStorage.GetAsync();
            messages.Clear();
            
            if (chatHistory.Success && chatHistory.Value!.Count > 0)
            {
                Logger.LogInformation("Loaded {chatHistoryCount} messages from chat history", chatHistory.Value!.Count);
                messages.AddRange(chatHistory.Value);
                chatSuggestions?.Update(messages);
                
                // Trigger UI update
                await InvokeAsync(StateHasChanged);
                
                // Focus the input after loading
                if (chatInput is not null)
                {
                    await chatInput.FocusAsync();
                }
            }
            else
            {
                Logger.LogInformation("No chat history found, starting new conversation");
                messages.Add(new(ChatRole.System, SystemPrompt));
            }
        }
        catch (TaskCanceledException ex)
        {                        
            if (retryCount < 4)
            {
                retryCount++;
                Logger.LogWarning(ex, "Chat history loading was cancelled (JS interop not ready), attempting to reload (attempt {retryCount})", retryCount);           
                await Task.Delay(500);
                await InitChatHistoryAsync(retryCount);
                return;
            }
            Logger.LogWarning(ex, "Chat history loading was cancelled (JS interop not ready), starting fresh conversation");           
            messages.Clear();
            messages.Add(new(ChatRole.System, SystemPrompt));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load chat history, starting fresh conversation");
            messages.Clear();
            messages.Add(new(ChatRole.System, SystemPrompt));
        }
    }
    
    private Task AddUserMessageAsync(ChatMessage userMessage)
    {
        if (_isOllama)
            return AddUserMessageSingleAsync(userMessage);
        
        return AddUserMessageStreamAsync(userMessage);

    }
    private async Task AddUserMessageSingleAsync(ChatMessage userMessage)
    {
        CancelAnyCurrentResponse();

        // Add the user message to the conversation
        messages.Add(userMessage);
        chatSuggestions?.Clear();
        await chatInput!.FocusAsync();

        try
        {
            // Display a new response from the IChatClient, streaming responses
            // aren't supported because Ollama will not support both streaming and using Tools
            currentResponseCancellation = new();
            var response = await ChatClient.GetResponseAsync(messages, chatOptions, currentResponseCancellation.Token);

            // Store responses in the conversation, and begin getting suggestions
            var beforeCount = messages.Count;
            messages.AddMessages(response);

            // Normalize any newly-added assistant/tool messages so tool results are stored as JSON when possible
            for (var i = beforeCount; i < messages.Count; i++)
            {
                messages[i] = messages[i].NormalizeChatMessageContents();
            }
            chatSuggestions?.Update(messages);
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation gracefully - conversation is preserved
            Logger.LogDebug("Chat response was cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during chat response");
            
            // Add error message to chat
            var errorMessage = new ChatMessage(ChatRole.Assistant, 
                $"Sorry, I encountered an error while processing your request: {ex.Message}");
            messages.Add(errorMessage);
            
            // Update suggestions and UI
            chatSuggestions?.Update(messages);
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            chatInput?.SetProcessing(false);
        }
    }
    private async Task AddUserMessageStreamAsync(ChatMessage userMessage)
    {
        CancelAnyCurrentResponse();

        // Add the user message to the conversation
        messages.Add(userMessage);
        chatSuggestions?.Clear();
        await chatInput!.FocusAsync();

        // Display a new response from the IChatClient with streaming
        currentResponseCancellation = new();
        
        // Track text for display and all contents
        var contentBuilder = new StringBuilder();
        var allContents = new List<AIContent>();

        try
        {
            // Normalize messages for API (split FunctionCallContent and FunctionResultContent into separate messages)
            var normalizedMessages = messages.NormalizeMessagesForApi();
            
            // Use streaming API to get progressive responses
            await foreach (var update in ChatClient.GetStreamingResponseAsync(normalizedMessages, chatOptions, currentResponseCancellation.Token))
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

                currentResponseMessage = new ChatMessage(ChatRole.Assistant, streamingContents.NormalizeAssistantContents());
                
                // Trigger UI update to show streaming content
                await InvokeAsync(StateHasChanged);
                
                // Check for cancellation
                currentResponseCancellation.Token.ThrowIfCancellationRequested();
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
                messages.Add(responseMessage);
                
                Logger.LogInformation("Added response message with {contentCount} contents", consolidatedContents.Count);
                foreach (var content in consolidatedContents)
                {
                    Logger.LogInformation("  Content type: {contentType}", content.GetType().Name);
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
                messages.Add(responseMessage);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during streaming chat response");
            
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
                messages.Add(responseMessage);
            }
            
            // Add error message
            var errorMessage = new ChatMessage(ChatRole.Assistant, 
                $"Sorry, I encountered an error while processing your request: {ex.Message}");
            messages.Add(errorMessage);
        }
        finally
        {
            // Clear the in-progress message and update suggestions
            currentResponseMessage = null;
            chatSuggestions?.Update(messages);
            chatInput?.SetProcessing(false);
            await StoreChatHistoryAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    

    private void CancelAnyCurrentResponse()
    {
        // If a response was cancelled while streaming, include it in the conversation so it's not lost
        if (currentResponseMessage is not null)
        {
            messages.Add(currentResponseMessage);
        }

        // Cancel the current operation if it exists
        if (currentResponseCancellation != null && !currentResponseCancellation.Token.IsCancellationRequested)
        {
            currentResponseCancellation?.Cancel();
        }
        currentResponseMessage = null;
    }

    private async Task ResetConversationAsync()
    {
        CancelAnyCurrentResponse();
        messages.Clear();
        await ChatHistoryStorage.DeleteAsync();
        chatSuggestions?.Clear();
        messages.Add(new(ChatRole.System, SystemPrompt));
        await chatInput!.FocusAsync();
    }

    private void ToggleTerminal()
    {
        _terminalVisible = !_terminalVisible;
    }
    
    public void Dispose()
    {
        TerminalUserInteraction.UserInteractionRequested -= OnTerminalUserInteractionRequested;
        currentResponseCancellation?.Cancel();
    }
}