using Microsoft.AspNetCore.Components.Forms;
using SharpCompress.Common;

namespace MudBlazor.Extensions.Core.ArchiveHandling;

//public interface IArchiveBrowserFile<TArchiveEntry> : IArchiveBrowserFile
//{    
//    TArchiveEntry Entry { get; }    
//}

public interface IArchiveBrowserFile: IBrowserFile
{
    byte[] FileBytes { get; }
    public string FullName { get; }
    public string Path { get; }
    public bool IsDirectory { get; }
    public string[] PathArray { get; }
    public string ParentDirectoryName { get; }
}