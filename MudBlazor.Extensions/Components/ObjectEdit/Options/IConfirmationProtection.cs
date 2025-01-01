namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Interface for a confirmation protection instance, providing a set of properties and methods to configure the confirmation protection.
/// </summary>
public interface IConfirmationProtection
{
    /// <summary>
    /// Additional render data to be used for the confirmation protection.
    /// </summary>
    IRenderData AdditionalRenderData { get; }

    /// <summary>
    /// Callback to be invoked when the confirmation protection is triggered.
    /// </summary>
    Action<bool>? ConfirmationCallback { get; set; }
}