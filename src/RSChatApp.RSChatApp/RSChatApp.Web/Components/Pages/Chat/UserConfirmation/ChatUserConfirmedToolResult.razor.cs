using Microsoft.AspNetCore.Components;
using RSChatApp.Web.Models.Chat.ToolCalls;
using RSChatApp.Web.Models.Chat.UserConfirmation;

namespace RSChatApp.Web.Components.Pages.Chat.UserConfirmation;

public partial class ChatUserConfirmedToolResult : ComponentBase
{
    private ElementReference _elementReference;
    [Parameter]
    public bool IsVisible { get; set; }

    [Parameter]
    public string Title { get; set; } = "Tool call result to review";

    [Parameter]
    public string? Subtitle { get; set; }

    [Parameter]
    public string? ToolName { get; set; }

    [Parameter]
    public ToolResult? ToolResult { get; set; }
    [Parameter]
    public ToolInvocation? ToolInvocation { get; set; }
    
    [Parameter]
    public EventCallback<UserConfirmationToolResult> OnRun { get; set; }

    [Parameter]
    public EventCallback OnSkip { get; set; }
    private bool _isRedacted = false;
    
    private string EditorId => $"uctc-{GetHashCode()}";

    private Task OnRunClicked() => OnRun.InvokeAsync(new UserConfirmationToolResult
    {   
        UserConfirmationResult = _isRedacted 
            ? UserConfirmationResultEnum.Redacted 
            : UserConfirmationResultEnum.Confirmed,
        ToolResult = ToolResult,
        
    });
    private Task OnSkipClicked() => OnSkip.InvokeAsync();

    private Task OnRedactClicked()
    {
        _isRedacted = true;
        return Task.CompletedTask;
    }

    public ValueTask FocusAsync()
    {
        return ValueTask.CompletedTask;
    }

    private Task EditInEditor()
    {
        return Task.CompletedTask;
    }
    protected override async Task OnParametersSetAsync()
    {
        ToolName = ToolInvocation?.DisplayName;
        IsVisible = true;
        await InvokeAsync(StateHasChanged);
        await FocusAsync();
    }
}