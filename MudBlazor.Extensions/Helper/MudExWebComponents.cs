using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Helper to enumerate all MudEx components that can be exposed as browser custom elements (web components).
/// </summary>
/// <remarks>
/// This class only computes the type/tag mapping. Registering the tags requires the
/// Microsoft.AspNetCore.Components.CustomElements package and happens in the hosting application.
/// </remarks>
public static class MudExWebComponents
{
    /// <summary>
    /// Prefix used for all generated custom element tag names.
    /// </summary>
    public const string TagPrefix = "mudex-";

    /// <summary>
    /// Returns all components of the given assembly (MudBlazor.Extensions by default) that can be registered
    /// as a custom element, together with the tag name they should be registered with.
    /// Ordered by tag name, duplicates removed.
    /// </summary>
    public static IReadOnlyList<(Type Type, string Tag)> GetRegistrableComponents(Assembly assembly = null)
    {
        assembly ??= typeof(MudExWebComponents).Assembly;
        return assembly.GetExportedTypes()
            .Where(IsRegistrable)
            .Select(type => (Type: type, Tag: TagName(type)))
            .GroupBy(x => x.Tag)
            .Select(g => g.OrderBy(x => x.Type.FullName, StringComparer.Ordinal).First())
            .OrderBy(x => x.Tag, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Returns all components that can not be registered as custom element, with the reason why.
    /// Used for documentation purposes.
    /// </summary>
    public static IReadOnlyList<(Type Type, string Reason)> GetSkippedComponents(Assembly assembly = null)
    {
        assembly ??= typeof(MudExWebComponents).Assembly;
        return assembly.GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IComponent).IsAssignableFrom(t) && !IsRegistrable(t))
            .Select(t => (Type: t, Reason: SkipReason(t)))
            .OrderBy(x => x.Type.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Builds the custom element tag name for a component type. MudExFileDisplay becomes mudex-file-display.
    /// </summary>
    public static string TagName(Type type)
    {
        var name = type.Name.Split('`')[0];
        if (name.StartsWith("MudEx", StringComparison.Ordinal))
            name = name[5..];
        else if (name.StartsWith("Mud", StringComparison.Ordinal))
            name = name[3..];
        var kebab = ToKebabCase(name);
        return string.IsNullOrEmpty(kebab) ? TagPrefix.TrimEnd('-') + "-component" : TagPrefix + kebab;
    }

    /// <summary>
    /// Sets the base url all MudEx javascript and css assets are resolved against.
    /// Only needed when MudEx is hosted on a different origin than the page using it (CDN / web components).
    /// Pass null or empty to restore the default behaviour (relative to the current document).
    /// </summary>
    /// <param name="baseUrl">Base url of the hosted MudEx assets, e.g. https://www.mudex.org/wc/ </param>
    public static void SetAssetBase(string baseUrl)
    {
        JsImportHelper.BasePath = string.IsNullOrWhiteSpace(baseUrl)
            ? "./_content/"
            : baseUrl.TrimEnd('/') + "/_content/";
    }

    private static bool IsRegistrable(Type type)
        => type.IsClass
           && type.IsPublic
           && !type.IsAbstract
           && !type.IsGenericTypeDefinition
           && typeof(IComponent).IsAssignableFrom(type)
           && type.GetConstructor(Type.EmptyTypes) != null
           && type.GetCustomAttribute<ObsoleteAttribute>() == null;

    private static string SkipReason(Type type)
        => type.IsGenericTypeDefinition ? "generic component"
            : type.GetConstructor(Type.EmptyTypes) == null ? "no parameterless constructor"
            : type.GetCustomAttribute<ObsoleteAttribute>() != null ? "obsolete"
            : "not public";

    private static string ToKebabCase(string value)
    {
        var sb = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                var previousIsLower = i > 0 && !char.IsUpper(value[i - 1]);
                var nextIsLower = i + 1 < value.Length && char.IsLower(value[i + 1]);
                if (i > 0 && (previousIsLower || nextIsLower))
                    sb.Append('-');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Trim('-');
    }
}
