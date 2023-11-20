namespace MudBlazor.Extensions.Core;

/// <summary>
/// Mode for state changes
/// </summary>
public enum StateChangeMode
{
    /// <summary>
    /// Auto is always asynchronous on server side and synchronous on client side
    /// </summary>
    Auto,
    
    /// <summary>
    /// Equals InvokeAsync(StateHasChanged)
    /// </summary>
    Asynchronous,

    /// <summary>
    /// Equals StateHasChanged()
    /// </summary>
    Synchronous
}