using Microsoft.AspNetCore.Components;

namespace RSChatApp.Web.Components.Pages.Chat;

public partial class ChatUserConfirmedToolCall : ComponentBase
{
	[Parameter]
	public bool IsVisible { get; set; }

	[Parameter]
	public string Title { get; set; } = "Pending tool call";

	[Parameter]
	public string? Subtitle { get; set; }

	[Parameter]
	public string? ToolName { get; set; }

	[Parameter]
	public string? CommandOrPayload { get; set; }

	[Parameter]
	public string Language { get; set; } = "bash";

	[Parameter]
	public bool ShowCancel { get; set; } = true;

	[Parameter]
	public bool IsBusy { get; set; }

	[Parameter]
	public EventCallback<string> OnRun { get; set; }

	[Parameter]
	public EventCallback OnSkip { get; set; }

	[Parameter]
	public EventCallback OnCancel { get; set; }

	private string EditorId => $"uctc-{GetHashCode()}";

	private Task OnRunClicked() => OnRun.InvokeAsync(CommandOrPayload);
	private Task OnSkipClicked() => OnSkip.InvokeAsync();
	private Task OnCancelClicked() => OnCancel.InvokeAsync();
}