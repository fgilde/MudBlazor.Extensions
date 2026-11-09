using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A dropdown list in the ribbon, for example the font name. With <see cref="Editable"/> the user may also type a value.
/// </summary>
/// <typeparam name="T">Type of the items.</typeparam>
public partial class MudExRibbonSelect<T>
{
    private readonly string _listId = $"mud-ex-ribbon-list-{Guid.NewGuid():N}";
    private int _inputVersion;

    /// <summary>
    /// Items to choose from.
    /// </summary>
    [Parameter] public IEnumerable<T> Items { get; set; }

    /// <summary>
    /// The selected value.
    /// </summary>
    [Parameter] public T Value { get; set; }

    /// <summary>
    /// Raised when the user selects or types a value.
    /// </summary>
    [Parameter] public EventCallback<T> ValueChanged { get; set; }

    /// <summary>
    /// Text of an item. Defaults to <see cref="object.ToString"/>.
    /// </summary>
    [Parameter] public Func<T, string> ItemText { get; set; }

    /// <summary>
    /// Whether the user may type a value. Typed text selects the item with the same text;
    /// for string items any text is accepted.
    /// </summary>
    [Parameter] public bool Editable { get; set; }

    /// <summary>
    /// Width of the field in pixels.
    /// </summary>
    [Parameter] public int Width { get; set; } = 120;

    /// <inheritdoc />
    public override bool IsCommand => false;

    /// <inheritdoc />
    protected override object StateKey => (base.StateKey, Items, Value, Editable, Width, _inputVersion);

    private string WidthStyle => $"width:{Width}px";

    private string TextOf(T item) => item == null ? null : ItemText?.Invoke(item) ?? item.ToString();

    private bool IsSelected(T item) => EqualityComparer<T>.Default.Equals(item, Value);

    private Task OnSelectedAsync(ChangeEventArgs args)
        => int.TryParse(args.Value?.ToString(), out var index) && (Items ?? []).Skip(index).Take(1).ToList() is [var item]
            ? SetValueAsync(item)
            : Task.CompletedTask;

    private Task OnTextChangedAsync(ChangeEventArgs args)
    {
        var text = args.Value?.ToString()?.Trim() ?? string.Empty;
        foreach (var item in Items ?? [])
        {
            if (string.Equals(TextOf(item), text, StringComparison.CurrentCultureIgnoreCase))
                return SetValueAsync(item);
        }
        if (text is T typed)
            return SetValueAsync(typed);
        _inputVersion++;
        NotifyStateChanged();
        return Task.CompletedTask;
    }

    private async Task SetValueAsync(T value)
    {
        if (Disabled)
            return;
        Value = value;
        NotifyStateChanged();
        await ValueChanged.InvokeAsync(value);
    }
}
