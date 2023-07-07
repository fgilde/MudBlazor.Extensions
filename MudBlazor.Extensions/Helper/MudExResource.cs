using Microsoft.Extensions.FileProviders;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Xml;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Static util class to access the internal code documentation
/// </summary>
public static class MudExResource
{
    private static readonly ConcurrentDictionary<Assembly, XmlDocument> xmlDocCache = new();

    /// <summary>
    /// Returns the version of used MudBlazor package
    /// </summary>
    /// <returns></returns>
    public static Version MudBlazorVersion() => typeof(MudButton).Assembly.GetName().Version;

    /// <summary>
    /// Returns the version of MudBlazor extensions
    /// </summary>
    /// <returns></returns>

    public static Version MudExVersion() => typeof(MudExResource).Assembly.GetName().Version;

    
    /// <summary>
    /// returns the Summary documentation text for a type
    /// </summary>
    public static async Task<string> GetSummaryDocumentationAsync(Type type) 
        => type == null ? null : await GetSummaryDocumentationAsync(type, $"T:{type.FullName}");

    /// <summary>
    /// returns a summary documentation toxt for given member
    /// </summary>
    public static async Task<string> GetSummaryDocumentationAsync(MemberInfo member)
    {
        if (member == null)
            return null;

        string prefixCode;
        string memberName;
        switch (member.MemberType)
        {
            case MemberTypes.Field:
                prefixCode = "F";
                memberName = $"{member.DeclaringType.FullName}.{member.Name}";
                break;
            case MemberTypes.Property:
                prefixCode = "P";
                memberName = $"{member.DeclaringType.FullName}.{member.Name}";
                break;
            case MemberTypes.Method:
                prefixCode = "M";
                var method = member as MethodInfo;
                if (method.GetParameters().Length > 0)
                {
                    var parameters = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
                    memberName = $"{member.DeclaringType.FullName}.{member.Name}({parameters})";
                }
                else
                {
                    memberName = $"{member.DeclaringType.FullName}.{member.Name}";
                }
                break;
            case MemberTypes.Event:
                prefixCode = "E";
                memberName = $"{member.DeclaringType.FullName}.{member.Name}";
                break;
            case MemberTypes.TypeInfo:
            case MemberTypes.NestedType:
                prefixCode = "T";
                memberName = member.DeclaringType.FullName;
                if (member is TypeInfo typeInfo && typeInfo.IsGenericType)
                {
                    memberName = $"{memberName}`{typeInfo.GenericTypeParameters.Length}";
                }
                break;
            default:
                throw new NotSupportedException();
        }

        return await GetSummaryDocumentationAsync(member.DeclaringType, $"{prefixCode}:{memberName}");
    }

    private static async Task<string> GetSummaryDocumentationAsync(Type type, string xpathQuery)
    {
        try
        {
            var xmlDoc = await LoadXmlDocAsync(type);
            var xpath = $"//member[@name='{xpathQuery}']/summary";
            var summaryNode = xmlDoc.SelectSingleNode(xpath);
            return summaryNode?.InnerText.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static async Task<XmlDocument> LoadXmlDocAsync(Type type)
    {
        var assembly = Assembly.GetAssembly(type);

        // Check if we already loaded and parsed this assembly's XML doc
        if (xmlDocCache.TryGetValue(assembly, out var xmlDoc))
            return xmlDoc;

        var resourceName = $"{assembly.GetName().Name}.xml";

        var embeddedProvider = new EmbeddedFileProvider(assembly);
        var fileInfo = embeddedProvider.GetFileInfo($"wwwroot/docs/{resourceName}");

        await using var resourceStream = fileInfo.CreateReadStream();

        xmlDoc = new XmlDocument();
        xmlDoc.Load(resourceStream);

        // Store the loaded and parsed XML doc in the cache
        xmlDocCache[assembly] = xmlDoc;

        return xmlDoc;
    }

    internal static async Task<string> GetEmbeddedFileContentAsync(string file, Assembly assembly = null)
    {
        var embeddedProvider = new EmbeddedFileProvider(assembly ?? Assembly.GetExecutingAssembly());
        var fileInfo = embeddedProvider.GetFileInfo(file);

        await using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
