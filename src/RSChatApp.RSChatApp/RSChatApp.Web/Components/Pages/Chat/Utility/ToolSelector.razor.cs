using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using RSChatApp.Web.Services.Chat.Tools;
using RSChatApp.Web.Storage.Utility;

namespace RSChatApp.Web.Components.Pages.Chat.Utility;

public partial class ToolSelector : ComponentBase
{
    [Inject] private ToolCollectionService ToolCollectionService { get; set; } = default!;
    [Inject] private ToolSelectionStorage ToolSelectionStorage { get; set; } = default!;

    [Parameter] public EventCallback OnSelectionChanged { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await ToolSelectionStorage.InitializeAllAsync(ToolCollectionService.AllTools.Select(t => t.Name));
    }

    private async Task OnToggleAsync(AITool tool, bool enabled)
    {
        await ToolSelectionStorage.SetEnabledAsync(tool.Name, enabled);
        await OnSelectionChanged.InvokeAsync();
    }
}