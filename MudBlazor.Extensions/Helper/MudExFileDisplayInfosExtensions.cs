using MudBlazor.Extensions.Components;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Helpers for <see cref="IMudExFileDisplay"/> implementations to tell whether they have a file to load, and
/// whether it is still the same one.
/// </summary>
public static class MudExFileDisplayInfosExtensions
{
    /// <summary>True when the infos carry something that can actually be read.</summary>
    public static bool HasSource(this IMudExFileDisplayInfos infos)
        => infos != null && (!string.IsNullOrEmpty(infos.Url) || infos.ContentStream is { Length: > 0 });

    /// <summary>
    /// Identifies the file the infos point at.
    /// </summary>
    /// <remarks>
    /// It has to be the values, not the instance: <see cref="MudExFileDisplay"/> passes itself as the infos,
    /// so a viewer sees the very same object for every file it is asked to show.
    /// </remarks>
    public static object SourceKey(this IMudExFileDisplayInfos infos)
        => infos == null ? null : (infos.FileName, infos.Url, infos.ContentStream);
    /// <summary>
    /// Tells the hosting <see cref="MudExFileDisplay"/> that the viewer's metadata is available now. Called at
    /// the end of a viewer's load, since that is when values like a row count or a bitrate first exist.
    /// </summary>
    public static Task NotifyMetaChangedAsync(this IMudExFileDisplayInfos infos)
        => infos is MudExFileDisplay display ? display.RefreshMetaInformationAsync() : Task.CompletedTask;

    /// <summary>
    /// True when the infos describe a different file than <paramref name="loadedSource"/> stands for, which is
    /// updated to the new one. Two files of the same kind share a viewer instance, so this is what tells the
    /// viewer to load again instead of keeping the first file on screen.
    /// </summary>
    public static bool SourceChanged(this IMudExFileDisplayInfos infos, ref object loadedSource)
    {
        if (!infos.HasSource())
            return false;

        var key = infos.SourceKey();
        if (Equals(loadedSource, key))
            return false;

        loadedSource = key;
        return true;
    }
}
