using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'cursor' property values.
/// </summary>
public enum Cursor
{
    // Basic
    [Description("auto")]
    Auto,

    [Description("default")]
    Default,

    [Description("none")]
    None,

    [Description("context-menu")]
    ContextMenu,

    [Description("help")]
    Help,

    [Description("pointer")]
    Pointer,

    [Description("progress")]
    Progress,

    [Description("wait")]
    Wait,


    // Selection / Editing
    [Description("cell")]
    Cell,

    [Description("crosshair")]
    Crosshair,

    [Description("text")]
    Text,

    [Description("vertical-text")]
    VerticalText,


    // Drag & Drop
    [Description("alias")]
    Alias,

    [Description("copy")]
    Copy,

    [Description("move")]
    Move,

    [Description("no-drop")]
    NoDrop,

    [Description("not-allowed")]
    NotAllowed,

    [Description("grab")]
    Grab,

    [Description("grabbing")]
    Grabbing,


    // Resize cursors
    [Description("all-scroll")]
    AllScroll,

    [Description("col-resize")]
    ColResize,

    [Description("row-resize")]
    RowResize,

    [Description("n-resize")]
    NResize,

    [Description("e-resize")]
    EResize,

    [Description("s-resize")]
    SResize,

    [Description("w-resize")]
    WResize,

    [Description("ne-resize")]
    NeResize,

    [Description("nw-resize")]
    NwResize,

    [Description("se-resize")]
    SeResize,

    [Description("sw-resize")]
    SwResize,

    [Description("ns-resize")]
    NsResize,

    [Description("ew-resize")]
    EwResize,

    [Description("nesw-resize")]
    NeswResize,

    [Description("nwse-resize")]
    NwseResize,


    // Zoom cursors
    [Description("zoom-in")]
    ZoomIn,

    [Description("zoom-out")]
    ZoomOut

}