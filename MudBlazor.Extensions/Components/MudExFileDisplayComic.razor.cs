using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;
using Nextended.Blazor.Models;
using Nextended.Blazor.Extensions;
using Nextended.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Comic book viewer for CBZ and CBR files. Pages are sorted naturally and displayed with the image viewer.
/// </summary>
public partial class MudExFileDisplayComic : IMudExFileDisplay
{
    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".tif", ".tiff", ".tga", ".qoi", ".pbm", ".pgm", ".ppm", ".avif" };
    private object _loadedSource;
    private IList<IArchivedBrowserFile> _pages = new List<IArchivedBrowserFile>();
    private MemoryStream _pageStream;
    private IMudExFileDisplayInfos _currentPage;
    private int _selectedIndex = -1;
    private bool _loading;
    private string _errorMessage;

    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayComic);

    /// <inheritdoc />
    public int RenderPriority => 120;

    /// <inheritdoc />
    [Parameter] public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>Reference to the parent file display.</summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos infos, IMudExFileService fileService)
    {
        var extension = Path.GetExtension(infos?.FileName ?? string.Empty);
        var result = extension.Equals(".cbz", StringComparison.OrdinalIgnoreCase)
                     || extension.Equals(".cbr", StringComparison.OrdinalIgnoreCase)
                     || MimeType.Matches(infos?.ContentType, "application/vnd.comicbook+zip", "application/vnd.comicbook-rar", "application/x-cbr", "application/x-cbz");
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos infos)
        => Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            ["Pages"] = _pages.Count,
            ["Format"] = Path.GetExtension(infos?.FileName ?? string.Empty).TrimStart('.').ToUpperInvariant()
        });

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var changed = infos.SourceChanged(ref _loadedSource);
        await base.SetParametersAsync(parameters);
        if (!changed) return;
        await LoadAsync();
        await FileDisplayInfos.NotifyMetaChangedAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _errorMessage = null;
        ClearPage();
        try
        {
            await using var source = await FileService.ReadStreamAsync(FileDisplayInfos);
            var seekable = new MemoryStream();
            await source.CopyToAsync(seekable);
            seekable.Position = 0;
            var archive = await FileService.ReadArchiveAsync(seekable, FileDisplayInfos.FileName, FileDisplayInfos.ContentType, CancellationToken.None);
            _pages = archive.List.Where(p => !p.IsDirectory && ImageExtensions.Contains(Path.GetExtension(p.Name), StringComparer.OrdinalIgnoreCase))
                .OrderBy(p => p.Name, NaturalFileNameComparer.Instance).ToList();
            if (_pages.Count == 0)
                throw new InvalidDataException(TryLocalize("The comic archive contains no supported image pages."));
            await SelectPageAsync(0);
        }
        catch (Exception e)
        {
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private bool CanGoBack => _selectedIndex > 0;
    private bool CanGoForward => _selectedIndex >= 0 && _selectedIndex < _pages.Count - 1;
    private Task MoveAsync(int offset) => SelectPageAsync(_selectedIndex + offset);

    private async Task SelectPageAsync(int index)
    {
        if (index < 0 || index >= _pages.Count) return;
        ClearPage();
        _selectedIndex = index;
        var page = _pages[index];
        _pageStream = new MemoryStream(await page.GetBytesAsync());
        _currentPage = new ComicPageInfos(page.Name, page.ContentType, _pageStream);
        StateHasChanged();
    }

    private void ClearPage()
    {
        _currentPage = null;
        _pageStream?.Dispose();
        _pageStream = null;
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        ClearPage();
        await base.DisposeAsync();
    }

    private sealed record ComicPageInfos(string FileName, string ContentType, Stream ContentStream) : IMudExFileDisplayInfos
    {
        public string Url => null;
    }

    private sealed class NaturalFileNameComparer : IComparer<string>
    {
        public static readonly NaturalFileNameComparer Instance = new();
        public int Compare(string x, string y)
        {
            var a = System.Text.RegularExpressions.Regex.Split(x ?? string.Empty, "([0-9]+)");
            var b = System.Text.RegularExpressions.Regex.Split(y ?? string.Empty, "([0-9]+)");
            for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
            {
                var comparison = long.TryParse(a[i], out var ai) && long.TryParse(b[i], out var bi)
                    ? ai.CompareTo(bi)
                    : StringComparer.OrdinalIgnoreCase.Compare(a[i], b[i]);
                if (comparison != 0) return comparison;
            }
            return a.Length.CompareTo(b.Length);
        }
    }
}
