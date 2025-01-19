using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component to select a theme from a list of themes.
/// </summary>
public partial class MudExThemeSelect<TTheme>
{
    private ThemePreset<TTheme> _selected;
    private MudSelect<ThemePreset<TTheme>> _mudSelector;

    /// <summary>
    /// Typography for Name
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public Typo TypoThemeName { get; set; } = Typo.h6;

    /// <summary>
    /// Typography for description
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public Typo TypoThemeDescription { get; set; } = Typo.subtitle1;

    /// <summary>
    /// Size of preview image
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExDimension PreviewImageSize { get; set; } = new(140, 100);

    [Parameter] public bool PreviewFont { get; set; }

    /// <summary>
    /// Variant if SelectionMode is ThemeSelectionMode.Select
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public Variant SelectVariant { get; set; } = Variant.Outlined;

    /// <summary>
    /// Label
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string Label { get; set; } = "Themes";

    /// <summary>
    /// Style for theme name container
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public string NameContainerStyle { get; set; }

    /// <summary>
    /// Style for one theme item
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public string ItemStyle { get; set; }

    /// <summary>
    /// Class for one theme item
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public string ItemClass { get; set; }

    /// <summary>
    /// Gets or sets the selection mode of the component with a default value of <see cref="ThemeSelectionMode.Select"/>.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public ThemeSelectionMode SelectionMode { get; set; } = ThemeSelectionMode.Select;

    /// <summary>
    /// Gets or sets the preview mode of the component with a default value of <see cref="ThemePreviewMode.BothDiagonal"/>.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public ThemePreviewMode PreviewMode { get; set; } = ThemePreviewMode.BothDiagonal;

    /// <summary>
    /// Gets or sets the available themes.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public ICollection<ThemePreset<TTheme>> Available { get; set; }

    /// <summary>
    /// Gets or sets the currently selected theme.
    /// </summary>
    [Parameter] [IgnoreOnObjectEdit]
    public ThemePreset<TTheme> Selected
    {
        get => _selected;
        set
        {
            if (!EqualityComparer<ThemePreset<TTheme>>.Default.Equals(_selected, value))
            {
                _selected = value;
                RaiseChanged();
            }
        }
    }

    /// <summary>
    /// Event that is raised when the <see cref="Selected"/> property is changed.
    /// </summary>
    [Parameter] public EventCallback<ThemePreset<TTheme>> SelectedChanged { get; set; }

    /// <summary>
    /// Event that is raised when the <see cref="SelectedTheme"/> property is changed.
    /// </summary>
    [Parameter] public EventCallback<TTheme> SelectedThemeChanged { get; set; }

    /// <summary>
    /// Event that is raised when the <see cref="SelectedValue"/> property is changed.
    /// </summary>
    [Parameter] public EventCallback<object> SelectedValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the selected theme ads object to allow binding to non generic components
    /// </summary>
    [Parameter] [IgnoreOnObjectEdit]
    public object SelectedValue
    {
        get => Selected;
        set => Selected = value as ThemePreset<TTheme>;
    }

    /// <summary>
    /// Gets or sets the selected theme from the available themes.
    /// </summary>
    [Parameter] [IgnoreOnObjectEdit]
    public TTheme SelectedTheme
    {
        get => Selected?.Theme;
        set => Selected = Available?.FirstOrDefault(x => x?.Theme == value);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Style ??= "min-width: 350px";
    }

    private void RaiseChanged()
    {
        SelectedChanged.InvokeAsync(Selected);
        SelectedThemeChanged.InvokeAsync(SelectedTheme);
        SelectedValueChanged.InvokeAsync(SelectedValue);
    }

    /// <summary>
    /// Returns a preview image of the theme with the specified dimensions.
    /// </summary>
    private string PreviewImage(MudExDimension mudExDimension) => PreviewImage(Selected?.Theme, mudExDimension);

    /// <summary>
    /// Returns a preview image of the specified theme with the specified dimensions.
    /// </summary>
    private string PreviewImage(TTheme theme, MudExDimension mudExDimension)
    {
        if (theme is null)
            return string.Empty;
        return PreviewMode switch
        {
            ThemePreviewMode.DarkOnly => MudExSvg.ApplicationImage(theme, true, mudExDimension),
            ThemePreviewMode.LightOnly => MudExSvg.ApplicationImage(theme, false, mudExDimension),
            ThemePreviewMode.BothDiagonal => MudExSvg.ApplicationImage(theme, mudExDimension, SliceDirection.Diagonal),
            ThemePreviewMode.BothHorizontal => MudExSvg.ApplicationImage(theme, mudExDimension, SliceDirection.Horizontal),
            ThemePreviewMode.BothVertical => MudExSvg.ApplicationImage(theme, mudExDimension, SliceDirection.Vertical),
            _ => MudExSvg.ApplicationImage(theme, mudExDimension, SliceDirection.Diagonal)
        };
    }

    /// <summary>
    /// Returns a boolean indicating whether the dropdown menu of the component is open or closed.
    /// </summary>
    private bool IsOpen()
    {
        // TODO: MudSelect should expose a property for this
        return _mudSelector is null || _mudSelector.ExposeField<bool>("_isOpen");
    }

    /// <summary>
    /// Returns the style of the container of the theme name label.
    /// </summary>
    private string GetThemeNameContainerStyle()
    {
        return MudExStyleBuilder.Default
            .WithMargin("auto auto auto 25px", IsOpen())
            .WithMargin("12px 0 0 18px", !IsOpen())
            .WithStyle(NameContainerStyle, !string.IsNullOrEmpty(NameContainerStyle))
            .Build();
    }

    private string TextStyleStr(TTheme theme)
    {

        var typography = theme.Typography.Default;
        return MudExStyleBuilder.Default
            .WithFontFamily(string.Join(", ", typography.FontFamily ?? Array.Empty<string>()), PreviewFont && typography.FontFamily?.Any() == true)
            .WithFontSize(typography.FontSize, PreviewFont)
            .WithFontWeight(typography.FontWeight.ToString(), PreviewFont)
            .WithLineHeight(typography.LineHeight, PreviewFont)
            .WithLetterSpacing(typography.LetterSpacing, PreviewFont)
            .Build();
    }
}

/// <summary>
/// Enumeration representing the preview mode of a theme.
/// </summary>
public enum ThemePreviewMode
{
    /// <summary>
    /// Only the dark theme is shown in preview.
    /// </summary>
    DarkOnly,

    /// <summary>
    /// Only the light theme is shown in preview.
    /// </summary>
    LightOnly,

    /// <summary>
    /// Both the dark and light theme are shown in preview as one image sliced diagonal.
    /// </summary>
    BothDiagonal,

    /// <summary>
    ///  Both the dark and light theme are shown in preview as one image sliced horizontal.
    /// </summary>
    BothHorizontal,

    /// <summary>
    ///  Both the dark and light theme are shown in preview as one image sliced vertical.
    /// </summary>
    BothVertical,
}

/// <summary>
/// Enumeration representing the selection mode of a theme.
/// </summary>
public enum ThemeSelectionMode
{
    /// <summary>
    /// Selection Dropdown
    /// </summary>
    Select,

    /// <summary>
    /// ItemList
    /// </summary>
    ItemList
}
