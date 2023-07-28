namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Extensions for dialog reference
/// </summary>
public static class DialogReferenceExtensions
{
    /// <summary>
    /// returns the Id for a rendered dialogReference 
    /// </summary>
    /// <param name="dialogReference"></param>
    /// <returns></returns>
    public static string GetDialogId(this IDialogReference dialogReference) 
        => dialogReference != null && dialogReference.Id != Guid.Empty ? $"_{dialogReference.Id.ToString().Replace("-", "")}" : null;
}