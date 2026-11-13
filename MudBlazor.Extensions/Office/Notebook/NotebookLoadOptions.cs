namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>Limits for reading a notebook file; a file that exceeds them is rejected.</summary>
public sealed class NotebookLoadOptions
{
    /// <summary>The largest unpacked size of a single entry in bytes. Default 256 MB.</summary>
    public long MaxEntryBytes { get; set; } = 256L * 1024 * 1024;

    /// <summary>The largest unpacked size of all entries together in bytes. Default 256 MB.</summary>
    public long MaxTotalBytes { get; set; } = 256L * 1024 * 1024;
}
