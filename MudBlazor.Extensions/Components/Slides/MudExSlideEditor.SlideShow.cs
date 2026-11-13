using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideEditor
{
    private EventCallback<KeyboardEventArgs>? _rootKeyDown;
    private EventCallback<int>? _showSlideChanged;
    private bool _showing;
    private bool _previewing;
    private bool _presenterView;
    private int _showStart;
    private int _showSlide = -1;
    private int _previewRun;

    private EventCallback<KeyboardEventArgs> RootKeyDown =>
        _rootKeyDown ??= new EventCallback<KeyboardEventArgs>(WithoutRender.Instance, (Func<KeyboardEventArgs, Task>)OnRootKeyDownAsync);

    private EventCallback<int> ShowSlideChanged =>
        _showSlideChanged ??= new EventCallback<int>(WithoutRender.Instance, (Action<int>)(index => _showSlide = index));

    private bool CanShow => _presentation?.Slides.Count > 0;

    private async Task OnRootKeyDownAsync(KeyboardEventArgs e)
    {
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
        if (!CanShow || _showing)
            return;
        await FlushPendingEditsAsync();
        await RegisterImagesAsync(_presentation.Slides);
        (_showStart, _showSlide, _previewing, _showing) = (fromCurrent ? _currentIndex : 0, -1, false, true);
    }

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
