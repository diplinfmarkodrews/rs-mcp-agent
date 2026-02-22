using Microsoft.AspNetCore.Components;
using RSChatApp.Web.Models.Chat.ToolCalls;

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
    public EventCallback<ToolResult> OnRun { get; set; }

    [Parameter]
    public EventCallback OnSkip { get; set; }

    [Parameter]
    public EventCallback OnRedact { get; set; }

    private string EditorId => $"uctc-{GetHashCode()}";

    private Task OnRunClicked() => OnRun.InvokeAsync(ToolResult);
    private Task OnSkipClicked() => OnSkip.InvokeAsync();
    private Task OnRedactClicked() => OnRedact.InvokeAsync();
    public ValueTask FocusAsync()
        => _elementReference.FocusAsync();

    protected override async Task OnParametersSetAsync()
    {
        ToolName= ToolInvocation.DisplayName;
        // Subtitle =ToolResult.
        await FocusAsync();
    }
}