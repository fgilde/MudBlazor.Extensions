namespace MudBlazor.Extensions.Core;

/// <summary>
/// IMudExClassAppearance holds an applicable class string
/// </summary>
public interface IMudExClassAppearance: IMudExAppearance
{
    /// <summary>
    /// Class to apply
    /// </summary>
    public string Class { get; }
}