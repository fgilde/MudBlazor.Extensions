namespace MudBlazor.Extensions.Office;

internal static class LoadLimit
{
    private const string Key = "MudBlazor.Extensions.LoadLimit";

    internal static InvalidDataException Exceeded(string message) => new(message) { Data = { [Key] = true } };

    internal static bool IsExceeded(Exception exception) => exception.Data[Key] is true;
}
