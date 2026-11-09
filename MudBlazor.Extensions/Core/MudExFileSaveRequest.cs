namespace MudBlazor.Extensions.Core;

/// <summary>
/// Raised before an editor saves, so the host can take over saving.
/// </summary>
public sealed class MudExFileSaveRequest
{
    /// <summary>
    /// The content to be saved. Only valid during the SaveRequested handler, it is disposed afterwards.
    /// </summary>
    public MudExFileContent Content { get; init; }

    /// <summary>
    /// True if the user asked to save under a new target
    /// </summary>
    public bool SaveAs { get; init; }

    /// <summary>
    /// True if a copy in another format is saved (export). The editor keeps its document, file and dirty state.
    /// </summary>
    public bool IsCopy { get; init; }

    /// <summary>
    /// Set to true if the host saved the content itself
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Set to true to abort saving
    /// </summary>
    public bool Cancel { get; set; }
}

/// <summary>
/// Raised after an editor saved successfully.
/// </summary>
/// <param name="FileName">Name of the saved file</param>
/// <param name="Target">Where the file was saved</param>
public sealed record MudExFileSavedEventArgs(string FileName, MudExFileSaveTarget Target);
