using MudBlazor.Extensions.Helper;
using Nextended.Core;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Core.FileManager;

/// <summary>
/// One file or directory in a file structure.
/// </summary>
/// <remarks>
/// Derives from <see cref="Hierarchical{T}"/>, which is what lets <c>MudExTreeView</c> render it and drive
/// lazy loading through <see cref="Hierarchical{T}.LoadChildrenFunc"/>. A manager that loads on demand sets
/// that func on every directory node it creates; a manager that already knows the whole tree leaves it unset.
/// </remarks>
public class MudExFileStructureNode : Hierarchical<MudExFileStructureNode>
{
    private string _icon;
    private MudExColor? _color;
    private string _contentType;
    private string _name;

    /// <summary>
    /// Display name, the last path segment. Setting it drops the derived content type, icon and color, so a
    /// rename from <c>a.txt</c> to <c>a.pdf</c> shows the pdf icon instead of the cached text one.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            _contentType = null;
            _icon = null;
            _color = null;
        }
    }

    /// <summary>
    /// Identity of this node, defined and interpreted by the manager that created it. Treated as opaque
    /// everywhere else, so a manager is free to use posix paths, urls or ids.
    /// </summary>
    public string FullPath { get; set; }

    /// <summary>True for a directory.</summary>
    public bool IsDirectory { get; set; }

    /// <summary>Size in bytes, 0 for a directory.</summary>
    public long Size { get; set; }

    /// <summary>Last write time, when the manager knows one.</summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// The manager's own handle for this node, for example the id of a browser file system handle. Not used
    /// by the components.
    /// </summary>
    public object Handle { get; set; }

    /// <summary>
    /// Mime type. Derived from <see cref="Name"/> when not set explicitly, and always null for a directory.
    /// </summary>
    public string ContentType
    {
        get => IsDirectory ? null : _contentType ??= MimeType.GetMimeType(Name ?? string.Empty);
        set => _contentType = value;
    }

    /// <summary>Icon for this node, matching what the archive viewer shows for the same file type.</summary>
    public string Icon => _icon ?? GetIcon();

    /// <summary>Preferred color for this node.</summary>
    public MudExColor Color => IsDirectory
        ? (IsExpanded ? MudExColor.Primary : MudExColor.Secondary)
        : _color ??= BrowserFileExt.GetPreferredColor(ContentType);

    /// <inheritdoc />
    public override string ToString() => Name;

    // Only a file may cache: a directory's glyph depends on IsExpanded and has to be recomputed every time.
    private string GetIcon() => IsDirectory
        ? IsExpanded ? Icons.Material.Filled.FolderOpen : Icons.Material.Filled.Folder
        : _icon = BrowserFileExt.GetIcon(Name, ContentType);
}
