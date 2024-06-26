using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'flex-flow' property values.
/// </summary>
public enum FlexFlow
{
    /// <summary>
    /// Set flex to nowrap.
    /// </summary>
    [Description("nowrap")]
    NoWrap,

    /// <summary>
    /// Set flex direction to row and wrap to wrap-reverse.
    /// </summary>
    [Description("wrap")]
    Wrap,

    /// <summary>
    /// Set flex direction to row and wrap to wrap-reverse.
    /// </summary>
    [Description("wrap-reverse")]
    WrapReverse,

    /// <summary>
    /// Set flex direction to row
    /// </summary>
    [Description("row")]
    Row,

    /// <summary>
    /// Set flex direction to row reverse
    /// </summary>
    [Description("row-reverse")]
    RowReverse,

    /// <summary>
    /// Set flex direction to row and wrap to nowrap.
    /// </summary>
    [Description("row nowrap")]
    RowNowrap,

    /// <summary>
    /// Set flex direction to row and wrap to wrap.
    /// </summary>
    [Description("row wrap")]
    RowWrap,

    /// <summary>
    /// Set flex direction to row and wrap to wrap-reverse.
    /// </summary>
    [Description("row wrap-reverse")]
    RowWrapReverse,

    /// <summary>
    /// Set flex direction to row-reverse and wrap to nowrap.
    /// </summary>
    [Description("row-reverse nowrap")]
    RowReverseNowrap,

    /// <summary>
    /// Set flex direction to row-reverse and wrap to wrap.
    /// </summary>
    [Description("row-reverse wrap")]
    RowReverseWrap,

    /// <summary>
    /// Set flex direction to row-reverse and wrap to wrap-reverse.
    /// </summary>
    [Description("row-reverse wrap-reverse")]
    RowReverseWrapReverse,

    /// <summary>
    /// Set flex direction to column and wrap to nowrap.
    /// </summary>
    [Description("column")]
    Column,

    /// <summary>
    /// Set flex direction to column and wrap to nowrap.
    /// </summary>
    [Description("column-reverse")]
    ColumnReverse,

    /// <summary>
    /// Set flex direction to column and wrap to nowrap.
    /// </summary>
    [Description("column nowrap")]
    ColumnNowrap,

    /// <summary>
    /// Set flex direction to column and wrap to wrap.
    /// </summary>
    [Description("column wrap")]
    ColumnWrap,

    /// <summary>
    /// Set flex direction to column and wrap to wrap-reverse.
    /// </summary>
    [Description("column wrap-reverse")]
    ColumnWrapReverse,

    /// <summary>
    /// Set flex direction to column-reverse and wrap to nowrap.
    /// </summary>
    [Description("column-reverse nowrap")]
    ColumnReverseNowrap,

    /// <summary>
    /// Set flex direction to column-reverse and wrap to wrap.
    /// </summary>
    [Description("column-reverse wrap")]
    ColumnReverseWrap,

    /// <summary>
    /// Set flex direction to column-reverse and wrap to wrap-reverse.
    /// </summary>
    [Description("column-reverse wrap-reverse")]
    ColumnReverseWrapReverse,

    /// <summary>
    /// Inherit the value from the parent element.
    /// </summary>
    [Description("inherit")]
    Inherit,

    /// <summary>
    /// Use the initial value.
    /// </summary>
    [Description("initial")]
    Initial,

    /// <summary>
    /// Revert
    /// </summary>
    [Description("revert")]
    Revert,

    /// <summary>
    /// RevertLayer
    /// </summary>
    [Description("revert-layer")]
    RevertLayer,

    /// <summary>
    /// Unset the value.
    /// </summary>
    [Description("unset")]
    Unset
}