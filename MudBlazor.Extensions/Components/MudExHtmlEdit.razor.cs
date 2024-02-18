using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;
using BlazorJS;

namespace MudBlazor.Extensions.Components;


/// <summary>
/// A WYSIWYG Html editor component.
/// </summary>
public partial class MudExHtmlEdit
{
    private ElementReference _editorElement;
    private string _value;

    private string GetClass() =>
      new MudExCssBuilder("mud-ex-html-edit")
          .AddClass(Class)
      .Build();

    private string GetStyle() =>
        new MudExStyleBuilder()
            .WithHeight($"{Height}", Height is not null)
            .WithWidth($"{Width}", Width is not null)
            .WithBorder(1, BorderStyle.Solid, Color.Primary)
            .WithBackgroundColor(Color.Surface)
            .AddRaw(Style)
        .Build();

    /// <summary>
    /// Is true if the value has changed.
    /// </summary>
    public bool ValueHasChanged { get; private set; }

    /// <summary>
    /// Readonly. If true, the editor is disabled.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>
    /// If true, the editor will update the value on every change event otherwise on blur only.
    /// </summary>
    [Parameter] public bool UpdateValueOnChange { get; set; }

    /// <summary>
    /// The HTML content to be displayed and edited inside the editor.
    /// </summary>
    [Parameter]
    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
                return;
            _= SetHtml(value);
            SetValueBackingField(value);
        }
    }
    
    /// <summary>
    /// Event callback for the value changed event.
    /// </summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the height of the component.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public MudExSize<double>? Height { get; set; } = 300;

    /// <summary>
    /// Gets or sets the height of the component.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public MudExSize<double>? Width { get; set; } = "100%";

    /// <summary>
    /// Event callback for the click event.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the click event should stop propagation. Default is false.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public bool OnClickStopPropagation { get; set; }

    /// <summary>
    /// Event callback for the context menu event.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public EventCallback OnContextMenu { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the context menu event should prevent its default action. Default is false.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public bool OnContextMenuPreventDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the context menu event should stop propagation. Default is false.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public bool OnContextMenuStopPropagation { get; set; }

    /// <summary>
    /// Returns the HTML content of the editor.
    /// </summary>
    public async Task<string> GetHtml()
        => await JsRuntime.DInvokeAsync<string>((_, element) => element.innerHTML, _editorElement);

    /// <summary>
    /// Returns the HTML content of the editor.
    /// </summary>
    public async Task<string> SetHtml(string html)
        => await JsRuntime.DInvokeAsync<string>((_, element, html) => element.innerHTML = html, _editorElement, html);

    /// <summary>
    /// Returns the HTML content of the editor.
    /// </summary>
    public async Task<string> GetSelectedHtml()
    {
        return await JsRuntime.DInvokeAsync<string>((window, element) =>
        {
            var selection = window.getSelection();
            if (selection.rangeCount > 0)
            {
                var range = selection.getRangeAt(0);
                var div = window.document.createElement("div");
                div.appendChild(range.cloneContents());
                return div.innerHTML;
            }
            return null;
        }, _editorElement);
    }

    /// <summary>
    /// The on click handler
    /// </summary>    
    protected async Task OnClickHandler(MouseEventArgs ev) => await OnClick.InvokeAsync(ev);


    private void SetValueBackingField(string value)
    {
        _value = value;
        ValueChanged.InvokeAsync(value);
    }

    private async Task OnChangeHandler(ChangeEventArgs arg)
    {
        ValueHasChanged = true;
        if (UpdateValueOnChange)
            SetValueBackingField(await GetHtml());
    }

    private async Task OnBlurHandler(FocusEventArgs arg)
    {
        if (!UpdateValueOnChange)
            SetValueBackingField(await GetHtml());
    }


    private async Task OnMouseUpHandler(MouseEventArgs arg)
    {
        var selectedHtml = await GetSelectedHtml();
        if (selectedHtml is not null)
        {
            Console.WriteLine(selectedHtml);
        }
    }
}