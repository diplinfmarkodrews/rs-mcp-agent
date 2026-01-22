using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;

namespace RSChatApp.Web.Components.Pages.Editor;

public partial class CodeSnippet : ComponentBase
{
    [Parameter]
    public string Id { get; set; } = $"code-snippet-{Guid.NewGuid():N}";

    [Parameter]
    public string CodeValue { get; set; } = string.Empty;

    [Parameter]
    public string Language { get; set; } = "groovy";

    [Parameter]
    public bool IsReadOnly { get; set; } = true;

    protected override void OnParametersSet()
    {
        _codeValue = CodeValue ?? string.Empty;
        _language = string.IsNullOrWhiteSpace(Language) ? "groovy" : Language;
        _isReadOnly = IsReadOnly;
        StateHasChanged();
    }

    public void SetCodeValue(string code, string? language = null)
    {
        if (_isReadOnly)
            return;

        if (language != null)
            _language = language;
        
        _codeValue = code;
        StateHasChanged();
    }
    private string _codeValue = string.Empty;
    private string _language = "groovy";
    private bool _isReadOnly = true;
    private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Language = _language,
            Value = _codeValue,
            ReadOnly = _isReadOnly
        };
    }
    
}