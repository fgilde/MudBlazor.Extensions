using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Viewer for unified diff files (.diff, .patch) as produced by <c>git diff</c>, <c>git format-patch</c> or
/// <c>diff -u</c>. Splits the patch into files and hunks, keeps the original and new line numbers and colorizes
/// added, removed and context lines. Fully client-side, no external dependency.
/// </summary>
public partial class MudExFileDisplayDiff : IMudExFileDisplay
{
    // "@@ -12,7 +12,9 @@ optional section heading"
    private static readonly Regex HunkRegex = new(
        @"^@@ -(?<oldStart>\d+)(?:,(?<oldCount>\d+))? \+(?<newStart>\d+)(?:,(?<newCount>\d+))? @@(?<heading>.*)$",
        RegexOptions.Compiled);

    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayDiff);

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    private object _loadedSource;
    private List<DiffFile> _files;
    private string _errorMessage;

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        var fileName = fileDisplayInfos?.FileName ?? string.Empty;
        var isMatch = fileName.EndsWith(".diff", StringComparison.OrdinalIgnoreCase)
                      || fileName.EndsWith(".patch", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(isMatch);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Files", _files?.Count ?? 0 },
            { "Additions", _files?.Sum(f => f.Additions) ?? 0 },
            { "Deletions", _files?.Sum(f => f.Deletions) ?? 0 }
        });
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        // The same viewer instance serves every file of its kind, and FileDisplayInfos is always
        // the same object - so only the values tell one file from the next.
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var sourceChanged = infos.SourceChanged(ref _loadedSource);

        await base.SetParametersAsync(parameters);

        if (sourceChanged)
        {
            _files = null;
            _errorMessage = null;
            await LoadDiffAsync();
            // A viewer knows its own values only now, and its renders do not reach the host.
            await FileDisplayInfos.NotifyMetaChangedAsync();
        }
    }

    private async Task LoadDiffAsync()
    {
        try
        {
            var content = await FileService.ReadAsStringFromFileDisplayInfosAsync(FileDisplayInfos);
            if (content == null)
                return;

            _files = Parse(content);
        }
        catch (Exception e)
        {
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
        }
        StateHasChanged();
    }

    /// <summary>
    /// Walks the patch line by line. A file starts either at a "diff --git" line (git) or at the "---" header of a
    /// plain unified diff; everything before the first hunk of a file is treated as its metadata and skipped.
    /// </summary>
    internal static List<DiffFile> Parse(string content)
    {
        var files = new List<DiffFile>();
        DiffFile current = null;
        DiffHunk hunk = null;
        var oldLine = 0;
        var newLine = 0;

        foreach (var line in content.SplitLines())
        {
            if (line.StartsWith("diff --git ", StringComparison.Ordinal))
            {
                current = new DiffFile { Path = PathFromGitHeader(line) };
                files.Add(current);
                hunk = null;
                continue;
            }

            if (line.StartsWith("--- ", StringComparison.Ordinal))
            {
                // A plain "diff -u" output has no "diff --git" line, so the "---" header opens the file instead.
                if (current == null || current.Hunks.Count > 0)
                {
                    current = new DiffFile();
                    files.Add(current);
                    hunk = null;
                }
                current.OldPath = StripPathPrefix(line[4..]);
                current.Path ??= current.OldPath;
                continue;
            }

            if (line.StartsWith("+++ ", StringComparison.Ordinal) && current != null)
            {
                current.NewPath = StripPathPrefix(line[4..]);
                if (current.NewPath != "/dev/null")
                    current.Path = current.NewPath;
                continue;
            }

            var hunkMatch = HunkRegex.Match(line);
            if (hunkMatch.Success)
            {
                current ??= AddFile(files);
                oldLine = int.Parse(hunkMatch.Groups["oldStart"].Value);
                newLine = int.Parse(hunkMatch.Groups["newStart"].Value);
                hunk = new DiffHunk { Header = line, Heading = hunkMatch.Groups["heading"].Value.Trim() };
                current.Hunks.Add(hunk);
                continue;
            }

            if (hunk == null || line.Length == 0)
                continue;

            switch (line[0])
            {
                case '+':
                    hunk.Lines.Add(new DiffLine(DiffLineKind.Added, line[1..], null, newLine++));
                    current.Additions++;
                    break;
                case '-':
                    hunk.Lines.Add(new DiffLine(DiffLineKind.Removed, line[1..], oldLine++, null));
                    current.Deletions++;
                    break;
                case ' ':
                    hunk.Lines.Add(new DiffLine(DiffLineKind.Context, line[1..], oldLine++, newLine++));
                    break;
                case '\\':
                    // "\ No newline at end of file" - belongs to the previous line, not a change of its own.
                    hunk.Lines.Add(new DiffLine(DiffLineKind.Meta, line, null, null));
                    break;
                default:
                    // Anything else after a hunk ends it (git trailers, "-- " signature of format-patch, ...)
                    hunk = null;
                    break;
            }
        }

        return files.Where(f => f.Hunks.Count > 0).ToList();
    }

    private static DiffFile AddFile(List<DiffFile> files)
    {
        var file = new DiffFile();
        files.Add(file);
        return file;
    }

    private static string PathFromGitHeader(string line)
    {
        // "diff --git a/src/File.cs b/src/File.cs" - the b/ side is the interesting one for renames too.
        var parts = line[11..].Split(' ');
        return StripPathPrefix(parts.Length > 1 ? parts[^1] : parts[0]);
    }

    private static string StripPathPrefix(string path)
    {
        path = path.Split('\t')[0].Trim();
        return path.Length > 2 && (path.StartsWith("a/", StringComparison.Ordinal) || path.StartsWith("b/", StringComparison.Ordinal))
            ? path[2..]
            : path;
    }

    private static string CssForKind(DiffLineKind kind) => kind switch
    {
        DiffLineKind.Added => "mud-ex-diff-added",
        DiffLineKind.Removed => "mud-ex-diff-removed",
        DiffLineKind.Meta => "mud-ex-diff-meta",
        _ => string.Empty
    };

    private static string MarkerForKind(DiffLineKind kind) => kind switch
    {
        DiffLineKind.Added => "+",
        DiffLineKind.Removed => "-",
        DiffLineKind.Meta => string.Empty,
        _ => " "
    };

    /// <summary>The kind of a single diff line.</summary>
    internal enum DiffLineKind
    {
        /// <summary>Unchanged line shown for context.</summary>
        Context,
        /// <summary>Line only present in the new file.</summary>
        Added,
        /// <summary>Line only present in the old file.</summary>
        Removed,
        /// <summary>Diff metadata such as "\ No newline at end of file".</summary>
        Meta
    }

    /// <summary>One line inside a hunk together with its position in the old and new file.</summary>
    internal sealed record DiffLine(DiffLineKind Kind, string Text, int? OldNumber, int? NewNumber);

    /// <summary>One "@@" section of a file diff.</summary>
    internal sealed class DiffHunk
    {
        /// <summary>The raw "@@ ... @@" line.</summary>
        public string Header { get; init; }
        /// <summary>The optional section name git puts behind the closing "@@".</summary>
        public string Heading { get; init; }
        /// <summary>All lines of this hunk.</summary>
        public List<DiffLine> Lines { get; } = new();
    }

    /// <summary>All hunks belonging to one file of the patch.</summary>
    internal sealed class DiffFile
    {
        /// <summary>Display path, the new path where available.</summary>
        public string Path { get; set; }
        /// <summary>Path taken from the "---" header.</summary>
        public string OldPath { get; set; }
        /// <summary>Path taken from the "+++" header.</summary>
        public string NewPath { get; set; }
        /// <summary>Number of added lines.</summary>
        public int Additions { get; set; }
        /// <summary>Number of removed lines.</summary>
        public int Deletions { get; set; }
        /// <summary>The hunks of this file.</summary>
        public List<DiffHunk> Hunks { get; } = new();

        /// <summary>True when the file is created by this patch.</summary>
        public bool IsNew => OldPath == "/dev/null";
        /// <summary>True when the file is deleted by this patch.</summary>
        public bool IsDeleted => NewPath == "/dev/null";
    }
}
