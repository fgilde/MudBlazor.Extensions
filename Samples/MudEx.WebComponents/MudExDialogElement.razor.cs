using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;

namespace MudEx.WebComponents;

/// <summary>
/// Backing component of &lt;mudex-dialog&gt;.
///
/// The library component MudExDialog needs a RenderFragment, which a custom element cannot provide.
/// This one takes the markup that stood between the tags as a string (mudex.js moves it into the
/// content attribute before the runtime starts) and opens it through the MudEx dialog service, so
/// the MudEx animations, drag modes and appearance options are available from plain html:
///
///   &lt;mudex-dialog title="Hello" animation="SlideIn" drag-mode="WithoutIds"&gt;
///       &lt;h3&gt;Any markup&lt;/h3&gt;
///   &lt;/mudex-dialog&gt;
///
///   document.querySelector('mudex-dialog').open = true;
/// </summary>
public partial class MudExDialogElement : ComponentBase
{
    private bool _isOpen;
    private IMudExDialogReference<HtmlDialog> _dialog;

    [Inject] private IDialogService DialogService { get; set; }

    /// <summary>
    /// Markup to show in the dialog. Filled from the content between the tags.
    /// </summary>
    [Parameter] public string Content { get; set; }

    /// <summary>
    /// Dialog title.
    /// </summary>
    [Parameter] public string Title { get; set; }

    /// <summary>
    /// Set to true to open the dialog, false to close it again.
    /// </summary>
    [Parameter]
    public bool Open
    {
        get => _isOpen;
        set
        {
            if (_isOpen == value)
                return;
            _isOpen = value;
            _ = _isOpen ? ShowAsync() : CloseAsync();
        }
    }

    // Enums count as complex types for custom elements and cannot be set as an attribute, so these
    // arrive as plain strings and are parsed here. That keeps <mudex-dialog animation="SlideIn">
    // working from html, which is the whole point of the element.

    /// <summary>
    /// One of the MudEx animation types, for example SlideIn, FadeIn, Scale, JackInTheBox.
    /// </summary>
    [Parameter] public string Animation { get; set; }

    /// <summary>
    /// How the dialog can be dragged: None, Simple or WithoutIds.
    /// </summary>
    [Parameter] public string DragMode { get; set; }

    /// <summary>
    /// Animation duration in milliseconds.
    /// </summary>
    [Parameter] public double AnimationDurationInMs { get; set; } = 500;

    /// <summary>
    /// Maximum width of the dialog: ExtraSmall, Small, Medium, Large, ExtraLarge or False.
    /// </summary>
    [Parameter] public string MaxWidth { get; set; }

    /// <summary>
    /// Where the dialog appears, for example Center, TopCenter or BottomCenter.
    /// </summary>
    [Parameter] public string Position { get; set; }

    /// <summary>
    /// Show the close button in the header.
    /// </summary>
    [Parameter] public bool CloseButton { get; set; } = true;

    /// <summary>
    /// Dialog takes the full width of its max width.
    /// </summary>
    [Parameter] public bool FullWidth { get; set; } = true;

    /// <summary>
    /// Can the dialog be resized by the user.
    /// </summary>
    [Parameter] public bool Resizeable { get; set; } = true;

    /// <summary>
    /// Raised when the dialog was closed, with true when it was confirmed.
    /// </summary>
    [Parameter] public EventCallback<bool> OnClosed { get; set; }

    private static TEnum Parse<TEnum>(string value, TEnum fallback) where TEnum : struct
        => Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;

    private DialogOptionsEx BuildOptions() => new()
    {
        Animations = [Parse(Animation, AnimationType.FadeIn)],
        AnimationDurationInMs = AnimationDurationInMs,
        DragMode = Parse(DragMode, MudDialogDragMode.Simple),
        MaxWidth = Parse(MaxWidth, MudBlazor.MaxWidth.Small),
        Position = Parse(Position, DialogPosition.Center),
        CloseButton = CloseButton,
        FullWidth = FullWidth,
        Resizeable = Resizeable,
        CloseOnEscapeKey = true
    };

    private async Task ShowAsync()
    {
        if (_dialog != null)
            return;

        var parameters = new DialogParameters<HtmlDialog> { { x => x.Html, Content } };
        _dialog = await DialogService.ShowExAsync<HtmlDialog>(Title ?? string.Empty, parameters, BuildOptions());

        var result = await _dialog.Result;
        _dialog = null;
        _isOpen = false;
        await OnClosed.InvokeAsync(result is { Canceled: false });
        StateHasChanged();
    }

    private Task CloseAsync()
    {
        if (_dialog == null)
            return Task.CompletedTask;
        var dialog = _dialog;
        _dialog = null;
        dialog.Close();
        return Task.CompletedTask;
    }
}
