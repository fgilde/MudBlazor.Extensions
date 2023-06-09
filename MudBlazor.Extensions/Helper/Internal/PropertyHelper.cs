namespace MudBlazor.Extensions.Helper.Internal;

internal static class PropertyHelper
{
    public static bool IsPropertyPathSubPropertyOf(string propertyPath, string path)
    {
        if (string.IsNullOrWhiteSpace(propertyPath) || string.IsNullOrWhiteSpace(path))
            return false;
        var pathParts = propertyPath.Split('.');
        var subPathParts = path.Split('.');
        return pathParts.Length >= subPathParts.Length && !subPathParts.Where((t, i) => pathParts[i] != t).Any();
    }
}