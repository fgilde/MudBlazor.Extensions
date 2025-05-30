﻿using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Dialog to display a file using <see cref="MudExFileDisplay"/> component.
/// </summary>
public partial class MudExFileDisplayDialog
{
    
    /// <summary>
    /// The MudDialog instance
    /// </summary>
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; }
    
    /// <summary>
    /// CSS classes applied to the content of the dialog.
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public string ContentClass { get; set; } = "full-height";

    /// <summary>
    /// Icon to display in the dialog.
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public string Icon { get; set; }

    /// <summary>
    /// If true icons are colored
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public MudExColor DialogIconColor { get; set; } = Color.Error;

    /// <summary>
    /// Action buttons to display in the dialog.
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public MudExDialogResultAction[] Buttons { get; set; }


    /// <inheritdoc />
    protected override void OnParametersSet()
    {        
        base.OnParametersSet();
        FileName ??=MudDialog.Title;
    }
    
    /// <summary>
    /// Shows a file using the <see cref="MudExFileDisplay"/> component in a dialog.
    /// </summary>
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowAsync(IDialogService dialogService, string url, string fileName, string contentType, Action<DialogOptionsEx> options = null) => dialogService.ShowFileDisplayDialogAsync(url, fileName, contentType, options);
    
    /// <summary>
    /// Shows a file using the <see cref="MudExFileDisplay"/> component in a dialog.
    /// </summary>
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowAsync(IDialogService dialogService, IBrowserFile browserFile, Action<DialogOptionsEx> options = null) => dialogService.ShowFileDisplayDialogAsync(browserFile, options);

    /// <summary>
    /// Shows a file using the <see cref="MudExFileDisplay"/> component in a dialog.
    /// </summary>
    public static Task<IMudExDialogReference<MudExFileDisplayDialog>> ShowAsync(IDialogService dialogService, Stream stream, string fileName, string contentType, Action<DialogOptionsEx> options = null) => dialogService.ShowFileDisplayDialogAsync(stream, fileName, contentType, options);

    /// <summary>
    /// Submits the dialog.
    /// </summary>
    public void Submit(DialogResult result) => MudDialog.Close(result);
    
    /// <summary>
    /// Closes the dialog.
    /// </summary>
    public void Cancel() => MudDialog.Cancel();
}