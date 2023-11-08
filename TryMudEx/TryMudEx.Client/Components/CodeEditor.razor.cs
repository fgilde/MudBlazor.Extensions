using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Try.Core;

namespace TryMudEx.Client.Components;

public partial class CodeEditor : IDisposable
{
    private const string EditorId = "user-code-editor";

    private bool hasCodeChanged;
    private bool hasReadOnlyChanged;
    private bool hasThemeChanged;

    [Inject] public IJSInProcessRuntime JsRuntime { get; set; }

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
        JsRuntime.InvokeVoid(Models.Try.Editor.Dispose);
    }

    internal void Focus()
    {
        JsRuntime.InvokeVoid(Models.Try.Editor.Focus);
    }

    internal string GetCode()
    {
        return JsRuntime.Invoke<string>(Models.Try.Editor.GetValue);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        var newCode = Code;
        if (newCode != null) Code = Regex.Replace(newCode, @"@code\s*\r?\n\s*{", "@code {");

        if (firstRender)
        {
            JsRuntime.InvokeVoid(Models.Try.Editor.Create, EditorId,
                Code ?? CoreConstants.MainComponentDefaultFileContent, GetLanguage(), ReadOnly, Theme);
        }
        else
        {
	        if (hasCodeChanged)
	        {
		        var language = GetLanguage();
		        JsRuntime.InvokeVoid(Models.Try.Editor.SetValue, Code);
		        JsRuntime.InvokeVoid(Models.Try.Editor.SetLangugage, language);
	        }
	        if (hasReadOnlyChanged)
	        {
		        JsRuntime.InvokeVoid(Models.Try.Editor.SetReadOnly, ReadOnly);
	        }
	        if (hasThemeChanged)
	        {
		        JsRuntime.InvokeVoid(Models.Try.Editor.SetTheme, Theme);
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
            await JsRuntime.InvokeVoidAsync(Models.Try.Editor.SetSelection, line.Value);
    }
}