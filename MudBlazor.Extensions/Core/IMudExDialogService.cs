using Microsoft.JSInterop;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// Interface for a service that provides dialog functionality.
/// </summary>
public interface IMudExDialogService : IDialogService
{
    /// <summary>
    /// Gets the JavaScript runtime instance.
    /// </summary>
    public IJSRuntime JSRuntime { get; }
    
    /// <summary>
    /// Gets the service provider instance.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }
    
    /// <summary>
    /// Gets the appearance service instance.
    /// </summary>
    public MudExAppearanceService AppearanceService { get; }

    /// <summary>
    /// Returns a dialog reference for the dialog with the specified GUID.
    /// </summary>
    public IDialogReference GetDialogReference(Guid dialogGuid);

    /// <summary>
    /// Returns the dialog options for the dialog with the specified GUID.
    /// </summary>
    public DialogOptionsEx GetDialogUsedDialogOptions(Guid dialogGuid);

}