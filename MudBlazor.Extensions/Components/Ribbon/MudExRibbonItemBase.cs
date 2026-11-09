using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Base class of all ribbon items. An item renders no markup where it is declared; it registers with its
/// nearest host (group, row or menu) and is drawn by the ribbon through <see cref="Render"/>.
/// </summary>
public abstract partial class MudExRibbonItemBase : ComponentBase, IDisposable
{
    private object _stateKey;
    private string _id;
    private Dictionary<string, object> _pinAttributes;

    [CascadingParameter] internal MudExRibbon Ribbon { get; set; }

    [CascadingParameter] internal MudExRibbonTab Tab { get; set; }

    [CascadingParameter] internal MudExRibbonGroup Group { get; set; }

    [CascadingParameter] internal IMudExRibbonItemHost Host { get; set; }

    /// <summary>
    /// Stable identifier of the command, used for example by <see cref="MudExRibbon.PinnedCommandIds"/>.
    /// Defaults to "{Tab}/{Group}/{Text}"; entries of a menu add the text of each parent menu before their own text,
    /// for example "Home/Font/Case/UPPERCASE". Two items with the same text in one group or menu get the same default,
    /// so set the id explicitly for them.
    /// </summary>
    [Parameter]
    public string Id
    {
        get => _id ?? string.Join("/", Location.Append(Text));
        set => _id = value;
    }

    /// <summary>
    /// Caption of the item.
    /// </summary>
    [Parameter] public string Text { get; set; }

    /// <summary>
    /// Icon of the item, for example a value of <see cref="Icons"/>.
    /// </summary>
    [Parameter] public string Icon { get; set; }

    /// <summary>
    /// Tooltip title. Defaults to <see cref="Text"/>.
    /// </summary>
    [Parameter] public string Tooltip { get; set; }

    /// <summary>
    /// Longer description shown in the tooltip below the title.
    /// </summary>
    [Parameter] public string Description { get; set; }

    /// <summary>
    /// Keyboard shortcut shown in brackets after the tooltip title, for example "Ctrl+B". The ribbon does not bind the key.
    /// </summary>
    [Parameter] public string Shortcut { get; set; }

    /// <summary>
    /// Key tip shown for the item in key tip mode.
    /// </summary>
    [Parameter] public string KeyTip { get; set; }

    /// <summary>
    /// Declared size of the item.
    /// </summary>
    [Parameter] public MudExRibbonItemSize Size { get; set; } = MudExRibbonItemSize.Medium;

    /// <summary>
    /// Whether the item is disabled.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Whether the item is shown.
    /// </summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>
    /// Whether the item is a command that appears in command search and menus.
    /// </summary>
    public virtual bool IsCommand => true;

    /// <summary>
    /// Values of all visible state. The ribbon is redrawn only when this value changes.
    /// </summary>
    protected virtual object StateKey => (Id, Text, Icon, Tooltip, Description, Shortcut, KeyTip, Size, Disabled, Visible);

    /// <summary>
    /// Tooltip title including the shortcut, for example "Bold (Ctrl+B)".
    /// </summary>
    protected string TooltipTitle
    {
        get
        {
            var title = Tooltip ?? Text;
            return string.IsNullOrEmpty(Shortcut) ? title : $"{title} ({Shortcut})";
        }
    }

    internal virtual IReadOnlyList<MudExRibbonItemBase> Children => [];

    internal IEnumerable<MudExRibbonItemBase> Ancestors
    {
        get
        {
            for (var parent = Host as MudExRibbonItemBase; parent != null; parent = parent.Host as MudExRibbonItemBase)
                yield return parent;
        }
    }

    private IEnumerable<string> Location
        => new[] { Tab?.Text, Group?.Text }.Concat(Ancestors.Reverse().Where(a => a is not MudExRibbonRow).Select(a => a.Text));

    internal string LocationText => string.Join(" \u203A ", Location.Where(s => !string.IsNullOrEmpty(s)));

    internal bool IsReachable => Visible && !Disabled && Tab is { Visible: true } && Ancestors.All(a => a.Visible && !a.Disabled);

    internal bool Matches(string text)
        => new[] { Text, Tooltip, Description }.Any(s => s?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true);

    internal IReadOnlyDictionary<string, object> SlotAttributes(MudExRibbonItemLayout layout)
        => HasContextMenu(layout)
            ? _pinAttributes ??= new() { ["oncontextmenu"] = EventCallback.Factory.Create<MouseEventArgs>(this, e => Ribbon?.OpenContextMenuAsync(this, e) ?? Task.CompletedTask) }
            : null;

    internal bool HasContextMenu(MudExRibbonItemLayout layout) => IsCommand && Tab != null && layout != MudExRibbonItemLayout.MenuEntry;

    internal MudExRibbonItemLayout DeclaredLayout => Size switch
    {
        MudExRibbonItemSize.Large => MudExRibbonItemLayout.Large,
        MudExRibbonItemSize.Small => MudExRibbonItemLayout.Small,
        _ => MudExRibbonItemLayout.Medium
    };

    /// <summary>
    /// Lower case name of a layout as used in css class names.
    /// </summary>
    protected static string LayoutName(MudExRibbonItemLayout layout) => layout.ToString().ToLowerInvariant();

    /// <summary>
    /// Renders the item in the given layout.
    /// </summary>
    public abstract RenderFragment Render(MudExRibbonItemLayout layout);

    /// <summary>
    /// Executes the command of the item.
    /// </summary>
    public virtual Task ExecuteAsync() => Task.CompletedTask;

    internal async Task ExecuteCommandAsync()
    {
        await ExecuteAsync();
        Ribbon?.NotifyCommandExecuted();
    }

    /// <summary>
    /// Redraws the ribbon if <see cref="StateKey"/> changed since the last call.
    /// </summary>
    protected void NotifyStateChanged()
    {
        var key = StateKey;
        if (Equals(key, _stateKey))
            return;
        _stateKey = key;
        Ribbon?.Invalidate();
    }

    /// <inheritdoc />
    protected override void OnInitialized() => Host?.Register(this);

    /// <inheritdoc />
    protected override void OnParametersSet() => NotifyStateChanged();

    /// <inheritdoc />
    public virtual void Dispose() => Host?.Unregister(this);
}
