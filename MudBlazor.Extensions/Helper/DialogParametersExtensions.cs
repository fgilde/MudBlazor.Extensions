using MudBlazor.Extensions.Helper.Internal;
using System.Reflection;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Static extensions for DialogParameters and relevant types.
/// </summary>
public static class DialogParametersExtensions
{
    /// <summary>
    /// Merges the dialog parameters.
    /// </summary>
    /// <param name="dialogParameters">The dialog parameters.</param>
    /// <param name="parameters">The parameters.</param>
    /// <returns>The merged dialog parameters.</returns>        
    public static DialogParameters MergeWith(this DialogParameters dialogParameters, DialogParameters parameters)
    {
        if (dialogParameters != null)
        {
            foreach (var param in dialogParameters)
                parameters.Add(param.Key, param.Value);
        }

        return parameters;
    }

    /// <summary>
    /// Converts parameters to dialog parameters.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns>The dialog parameters.</returns>        
    public static DialogParameters ToDialogParameters(this IEnumerable<KeyValuePair<string, object>> parameters)
    {
        var dialogParameters = new DialogParameters();
        foreach (var parameter in parameters)
            dialogParameters.Add(parameter.Key, parameter.Value);
        return dialogParameters;
    }

    /// <summary>
    /// Converts dialog parameters to a dictionary.
    /// </summary>
    /// <typeparam name="TDialog">The dialog type.</typeparam>
    /// <param name="dialogParameters">The dialog parameters.</param>
    /// <returns>The dictionary of dialog parameters.</returns>
    public static DialogParameters ConvertToDialogParameters<TDialog>(this Action<TDialog> dialogParameters) where TDialog : new()
        => PropertyHelper.ValidValuesDictionary(dialogParameters, true).Where(p => typeof(TDialog).GetProperty(p.Key, BindingFlags.Public | BindingFlags.Instance)?.CanWrite == true).ToDialogParameters();

    /// <summary>
    /// Converts dialog parameters to a dictionary.
    /// </summary>
    /// <typeparam name="TDialog">The dialog type.</typeparam>
    /// <param name="dialogParameters">The dialog parameters.</param>
    /// <returns>The dictionary of dialog parameters.</returns>
    public static DialogParameters ConvertToDialogParameters<TDialog>(this TDialog dialogParameters) where TDialog : new()
        => PropertyHelper.ValidValuesDictionary(dialogParameters, true).Where(p => typeof(TDialog).GetProperty(p.Key, BindingFlags.Public | BindingFlags.Instance)?.CanWrite == true).ToDialogParameters();
}