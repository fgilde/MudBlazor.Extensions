namespace MudBlazor.Extensions.Components;

/// <summary>
/// Position to move the ChildContent to a specific element that can defined by ElementSelector.
/// </summary>
public enum MoveContentPosition
{
    // Don't rename value names. They are used in MoveContent.js

    /// <summary>
    /// Move content before the element
    /// </summary>
    BeforeBegin,

    /// <summary>
    /// Move content after begin of element
    /// </summary>
    AfterBegin,

    /// <summary>
    /// Move content before end of element
    /// </summary>
    BeforeEnd,

    /// <summary>
    /// Move content to last after end of element
    /// </summary>
    AfterEnd,
    /**
     * Can be used if you don't want to move the content but use the found or not found content
     */
    None
}

/// <summary>
/// Mode to move content
/// </summary>
public enum MoveContentMode
{
    // Dont rename value names. They are used in MoveContent.js
    /// <summary>
    /// MoveContent to selector
    /// </summary>
    MoveToSelector,

    /// <summary>
    /// Move content from selector
    /// </summary>
    MoveFromSelector
}