using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Blazor.Extensions;
using Nextended.Core;

namespace MudBlazor.Extensions;

public static partial class DialogServiceExt
{
    public static Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, string url, string fileName, string contentType, Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> handleContentErrorFunc, Action<DialogOptionsEx> options = null)
        => dialogService.ShowFileDisplayDialog(url, fileName, contentType, options, new DialogParameters { { nameof(MudExFileDisplay.HandleContentErrorFunc), handleContentErrorFunc } });

    public static Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, IBrowserFile browserFile, Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> handleContentErrorFunc, Action<DialogOptionsEx> options = null)
        => dialogService.ShowFileDisplayDialog(browserFile, options, new DialogParameters { { nameof(MudExFileDisplay.HandleContentErrorFunc), handleContentErrorFunc } });

    public static Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, Stream stream, string fileName, string contentType, Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> handleContentErrorFunc, Action<DialogOptionsEx> options = null)
        => dialogService.ShowFileDisplayDialog(stream, fileName, contentType, options, new DialogParameters { { nameof(MudExFileDisplay.HandleContentErrorFunc), handleContentErrorFunc } });

    public static async Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, string url, string fileName, string contentType, Action<DialogOptionsEx> options = null, DialogParameters dialogParameters = null)
    {
        var parameters = new DialogParameters
        {
            {nameof(MudExFileDisplayDialog.Icon), BrowserFileExt.IconForFile(contentType)},
            {nameof(MudExFileDisplayDialog.Url), url},
            {nameof(MudExFileDisplayDialog.ContentType), contentType}
        };

        return await dialogService.ShowFileDisplayDialog(fileName, MergeParameters(dialogParameters, parameters), options);
    }

    public static async Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, IBrowserFile browserFile, Action<DialogOptionsEx> options = null, DialogParameters dialogParameters = null)
    {
        if (MimeType.IsZip(browserFile.ContentType))
        {
            var ms = new MemoryStream(await browserFile.GetBytesAsync());
            return await dialogService.ShowFileDisplayDialog(ms, browserFile.Name, browserFile.ContentType, options);
        }
        return await dialogService.ShowFileDisplayDialog(await browserFile.GetDataUrlAsync(), browserFile.Name, browserFile.ContentType, options, dialogParameters);
    }

    public static async Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, Stream stream, string fileName, string contentType, Action<DialogOptionsEx> options = null, DialogParameters dialogParameters = null)
    {
        var parameters = new DialogParameters
        {
            {nameof(MudExFileDisplayDialog.Icon), BrowserFileExt.IconForFile(contentType)},
            {nameof(MudExFileDisplayDialog.ContentStream), stream},
            {nameof(MudExFileDisplayDialog.ContentType), contentType}
        };

        return await dialogService.ShowFileDisplayDialog(fileName, MergeParameters(dialogParameters, parameters), options);
    }

    private static async Task<IDialogReference> ShowFileDisplayDialog(this IDialogService dialogService, string fileName, DialogParameters parameters, Action<DialogOptionsEx> options = null)
    {
        var optionsEx = new DialogOptionsEx
        {
            CloseButton = true,
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true,
            DisableBackdropClick = false,
            MaximizeButton = true,
            DragMode = MudDialogDragMode.Simple,
            Position = DialogPosition.BottomCenter,
            Animations = new[] { AnimationType.FadeIn, AnimationType.SlideIn },
            AnimationDuration = TimeSpan.FromSeconds(1),
            FullHeight = true,
            Resizeable = true
        };
        options?.Invoke(optionsEx);

        return await dialogService.ShowEx<MudExFileDisplayDialog>(fileName, parameters, optionsEx);
    }
}