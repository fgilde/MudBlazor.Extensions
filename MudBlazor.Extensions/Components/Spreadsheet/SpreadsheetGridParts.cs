using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// One row of the visible window. Its parameters are primitives, so Blazor skips it when the grid re-renders and neither the
/// window columns nor the row's version changed.
/// </summary>
internal sealed class SpreadsheetGridRow : ComponentBase
{
    [CascadingParameter] public MudExSpreadsheetGrid Grid { get; set; }

    [Parameter] public int Row { get; set; }

    [Parameter] public int FirstColumn { get; set; }

    [Parameter] public int LastColumn { get; set; }

    [Parameter] public long Version { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => Grid?.BuildRow(builder, Row, FirstColumn, LastColumn);
}

/// <summary>Reference highlights and formula assist while editing in a cell; re-renders on its own while typing.</summary>
internal sealed class SpreadsheetEditOverlay : ComponentBase, IDisposable
{
    private MudExSpreadsheetGrid _grid;

    [CascadingParameter] public MudExSpreadsheetGrid Grid { get; set; }

    public void Dispose()
    {
        if (_grid != null) _grid.EditChanged -= OnChanged;
    }

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_grid, Grid)) return;
        if (_grid != null) _grid.EditChanged -= OnChanged;
        _grid = Grid;
        if (_grid != null) _grid.EditChanged += OnChanged;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_grid is not { IsEditing: true, EditFromBar: false }) return;
        builder.OpenRegion(0);
        _grid.BuildReferences(builder);
        builder.CloseRegion();
        builder.OpenElement(1, "div");
        builder.AddAttribute(2, "class", "mud-ex-ssg-assist-host");
        builder.AddAttribute(3, "style", _grid.AssistHost);
        builder.OpenRegion(4);
        _grid.BuildAssist(builder, this);
        builder.CloseRegion();
        builder.CloseElement();
    }

    private void OnChanged() => _ = InvokeAsync(StateHasChanged);
}

/// <summary>Renders its content once. Used for large ribbon declarations that never change, so editor renders skip them.</summary>
internal sealed class SpreadsheetStaticContent : ComponentBase
{
    [Parameter] public RenderFragment ChildContent { get; set; }

    protected override bool ShouldRender() => false;

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
}
