using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideEditor
{
    private EventCallback<KeyboardEventArgs>? _rootKeyDown;
    private EventCallback<int>? _showSlideChanged;
    private bool _showing;
    private bool _showStarting;
    private bool _previewing;
    private bool _presenterView;
    private int _showStart;
    private int _showSlide = -1;
    private int _previewRun;

    private EventCallback<KeyboardEventArgs> RootKeyDown =>
        _rootKeyDown ??= new EventCallback<KeyboardEventArgs>(WithoutRender.Instance, (Func<KeyboardEventArgs, Task>)OnRootKeyDownAsync);

    private EventCallback<int> ShowSlideChanged =>
        _showSlideChanged ??= new EventCallback<int>(WithoutRender.Instance, (Func<int, Task>)OnShowSlideChangedAsync);

    private bool CanShow => _presentation?.Slides.Count > 0;

    private async Task OnRootKeyDownAsync(KeyboardEventArgs e)
    {
        if (_showing || _showStarting)
            return;
        if (e.Key == "F5" && !e.CtrlKey && !e.AltKey && !e.MetaKey)
        {
            await StartShowAsync(e.ShiftKey);
            StateHasChanged();
            return;
        }

        await OnKeyDownAsync(e);
    }

    private async Task StartShowAsync(bool fromCurrent)
    {
        if (!CanShow || _showing || _showStarting)
            return;
        _showStarting = true;
        try
        {
            var start = fromCurrent ? _currentIndex : 0;
            await FlushPendingEditsAsync();
            await RegisterImagesAsync(ShowSlides(start));
            (_showStart, _showSlide, _previewing, _showing) = (start, -1, false, true);
        }
        finally
        {
            _showStarting = false;
        }
    }

    private Task OnShowSlideChangedAsync(int index)
    {
        _showSlide = index;
        return RegisterImagesAsync(ShowSlides(index));
    }

    private IEnumerable<Slide> ShowSlides(int index) => _presentation.Slides.Skip(index).Where(s => !s.Hidden).Take(2);

    private void OnShowClosed()
    {
        _showing = false;
        if (_showSlide >= 0 && _showSlide < _presentation.Slides.Count)
            Show(_presentation.Slides[_showSlide]);
    }

    private async Task PreviewAsync()
    {
        if (_current == null || _showing)
            return;
        await FlushPendingEditsAsync();
        _previewRun++;
        _previewing = true;
    }
}
