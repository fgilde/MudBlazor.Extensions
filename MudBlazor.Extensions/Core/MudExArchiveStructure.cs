using MudBlazor.Extensions.Helper;
using Nextended.Blazor.Models;
using Nextended.Core;

namespace MudBlazor.Extensions.Core;

public class MudExArchiveStructure : ArchiveStructureBase<MudExArchiveStructure>
{
    string _icon;
    MudExColor? _color;
    public string Icon => _icon ?? GetIcon();
    public MudExColor Color => _color ??= GetColor();
    
    private MudExColor GetColor()
    {       
        if (Parent == null)
            return BrowserFileExt.GetPreferredColor("application/zip");
        if (IsDirectory)
            return IsExpanded ? MudExColor.Primary : MudExColor.Secondary;
        return BrowserFile.GetPreferredColor();
    }

    private string GetIcon()
    {
        if (Parent == null)
            return _icon = BrowserFileExt.GetIcon(Name, BrowserFile?.GetContentType() ?? MimeType.GetMimeType(Name)); //Icons.Material.Filled.Archive;
        if (IsDirectory)
            return IsExpanded ? Icons.Material.Filled.FolderOpen : Icons.Material.Filled.Folder;
        return _icon = BrowserFile?.GetIcon();
    }
}