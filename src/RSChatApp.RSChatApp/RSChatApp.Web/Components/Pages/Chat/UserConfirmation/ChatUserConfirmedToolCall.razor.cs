using Microsoft.AspNetCore.Components;
using RSChatApp.Web.Models.Chat.UserConfirmation;

namespace RSChatApp.Web.Components.Pages.Chat.UserConfirmation;

public partial class ChatUserConfirmedToolCall : ComponentBase
{
	private ElementReference _elementReference;
	[Parameter]
	public bool IsVisible { get; set; }

	[Parameter]
	public string Title { get; set; } = "Pending tool call";

	[Parameter]
	public string? Subtitle { get; set; }

	[Parameter]
	public string? ToolName { get; set; }

	[Parameter]
	public IDictionary<string, object>? Arguments { get; set; }
	[Parameter]
	public string? CommandOrPayload { get; set; }

	[Parameter]
	public string Language { get; set; } = "bash";

	[Parameter]
	public bool ShowCancel { get; set; } = true;

	[Parameter]
	public bool IsBusy { get; set; }

	[Parameter]
	public EventCallback<UserConfirmationToolCall> OnRun { get; set; }

	[Parameter]
	public EventCallback OnSkip { get; set; }

	[Parameter]
	public EventCallback OnCancel { get; set; }

	private string EditorId => $"uctc-{GetHashCode()}";
	private string ArgumentKey;
	private Task OnRunClicked()
	{
		if (Arguments is null)
			Arguments = new Dictionary<string, object>();

		if (string.IsNullOrWhiteSpace(ArgumentKey) == false 
		    && string.IsNullOrWhiteSpace(CommandOrPayload) == false)
		{
			Arguments[ArgumentKey] = CommandOrPayload;
		}
		
		return OnRun.InvokeAsync(new UserConfirmationToolCall(UserConfirmationResultEnum.Confirmed, Arguments!));
	}
	private Task OnSkipClicked() => OnSkip.InvokeAsync();
	private Task OnCancelClicked() => OnCancel.InvokeAsync();
	public ValueTask FocusAsync()
		=> _elementReference.FocusAsync();

	protected override async Task OnParametersSetAsync()
	{
		if (Arguments?.TryGetValue("command", out var command) ?? false)
		{
			CommandOrPayload = (string)command;
			ArgumentKey = "command";
			Language = "groovy";
		}

		if (Arguments?.TryGetValue("script", out var script) ?? false)
		{
			CommandOrPayload = (string)script;
			ArgumentKey = "script";
			Language = "javascript";
		}
		
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		await InvokeAsync(StateHasChanged);
		await FocusAsync();
	}
}