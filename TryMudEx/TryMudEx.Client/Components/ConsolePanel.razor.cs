using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TryMudEx.Client.Services;

namespace TryMudEx.Client.Components;

public record ConsoleEntry(string Level, string Text, long Ts);

public partial class ConsolePanel : IDisposable
{
    private const int MaxEntries = 2000;
    private const string ScrollContainerId = "try-console-output";

    [Inject] public IJSRuntime JsRuntime { get; set; }
    [Inject] private PlaygroundLocalizer L { get; set; }

    private readonly List<ConsoleEntry> _entries = new();
    private DotNetObjectReference<ConsolePanel> _ref;
    private string _filter = string.Empty;
    private HashSet<string> _levels = new() { "error", "warn", "log" };
    private bool _follow = true;

    private IEnumerable<ConsoleEntry> Filtered => _entries.Where(e =>
        _levels.Contains(NormalizeLevel(e.Level)) &&
        (string.IsNullOrEmpty(_filter) || e.Text.Contains(_filter, StringComparison.OrdinalIgnoreCase)));

    // info/debug ride along with the plain "log" filter chip
    private static string NormalizeLevel(string level) => level switch
    {
        "error" => "error",
        "warn" => "warn",
        _ => "log",
    };

    private static string LineStyle(string level) => NormalizeLevel(level) switch
    {
        "error" => "color: var(--mud-palette-error);",
        "warn" => "color: var(--mud-palette-warning);",
        _ => string.Empty,
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _ref = DotNetObjectReference.Create(this);
            var existing = await JsRuntime.InvokeAsync<ConsoleEntry[]>("Try.Console.init", _ref);
            if (existing?.Length > 0)
            {
                _entries.AddRange(existing);
                StateHasChanged();
            }
        }
    }

    [JSInvokable]
    public async Task OnConsoleBatch(ConsoleEntry[] batch)
    {
        if (batch == null || batch.Length == 0) return;

        _entries.AddRange(batch);
        if (_entries.Count > MaxEntries)
            _entries.RemoveRange(0, _entries.Count - MaxEntries);

        StateHasChanged();

        if (_follow)
        {
            await Task.Yield(); // render first, then scroll
            await JsRuntime.InvokeVoidAsync("Try.Console.scrollToBottom", "#" + ScrollContainerId);
        }
    }

    private void Clear()
    {
        _entries.Clear();
        JsRuntime.InvokeVoidAsync("Try.Console.clear");
    }

    private async Task CopyAsync()
    {
        var text = string.Join(Environment.NewLine, Filtered.Select(e => e.Text));
        await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }

    public void Dispose()
    {
        JsRuntime.InvokeVoidAsync("Try.Console.dispose");
        _ref?.Dispose();
    }
}
