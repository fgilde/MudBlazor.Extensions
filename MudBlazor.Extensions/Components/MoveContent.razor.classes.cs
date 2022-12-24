namespace MudBlazor.Extensions.Components;

public enum MoveContentPosition
{
    // Dont rename value names. They are used in MoveContent.js
    BeforeBegin,
    AfterBegin,
    BeforeEnd,
    AfterEnd,
    /**
     * Can be used if you don't want to move the content but use the found or not found content
     */
    None
}

public enum MoveContentMode
{
    // Dont rename value names. They are used in MoveContent.js
    MoveToSelector,
    MoveFromSelector
}