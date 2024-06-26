namespace MudBlazor.Extensions.Core.Enums;

/// <summary>
/// Controls how the tree view will expand.
/// </summary>
public enum TreeViewExpandBehaviour
{
    /// <summary>
    /// Default behaviour.
    /// </summary>
    Default,

    /// <summary>
    /// Only one node can be expanded at a time.
    /// </summary>
    SingleExpand,

    /// <summary>
    /// All nodes are expanded and cannot be collapsed.
    /// </summary>
    None
}