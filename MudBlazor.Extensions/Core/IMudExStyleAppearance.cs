namespace MudBlazor.Extensions.Core;

/// <summary>
/// IMudExStyleAppearance holds an applicable style string
/// </summary>
public interface IMudExStyleAppearance : IMudExAppearance
{
    /// <summary>
    /// CSS Style string to apply
    /// </summary>
    public string Style { get; }
}

/// <summary>
/// IMudExAppearance is used to know an applicable style or class holder
/// </summary>
public interface IMudExAppearance
{}