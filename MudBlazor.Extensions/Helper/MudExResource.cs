using Microsoft.Extensions.FileProviders;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Static util class to access the internal code documentation
/// </summary>
public static class MudExResource
{
    private static readonly ConcurrentDictionary<Assembly, XmlDocument> XmlDocCache = new();
    private static readonly ConcurrentDictionary<string, string> EmbeddedFileContentCache = new();

    public static bool IsClientSide => RuntimeInformation.OSDescription == "Browser"; // WASM
    public static bool IsServerSide => !IsClientSide;

    public static bool IsDebug => Assembly.GetEntryAssembly()?.GetCustomAttribute<DebuggableAttribute>()?.IsJITTrackingEnabled ?? false;
    public static bool IsDebugOrPreviewBuild
    {
        get
        {
            #if DEBUG
                        return true;
            #else
                return false;
            #endif   
        }
    }

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
    public static async Task<string> GetSummaryDocumentationAsync(Type type, bool allowCache = true) 
        => type == null ? null : await GetSummaryDocumentationAsync(type, $"T:{type.FullName}", allowCache);

    /// <summary>
    /// returns a summary documentation text for given member
    /// </summary>
    public static async Task<string> GetSummaryDocumentationAsync(MemberInfo member, bool allowCache = true)
    {
        if (member == null)
            return null;

        string prefixCode;
        string memberName;
        var memberDeclaringType = member.DeclaringType;
        switch (member.MemberType)
        {
            case MemberTypes.Field:
                prefixCode = "F";
                memberName = $"{memberDeclaringType?.FullName}.{member.Name}";
                break;
            case MemberTypes.Property:
                prefixCode = "P";
                memberName = $"{memberDeclaringType?.FullName}.{member.Name}";
                break;
            case MemberTypes.Method:
                prefixCode = "M";
                var method = member as MethodInfo;
                if (method?.GetParameters().Length > 0)
                {
                    var parameters = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
                    memberName = $"{memberDeclaringType?.FullName}.{member.Name}({parameters})";
                }
                else
                {
                    memberName = $"{memberDeclaringType?.FullName}.{member.Name}";
                }
                break;
            case MemberTypes.Event:
                prefixCode = "E";
                memberName = $"{memberDeclaringType?.FullName}.{member.Name}";
                break;
            case MemberTypes.TypeInfo:
            case MemberTypes.NestedType:
                prefixCode = "T";
                memberName = memberDeclaringType?.FullName;
                if (member is TypeInfo {IsGenericType: true} typeInfo)
                {
                    memberName = $"{memberName}`{typeInfo.GenericTypeParameters.Length}";
                }
                break;
            default:
                throw new NotSupportedException();
        }

        return await GetSummaryDocumentationAsync(memberDeclaringType, $"{prefixCode}:{memberName}", allowCache);
    }

    private static async Task<string> GetSummaryDocumentationAsync(Type type, string xpathQuery, bool allowCache = true)
    {
        try
        {
            var xmlDoc = await LoadXmlDocAsync(type, allowCache);
            if (xmlDoc == null)
                return null;
            var xpath = $"//member[@name='{xpathQuery}']/summary";
            var summaryNode = xmlDoc.SelectSingleNode(xpath);
            return summaryNode?.InnerText.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static async Task<XmlDocument> LoadXmlDocAsync(Type type, bool allowCache = true)
    {
        var assembly = Assembly.GetAssembly(type);
        if (assembly == null)
            return null;

        // Check if we already loaded and parsed this assembly's XML doc
        if (allowCache && XmlDocCache.TryGetValue(assembly, out var xmlDoc))
            return xmlDoc;

        var resourceName = $"{assembly.GetName().Name}.xml";

        var embeddedProvider = new EmbeddedFileProvider(assembly);
        var fileInfo = embeddedProvider.GetFileInfo($"wwwroot/docs/{resourceName}");

        await using var resourceStream = fileInfo.CreateReadStream();

        xmlDoc = new XmlDocument();
        xmlDoc.Load(resourceStream);

        // Store the loaded and parsed XML doc in the cache
        XmlDocCache[assembly] = xmlDoc;

        return xmlDoc;
    }

    internal static async Task<string> GetEmbeddedFileContentAsync(string file, Assembly assembly = null)
    {
        if (EmbeddedFileContentCache.TryGetValue(file, out var content))
            return content;
        
        var embeddedProvider = new EmbeddedFileProvider(assembly ?? Assembly.GetExecutingAssembly());
        var fileInfo = embeddedProvider.GetFileInfo(file);

        await using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var result = await reader.ReadToEndAsync();
        EmbeddedFileContentCache[file] = result;
        return result;    
    }
}
