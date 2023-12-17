using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Options;

/// <summary>
/// Represents the configuration options for MudBlazor Extended.
/// </summary>
public class MudExConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MudExConfiguration"/> class.
    /// </summary>
    internal MudExConfiguration()
    { }

    /// <summary>
    /// Set this to true to disable all automatic css loadings
    /// </summary>
    internal bool DisableAutomaticCssLoading { get; set; }

    /// <summary>
    /// Global google drive client id
    /// </summary>
    internal string GoogleDriveClientId { get; set; }

    /// <summary>
    /// Global one drive client id
    /// </summary>
    internal string OneDriveClientId { get; set; }

    /// <summary>
    /// Global one drive client id
    /// </summary>
    internal string DropBoxApiKey { get; set; }

    /// <summary>
    /// Enable global drop box integration
    /// </summary>    
    public MudExConfiguration EnableDropBoxIntegration(string dropBoxApiKey) => With(c => c.DropBoxApiKey = dropBoxApiKey);

    /// <summary>
    /// Enable global google drive integration
    /// </summary>    
    public MudExConfiguration EnableGoogleDriveIntegration(string googleDriveClientId) => With(c => c.GoogleDriveClientId = googleDriveClientId);

    /// <summary>
    /// Enable global google drive integration
    /// </summary>    
    public MudExConfiguration EnableOneDriveIntegration(string oneDriveClientId) => With(c => c.OneDriveClientId = oneDriveClientId);

    /// <summary>
    /// Disables automatic Css loading. Then you need to ensure you have added the mud-ex styles in your index.html or _Host.cshtml
    /// </summary>    
    public MudExConfiguration WithoutAutomaticCssLoading() => With(c => c.DisableAutomaticCssLoading = true);

    /// <summary>
    /// Enables automatic Css loading.
    /// </summary>    
    public MudExConfiguration WithAutomaticCssLoading() => With(c => c.DisableAutomaticCssLoading = false);

    /// <summary>
    /// Sets a specific base path for static js files
    /// </summary>    
    public MudExConfiguration WithJsBasePath(string basePath)
    {
        JsImportHelper.BasePath = basePath;
        return this;
    }

    /// <summary>
    /// Set specific options
    /// </summary>
    internal MudExConfiguration With(params Action<MudExConfiguration>[] actions) => this.SetProperties(actions);

    /// <summary>
    /// Configures the default dialog options using the provided action.
    /// </summary>
    /// <param name="options">The action to configure the default dialog options.</param>
    /// <returns>The current configuration instance.</returns>
    public MudExConfiguration WithDefaultDialogOptions(Action<DialogOptionsEx> options)
    {
        options?.Invoke(DialogOptionsEx.DefaultDialogOptions);
        return this;
    }

    /// <summary>
    /// Sets the provided dialog options as the default dialog options.
    /// </summary>
    /// <param name="options">The options to set as the default dialog options.</param>
    /// <returns>The current configuration instance.</returns>
    public MudExConfiguration WithDefaultDialogOptions(DialogOptionsEx options)
    {
        options?.SetAsDefaultDialogOptions();
        return this;
    }
}
