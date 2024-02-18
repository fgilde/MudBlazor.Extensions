using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Just a simple MudCheckBox with HelperText
/// </summary>
public partial class MudExCheckBox<T>
{

    /// <summary>
    /// The HelperText will be displayed below the text field.
    /// </summary>
    [Parameter][SafeCategory(CategoryTypes.FormComponent.Behavior)] 
    public string HelperText { get; set; }

    /// <summary>
    /// The HelperText will be displayed below the text field.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public HelperTextAlignment HelperTextAlignment { get; set; }

    /// <summary>
    /// Gets the inherited render fragment.
    /// </summary>
    private RenderFragment Inherited() => builder =>
    {
        base.BuildRenderTree(builder);
    };

    private string ClassnameStr => MudExCssBuilder.From("mud-input-helper-text")
        .AddClass("ml-12", HelperTextAlignment == HelperTextAlignment.OnLabel)
        .AddClass("ml-4", HelperTextAlignment == HelperTextAlignment.OnComponent)
        .Build();
}