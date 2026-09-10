namespace MudBlazor.Extensions.Core.Enums;

/// <summary>
/// How <see cref="Components.MudExFileGrid"/> presents its entries.
/// </summary>
public enum MudExFileGridView
{
    /// <summary>A grid of tiles, each with a large icon and the name below it.</summary>
    Tiles,

    /// <summary>One row per entry with sortable columns for name, size, type and date.</summary>
    Details
}

/// <summary>
/// The column <see cref="Components.MudExFileGrid"/> sorts by.
/// </summary>
public enum MudExFileGridSort
{
    /// <summary>By name. Directories still come first.</summary>
    Name,

    /// <summary>By size. Directories have none, so they keep their name order.</summary>
    Size,

    /// <summary>By content type.</summary>
    Type,

    /// <summary>By last modification date.</summary>
    Modified
}
