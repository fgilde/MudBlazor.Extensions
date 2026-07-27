using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions;
using Try.Core;
using TryMudEx.Client.Services;

namespace TryMudEx.Client.Components;

public partial class FileTree
{
    [Inject] private IDialogService DialogService { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; }

    [Parameter] public IEnumerable<CodeFile> Files { get; set; } = Array.Empty<CodeFile>();
    [Parameter] public string ActivePath { get; set; }
    [Parameter] public bool ShowHiddenFiles { get; set; }

    [Parameter] public EventCallback<string> OnOpen { get; set; }
    [Parameter] public EventCallback<string> OnCreate { get; set; }
    [Parameter] public EventCallback<CodeFile> OnCreateFromTemplate { get; set; }
    [Parameter] public EventCallback<(string OldPath, string NewPath)> OnRename { get; set; }
    [Parameter] public EventCallback<string> OnDelete { get; set; }

    private List<TreeItemData<string>> _items = new();

    private IEnumerable<CodeFile> AvailableTemplates =>
        CodeFileTemplates.All().Where(t => Files.All(f => f.Path != t.Path));

    protected override void OnParametersSet()
    {
        _items = BuildTree();
    }

    private List<TreeItemData<string>> BuildTree()
    {
        var visibleFiles = Files
            .Where(f => ShowHiddenFiles || f.Type != CodeFileType.Hidden)
            .Select(f => (Segments: f.Path.Split('/'), File: f))
            .ToList();

        return BuildLevel(visibleFiles, 0);
    }

    private static List<TreeItemData<string>> BuildLevel(List<(string[] Segments, CodeFile File)> entries, int depth)
    {
        var result = new List<TreeItemData<string>>();

        foreach (var folder in entries
                     .Where(e => e.Segments.Length > depth + 1)
                     .GroupBy(e => e.Segments[depth])
                     .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            result.Add(new TreeItemData<string>
            {
                Text = folder.Key,
                Icon = Icons.Material.Outlined.Folder,
                Expanded = true,
                Children = BuildLevel(folder.ToList(), depth + 1),
            });
        }

        foreach (var entry in entries
                     .Where(e => e.Segments.Length == depth + 1)
                     .OrderBy(e => e.Segments[depth], StringComparer.OrdinalIgnoreCase))
        {
            result.Add(new TreeItemData<string>
            {
                Text = entry.Segments[depth],
                Value = entry.File.Path,
                Icon = entry.File.Type == CodeFileType.CSharp ? Icons.Material.Outlined.DataObject : Icons.Material.Outlined.Code,
            });
        }

        return result;
    }

    private Task HandleSelected(string value)
        => value != null ? OnOpen.InvokeAsync(value) : Task.CompletedTask;

    private async Task PromptCreate(string folderPrefix)
    {
        var suggestion = string.IsNullOrEmpty(folderPrefix) ? "MyComponent.razor" : $"{folderPrefix}/MyComponent.razor";
        var name = await DialogService.PromptAsync("New file", "Enter file name (folders with '/', e.g. Components/Card.razor)", suggestion,
            icon: Icons.Material.Outlined.NoteAdd, canConfirm: s => !string.IsNullOrWhiteSpace(s));
        if (string.IsNullOrWhiteSpace(name)) return;

        var normalized = CodeFilesHelper.NormalizeCodeFilePath(name, out var error);
        if (error != null)
        {
            Snackbar.Add(error, Severity.Error);
            return;
        }
        if (Files.Any(f => string.Equals(f.Path, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            Snackbar.Add($"File '{normalized}' already exists.", Severity.Error);
            return;
        }

        await OnCreate.InvokeAsync(normalized);
    }

    private Task PromptCreateFolder() => PromptCreate("NewFolder");

    private async Task PromptRename(string oldPath)
    {
        var name = await DialogService.PromptAsync("Rename", $"New name for {oldPath}", oldPath,
            icon: Icons.Material.Outlined.DriveFileRenameOutline, canConfirm: s => !string.IsNullOrWhiteSpace(s) && s != oldPath);
        if (string.IsNullOrWhiteSpace(name) || name == oldPath) return;

        var normalized = CodeFilesHelper.NormalizeCodeFilePath(name, out var error);
        if (error != null)
        {
            Snackbar.Add(error, Severity.Error);
            return;
        }
        if (Files.Any(f => string.Equals(f.Path, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            Snackbar.Add($"File '{normalized}' already exists.", Severity.Error);
            return;
        }

        await OnRename.InvokeAsync((oldPath, normalized));
    }

    private async Task ConfirmDelete(string path)
    {
        var confirmed = await DialogService.ShowConfirmationDialogAsync("Delete file", $"Delete '{path}'? This cannot be undone.");
        if (confirmed)
            await OnDelete.InvokeAsync(path);
    }
}
