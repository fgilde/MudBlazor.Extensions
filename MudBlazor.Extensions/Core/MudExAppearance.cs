using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Core;

public class MudExAppearance : IMudExClassAppearance, IMudExStyleAppearance, ICloneable
{
    private MudExAppearanceService _appearanceService = new();
    public string Class { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public bool KeepExisting { get; set; } = true;

    public static MudExAppearance Empty => new();
    public static MudExAppearance FromCss(string cls, params string[] other) => FromCss(MudExCssBuilder.From(cls, other));
    public static MudExAppearance FromCss(MudExCss.Classes cls, params MudExCss.Classes[] other) => FromCss(MudExCssBuilder.From(cls, other));
    public static MudExAppearance FromCss(MudExCssBuilder builder) => Empty.WithCss(builder);
    public static MudExAppearance FromStyle(object styleObj) => FromStyle(MudExStyleBuilder.FromObject(styleObj));
    public static MudExAppearance FromStyle(string style) => FromStyle(MudExStyleBuilder.FromStyle(style));
    public static MudExAppearance FromStyle(MudExStyleBuilder styleBuilder) => Empty.WithStyle(styleBuilder);

    public MudExAppearance WithStyle(IMudExStyleAppearance style)
    {
        Style += $" {style.Style.EnsureEndsWith(";")}";
        return this;
    }

    public MudExAppearance WithStyle(object styleObj, CssUnit cssUnit = CssUnit.Pixels) => WithStyle(MudExStyleBuilder.FromObject(styleObj, "", cssUnit));
    public MudExAppearance WithStyle(object styleObj, string existingStyleToKeep, CssUnit cssUnit = CssUnit.Pixels) => WithStyle(MudExStyleBuilder.FromObject(styleObj, existingStyleToKeep, cssUnit));
    public MudExAppearance WithStyle(string styleString) => WithStyle(MudExStyleBuilder.FromStyle(styleString));

    public MudExAppearance WithStyle(Action<MudExStyleBuilder> styleAction)
    {
        var builder = new MudExStyleBuilder();
        styleAction(builder);
        return WithStyle(builder);
    }

    public async Task<MudExAppearance> WithStyle(Func<MudExStyleBuilder, Task> styleAction)
    {
        var builder = new MudExStyleBuilder();
        await styleAction(builder);
        return WithStyle(builder);
    }
    
    

    public MudExAppearance WithCss(string cls, params string[] other) => WithCss(MudExCssBuilder.From(cls, other));
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
