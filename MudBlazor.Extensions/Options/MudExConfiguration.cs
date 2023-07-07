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
