using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// The `MudExAppearance` class is a powerful tool that helps to manage CSS and styles of MudBlazor components dynamically. 
/// </summary>
[HasDocumentation("MudExAppearance.md")]
public class MudExAppearance : IMudExClassAppearance, IMudExStyleAppearance, ICloneable
{
    private MudExAppearanceService _appearanceService = new();

    /// <summary>
    /// Class to apply
    /// </summary>
    public string Class { get; set; } = string.Empty;

    /// <summary>
    /// CSS Style string to apply
    /// </summary>
    public string Style { get; set; } = string.Empty;

    /// <summary>
    /// Set to false to overwrite all existing classes and styles
    /// </summary>
    public bool KeepExisting { get; set; } = true;

    /// <summary>
    /// Factory method for an empty instance
    /// </summary>
    public static MudExAppearance Empty() => new();

    public static MudExAppearance FromCss(string cls, params string[] other) => FromCss(MudExCssBuilder.From(cls, other));
    public static MudExAppearance FromCss(MudExCss.Classes cls, params MudExCss.Classes[] other) => FromCss(MudExCssBuilder.From(cls, other));
    public static MudExAppearance FromCss(MudExCssBuilder builder) => Empty().WithCss(builder);
    public static MudExAppearance FromStyle(object styleObj) => FromStyle(MudExStyleBuilder.FromObject(styleObj));
    public static MudExAppearance FromStyle(string style) => FromStyle(MudExStyleBuilder.FromStyle(style));
    public static MudExAppearance FromStyle(MudExStyleBuilder styleBuilder) => Empty().WithStyle(styleBuilder);
    public static MudExAppearance FromStyle(Action<MudExStyleBuilder> styleBuilderAction) => Empty().WithStyle(styleBuilderAction);

    /// <summary>
    /// Adds style to this appearance from given appearance
    /// </summary>
    public MudExAppearance WithStyle(IMudExStyleAppearance style)
    {
        Style += $" {style.Style.EnsureEndsWith(";")}";
        return this;
    }

    /// <summary>
    /// Adds style to this appearance from given styleObj
    /// </summary>
    public MudExAppearance WithStyle(object styleObj, CssUnit cssUnit = CssUnit.Pixels) => WithStyle(MudExStyleBuilder.FromObject(styleObj, "", cssUnit));

    /// <summary>
    /// Adds style to this appearance from given styleObj
    /// </summary>
    public MudExAppearance WithStyle(object styleObj, string existingStyleToKeep, CssUnit cssUnit = CssUnit.Pixels) => WithStyle(MudExStyleBuilder.FromObject(styleObj, existingStyleToKeep, cssUnit));

    /// <summary>
    /// Adds style to this appearance from given styleString
    /// </summary>    
    public MudExAppearance WithStyle(string styleString) => WithStyle(MudExStyleBuilder.FromStyle(styleString));

    /// <summary>
    /// Adds style to this appearance with passing a fluent Action with a MudExStyleBuilder
    /// </summary>    
    public MudExAppearance WithStyle(Action<MudExStyleBuilder> styleAction)
    {
        var builder = new MudExStyleBuilder();
        styleAction(builder);
        return WithStyle(builder);
    }

    /// <summary>
    /// Adds style to this appearance with passing a async Func with a MudExStyleBuilder
    /// </summary>    
    public async Task<MudExAppearance> WithStyle(Func<MudExStyleBuilder, Task> styleAction)
    {
        var builder = new MudExStyleBuilder();
        await styleAction(builder);
        return WithStyle(builder);
    }


    /// <summary>
    /// Adds class to this appearance
    /// </summary>    
    public MudExAppearance WithCss(string cls, params string[] other) => WithCss(MudExCssBuilder.From(cls, other));
    public MudExAppearance WithCss(string cls, bool when) => WithCss(MudExCssBuilder.Default.AddClass(cls, when));
    public MudExAppearance WithCss(MudExCss.Classes cls, bool when) => WithCss(MudExCssBuilder.Default.AddClass(cls, when));
    
    public MudExAppearance WithCss(MudExCss.Classes cls, params MudExCss.Classes[] other) => WithCss(MudExCssBuilder.From(cls, other));

    public MudExAppearance WithCss(IMudExClassAppearance css)
    {
        Class += $" {css.Class}";
        return this;
    }

    public MudExAppearance WithCss(Action<MudExCssBuilder> cssAction)
    {
        var builder = new MudExCssBuilder();
        cssAction(builder);
        return WithCss(builder);
    }

    public async Task<MudExAppearance> WithCss(Func<MudExCssBuilder, Task> cssAction)
    {
        var builder = new MudExCssBuilder();
        await cssAction(builder);
        return WithCss(builder);
    }


    public Task<MudExAppearance> ApplyAsClassOnlyToAsync<TComponent>(TComponent component, Action<TComponent, string> updateFunc)
        => _appearanceService.ApplyAsClassOnlyToAsync(this, component, updateFunc);

    public Task<MudExAppearance> ApplyToAsync(MudComponentBase component) => _appearanceService.ApplyToAsync(this, component, KeepExisting);
    public Task<MudExAppearance> ApplyToAsync(string elementSelector) => _appearanceService.ApplyToAsync(this, elementSelector, KeepExisting);
    public Task<MudExAppearance> ApplyToAsync(ElementReference elementRef) => _appearanceService.ApplyToAsync(this, elementRef, KeepExisting);
    public Task<MudExAppearance> ApplyToAsync(IDialogReference dialogReference) => _appearanceService.ApplyToAsync(this, dialogReference, KeepExisting);

    public object Clone() => MemberwiseClone();
}
