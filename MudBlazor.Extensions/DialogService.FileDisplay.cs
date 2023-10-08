using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Blazor.Extensions;
using Nextended.Core;

namespace MudBlazor.Extensions;

/// <summary>
/// Contains extensions for the IDialogService for displaying a file in different formats.
/// </summary>
public static partial class DialogServiceExt
{
    /// <summary>
    /// Shows a dialog which displays a file at the specified url.
    /// </summary>
    public static async Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, string url, DialogOptionsEx options = null, DialogParameters dialogParameters = null)
    {
        var fileName = Path.GetFileName(url);
        var contentType = MimeType.GetMimeType(fileName);
        var parameters = new DialogParameters
        {
            {nameof(MudExFileDisplayDialog.Icon), BrowserFileExt.IconForFile(contentType)},
            {nameof(MudExFileDisplayDialog.Url), url},
            {nameof(MudExFileDisplayDialog.ContentType), contentType}
        };

        return await dialogService.ShowEx<MudExFileDisplayDialog>(fileName, dialogParameters.MergeWith(parameters), options ?? DialogOptionsEx.FileDisplayDialogOptions);
    }


    /// <summary>
    /// Shows a dialog which displays a file at the specified url.
    /// </summary>
    public static async Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, string url, string contentType, DialogOptionsEx options, DialogParameters dialogParameters = null)
    {
        var fileName = Path.GetFileName(url);
        var parameters = new DialogParameters
        {
            {nameof(MudExFileDisplayDialog.Icon), BrowserFileExt.IconForFile(contentType)},
            {nameof(MudExFileDisplayDialog.Url), url},
            {nameof(MudExFileDisplayDialog.ContentType), contentType}
        };

        return await dialogService.ShowEx<MudExFileDisplayDialog>(fileName, dialogParameters.MergeWith(parameters), options ?? DialogOptionsEx.FileDisplayDialogOptions);
    }

    /// <summary>
    /// Shows a dialog which displays a file at the specified url.
    /// </summary>
    public static async Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, string url, string fileName, ContentType contentType, DialogOptionsEx options, DialogParameters dialogParameters = null)
    {
        var mime = contentType.MediaType;
        var parameters = new DialogParameters
        {
            {nameof(MudExFileDisplayDialog.Icon), BrowserFileExt.IconForFile(mime)},
            {nameof(MudExFileDisplayDialog.Url), url},
            {nameof(MudExFileDisplayDialog.ContentType), mime}
        };

        return await dialogService.ShowEx<MudExFileDisplayDialog>(fileName, dialogParameters.MergeWith(parameters), options ?? DialogOptionsEx.FileDisplayDialogOptions);
    }

    /// <summary>
    /// Shows a dialog which displays a file at the specified url.
    /// </summary>
    /// <param name="dialogService">The dialog service instance.</param>
    /// <param name="url">The url of the file.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="handleContentErrorFunc">A function that is called if an error occurs while handling the file's content.</param>
    /// <param name="options">Dialog options for the displayed file.</param>
    /// <returns>An awaitable task with the dialog reference.</returns>
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, string url, string fileName, string contentType, Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> handleContentErrorFunc, Action<DialogOptionsEx> options = null)
        => dialogService.ShowFileDisplayDialog(url, fileName, contentType, options, new DialogParameters { { nameof(MudExFileDisplay.HandleContentErrorFunc), handleContentErrorFunc } });

    /// <summary>
    /// Shows a dialog which displays a browser file.
    /// </summary>
    /// <param name="dialogService">The dialog service instance.</param>
    /// <param name="browserFile">The browser file to display.</param>
    /// <param name="handleContentErrorFunc">A function that is called if an error occurs while handling the file's content.</param>
    /// <param name="options">Dialog options for the displayed file.</param>
    /// <returns>An awaitable task with the dialog reference.</returns>
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, IBrowserFile browserFile, Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> handleContentErrorFunc, Action<DialogOptionsEx> options = null)
        => dialogService.ShowFileDisplayDialog(browserFile, options, new DialogParameters { { nameof(MudExFileDisplay.HandleContentErrorFunc), handleContentErrorFunc } });

    /// <summary>
    /// Shows a dialog which displays a file from a given stream.
    /// </summary>
    /// <param name="dialogService">The dialog service instance.</param>
    /// <param name="stream">The stream containing the file.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="handleContentErrorFunc">A function that is called if an error occurs while handling the file's content.</param>
    /// <param name="options">Dialog options for the displayed file.</param>
    /// <returns>An awaitable task with the dialog reference.</returns>
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, Stream stream, string fileName, string contentType, Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> handleContentErrorFunc, Action<DialogOptionsEx> options = null, DialogParameters parameters = null)
    {
        parameters ??= new DialogParameters();
        parameters.Add(nameof(MudExFileDisplay.HandleContentErrorFunc), handleContentErrorFunc);
        return dialogService.ShowFileDisplayDialog(stream, fileName, contentType, options, parameters);
    }

    /// <summary>
    /// Shows a dialog which displays a file at the specified url.
    /// </summary>
    /// <param name="dialogService">The dialog service instance.</param>
    /// <param name="url">The url of the file.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="options">Dialog options for the displayed file.</param>
    /// <param name="dialogParameters">Parameters to pass to the dialog.</param>
    /// <returns>An awaitable task with the dialog reference.</returns>
    public static async Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, string url, string fileName, string contentType, Action<DialogOptionsEx> options = null, DialogParameters dialogParameters = null)
    {
        var parameters = new DialogParameters
        {
            {nameof(MudExFileDisplayDialog.Icon), BrowserFileExt.IconForFile(contentType)},
            {nameof(MudExFileDisplayDialog.Url), url},
            {nameof(MudExFileDisplayDialog.ContentType), contentType}
        };

        return await dialogService.ShowFileDisplayDialog(fileName, dialogParameters.MergeWith(parameters), options);
    }

    /// <summary>
    /// Shows a dialog which displays a browser file.
    /// </summary>
    /// <param name="dialogService">The dialog service instance.</param>
    /// <param name="browserFile">The browser file to display.</param>
    /// <param name="options">Dialog options for the displayed file.</param>
    /// <param name="dialogParameters">Parameters to pass to the dialog.</param>
    /// <returns>An awaitable task with the dialog reference.</returns>
    public static async Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, IBrowserFile browserFile, Action<DialogOptionsEx> options = null, DialogParameters dialogParameters = null)
    {
        using var ms = new MemoryStream(await browserFile.GetBytesAsync());
        return await dialogService.ShowFileDisplayDialog(ms, browserFile.Name, browserFile.ContentType, options, dialogParameters);
    }

    /// <summary>
    /// Shows a dialog which displays a file from a given stream.
    /// </summary>
    /// <param name="dialogService">The dialog service instance.</param>
    /// <param name="stream">The stream containing the file.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="options">Dialog options for the displayed file.</param>
    /// <param name="dialogParameters">Parameters to pass to the dialog.</param>
    /// <returns>An awaitable task with the dialog reference.</returns>
    public static async Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, Stream stream, string fileName, string contentType, Action<DialogOptionsEx> options = null, DialogParameters dialogParameters = null)
    {
        var parameters = new DialogParameters
        {
            {nameof(MudExFileDisplayDialog.Icon), BrowserFileExt.IconForFile(contentType)},
            {nameof(MudExFileDisplayDialog.ContentStream), stream},
            {nameof(MudExFileDisplayDialog.ContentType), contentType}
        };

        return await dialogService.ShowFileDisplayDialog(fileName, dialogParameters.MergeWith(parameters), options);
    }

    private static async Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowFileDisplayDialog(this IDialogService dialogService, string fileName, DialogParameters parameters, Action<DialogOptionsEx> options = null)
    {
        var optionsEx = DialogOptionsEx.FileDisplayDialogOptions;
        options?.Invoke(optionsEx);

        return await dialogService.ShowEx<MudExFileDisplayDialog>(fileName, parameters, optionsEx);
    }
}