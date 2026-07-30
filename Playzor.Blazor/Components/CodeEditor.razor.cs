using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Try.Core;

namespace Playzor.Blazor.Components;

public partial class CodeEditor : IDisposable
{
    private bool hasCodeChanged;
    private bool hasReadOnlyChanged;
    private bool hasThemeChanged;

    [Inject] public IJSInProcessRuntime JsRuntime { get; set; }

    /// <summary>Unique DOM/editor instance id — one monaco instance per CodeEditor.</summary>
    [Parameter] public string Id { get; set; } = "user-code-editor";

    [Parameter] public string Code { get; set; }

    [Parameter] public CodeFileType CodeFileType { get; set; }

    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public string Theme { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<string>(nameof(Code), out var parameterValue))
            hasCodeChanged = Code != parameterValue;

        if (parameters.TryGetValue<bool>(nameof(ReadOnly), out var readOnly)) hasReadOnlyChanged = ReadOnly != readOnly;
        if (parameters.TryGetValue<string>(nameof(Theme), out var theme)) hasThemeChanged = Theme != theme;

        return base.SetParametersAsync(parameters);
    }

    public void Dispose()
    {
        JsRuntime.InvokeVoid(PlayzorJs.Editor.Dispose, Id);
    }

    internal void Focus()
    {
        JsRuntime.InvokeVoid(PlayzorJs.Editor.Focus, Id);
    }

    internal string GetCode()
    {
        return JsRuntime.Invoke<string>(PlayzorJs.Editor.GetValue, Id);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        var newCode = Code;
        if (newCode != null) Code = Regex.Replace(newCode, @"@code\s*\r?\n\s*{", "@code {");

        if (firstRender)
        {
            JsRuntime.InvokeVoid(PlayzorJs.Editor.Create, Id,
                Code ?? CoreConstants.MainComponentDefaultFileContent, GetLanguage(), ReadOnly, Theme);
        }
        else
        {
	        if (hasCodeChanged)
	        {
		        var language = GetLanguage();
		        JsRuntime.InvokeVoid(PlayzorJs.Editor.SetValue, Id, Code);
		        JsRuntime.InvokeVoid(PlayzorJs.Editor.SetLanguage, Id, language);
	        }
	        if (hasReadOnlyChanged)
	        {
		        JsRuntime.InvokeVoid(PlayzorJs.Editor.SetReadOnly, Id, ReadOnly);
	        }
	        if (hasThemeChanged)
	        {
		        JsRuntime.InvokeVoid(PlayzorJs.Editor.SetTheme, Theme); // monaco theme is global
	        }
		}

        base.OnAfterRender(firstRender);
    }

    private string GetLanguage()
    {
        return CodeFileType == CodeFileType.CSharp ? "csharp" : "razor";
    }

    public async Task SelectLineAsync(int? line)
    {
        if(line.HasValue)
            await JsRuntime.InvokeVoidAsync(PlayzorJs.Editor.SetSelection, Id, line.Value);
    }
}
