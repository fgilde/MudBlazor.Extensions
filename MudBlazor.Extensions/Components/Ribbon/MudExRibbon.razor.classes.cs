using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Presentation of a <see cref="MudExRibbon"/>. All modes render the same tabs, groups and items.
/// </summary>
public enum MudExRibbonMode
{
    /// <summary>
    /// Office ribbon with tabs, titled groups and large or small commands.
    /// </summary>
    Classic,

    /// <summary>
    /// Single line ribbon with small commands.
    /// </summary>
    Simplified,

    /// <summary>
    /// Classic menu bar with one dropdown per tab.
    /// </summary>
    Menu,

    /// <summary>
    /// Flat icon toolbar of the active tab.
    /// </summary>
    Toolbar
}

/// <summary>
/// Declared size of a ribbon item.
/// </summary>
public enum MudExRibbonItemSize
{
    /// <summary>
    /// Large icon above the text, spanning all rows of a group.
    /// </summary>
    Large,

    /// <summary>
    /// Icon followed by the text.
    /// </summary>
    Medium,

    /// <summary>
    /// Icon only.
    /// </summary>
    Small
}

/// <summary>
/// Layout an item is asked to render itself in.
/// </summary>
public enum MudExRibbonItemLayout
{
    /// <summary>
    /// Large icon above the text.
    /// </summary>
    Large,

    /// <summary>
    /// Icon followed by the text.
    /// </summary>
    Medium,

    /// <summary>
    /// Icon only.
    /// </summary>
    Small,

    /// <summary>
    /// Entry of a dropdown menu.
    /// </summary>
    MenuEntry,

    /// <summary>
    /// Icon of a flat toolbar.
    /// </summary>
    ToolbarIcon
}

internal interface IMudExRibbonItemHost
{
    void Register(MudExRibbonItemBase item);
    void Unregister(MudExRibbonItemBase item);
}

internal static class MudExRibbonColors
{
    internal static string BackgroundFor(MudExColor color)
        => color.IsColor ? color.AsColor.CssVarDeclaration() : color.ToCssStringValue();

    internal static string ForegroundFor(MudExColor color)
    {
        if (color.IsColor)
            return $"var({color.AsColor.CssVarName()}-text)";
        if (color.ToCssStringValue().StartsWith("var(", StringComparison.OrdinalIgnoreCase))
            return "var(--mud-palette-text-primary)";
        return RelativeLuminance(color.ToMudColor()) > 0.5 ? "rgba(0,0,0,.87)" : "#fff";
    }

    private static double RelativeLuminance(MudColor color)
        => 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);

    private static double Linear(byte channel)
    {
        var c = channel / 255d;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
