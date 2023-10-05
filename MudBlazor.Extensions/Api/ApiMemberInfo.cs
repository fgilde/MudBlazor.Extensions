using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Api;

/// <summary>
/// Simple class that reads default values and Descriptions for a Member
/// </summary>
/// <typeparam name="TMemberInfo"></typeparam>
public sealed class ApiMemberInfo<TMemberInfo> : ApiMemberInfo, IApiMemberInfo
    where TMemberInfo : MemberInfo
{

    private readonly Type _initialType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiMemberInfo"/> class.
    /// </summary>
    /// <param name="memberInfo">The member information.</param>
    /// <param name="initialType">The initial type of the member.</param>
    public ApiMemberInfo(TMemberInfo memberInfo, Type initialType)
    {
        _initialType = initialType;
        MemberInfo = memberInfo;
        (LoadTask = LoadDescription()).ContinueWith(_ => DescriptionLoaded = true);
    }


    /// <summary>
    /// Gets a value indicating whether the member is inherited.
    /// </summary>
    public bool IsInherited => _initialType != MemberInfo.DeclaringType;

    /// <summary>
    /// Gets the task responsible for loading the description.
    /// </summary>
    public Task LoadTask { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the description has been loaded.
    /// </summary>
    public bool DescriptionLoaded { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the member is static.
    /// </summary>
    public bool IsStatic => (MemberInfo is PropertyInfo info && info.GetAccessors().Any(p => p.IsStatic)) || MemberInfo is MethodInfo { IsStatic: true };

    /// <summary>
    /// Gets the member information.
    /// </summary>
    public TMemberInfo MemberInfo { get; }

    /// <summary>
    /// Gets the name of the member.
    /// </summary>
    public string Name => MemberInfo is MethodInfo info ? MethodToString(info) : MemberInfo.Name;

    /// <summary>
    /// Gets or sets the description of the member.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets the type name of the member.
    /// </summary>
    public string TypeName => GetTypeName(MemberInfo is PropertyInfo info ? info.PropertyType : (MemberInfo as MethodInfo)!.ReturnType);

    /// <summary>
    /// Gets the default value of the member.
    /// </summary>
    public string Default => _default ??= DefaultValue();

    private string? _default;

    private async Task LoadDescription()
    {
        var attr = MemberInfo.GetCustomAttribute<DescriptionAttribute>();
        if (attr != null)
            Description = attr.Description;
        Description = await MudExResource.GetSummaryDocumentationAsync(MemberInfo);
    }

    private string GetTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;
        var genericTypeName = type.GetGenericTypeDefinition().Name;
        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetTypeName).ToArray());

        return $"{genericTypeName[..genericTypeName.IndexOf('`')]}<{genericArgs}>";
    }

    private string MethodToString(MethodInfo method)
    {

        var sb = new StringBuilder();

        // Getting the access modifier
        var accessModifier = method.IsPublic ? "public" :
            method.IsPrivate ? "private" :
            method.IsFamily ? "protected" : "internal";

        // Getting the return type
        var returnType = GetTypeName(method.ReturnType);

        // Getting the method name
        var methodName = method.Name;

        // Adding modifier, return type and method name to string builder
        sb.Append($"{accessModifier} {returnType} {methodName}(");

        // Getting the parameters
        var parameters = method.GetParameters();
        for (var i = 0; i < parameters.Length; i++)
        {
            // Adding parameter type and name to string builder
            sb.Append($"{GetTypeName(parameters[i].ParameterType)} {parameters[i].Name}");

            // If not the last parameter, add a comma and a space
            if (i < parameters.Length - 1)
                sb.Append(", ");

        }

        sb.Append(")");

        return sb.ToString();
    }
    
    private string DefaultValue()
    {
        var info = MemberInfo as PropertyInfo;
        if (info == null || info.DeclaringType == null)
            return string.Empty;
        try
        {
            if (info.PropertyType.Name.StartsWith("EventCallback"))
                return "default";
            var instance = CreateGenericTypeInstance(info.DeclaringType);
            var res = instance?.GetType()?.GetProperty(info.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField)?.GetValue(instance, null) ?? info.GetValue(instance, null);

            var sres = res?.ToString() ?? "null";
            if (sres.StartsWith("<"))
            {
                string name = MudExSvg.SvgPropertyNameForValue(sres);
                return name != null ? $"@{name}" : sres;
            }
            return sres;
        }
        catch (Exception e)
        {
            return "Unknown";
        }
    }

}

public class ApiMemberInfo
{
    public static object CreateGenericTypeInstance(Type type)
    {
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            var typeArgs = new Type[genericTypeDef.GetGenericArguments().Length];
            for (int i = 0; i < typeArgs.Length; i++)
            {
                typeArgs[i] = typeof(object);
            }
            var specificType = genericTypeDef.MakeGenericType(typeArgs);
            return Activator.CreateInstance(specificType);
        }

        return Activator.CreateInstance(type);
    }

    /// <summary>
    /// Returns a generic friendly type name by an full type name
    /// </summary>
    public static string GetGenericFriendlyTypeName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return fullName;

        // Wenn der Typ ein Array ist
        if (fullName.EndsWith("[]"))
            return GetGenericFriendlyTypeName(fullName.Substring(0, fullName.Length - 2)) + "[]";

        // Typ ist ein generischer Typ
        var genericMatch = new Regex(@"^(.*?)`\d+\[\[(.*)\]\]$").Match(fullName);
        if (genericMatch.Success)
        {
            var typeName = genericMatch.Groups[1].Value;
            var genericArguments = genericMatch.Groups[2].Value;

            var splitArgs = genericArguments.Split(new string[] { "],[" }, StringSplitOptions.None)
                .Select(s => s.Split(',')[0])
                .Select(GetGenericFriendlyTypeName)
                .ToArray();

            return $"{typeName}<{string.Join(", ", splitArgs)}>";
        }

        return fullName;
    }

    /// <summary>
    /// Returns a generic friendly type name
    /// </summary>
    public static string? GetGenericFriendlyTypeName(Type? type)
    {
        if (type == null)
            return null;
        if (!type.IsGenericType)
            return type.Name;
        var genericTypeName = type.GetGenericTypeDefinition().Name;
        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetGenericFriendlyTypeName).ToArray());

        return $"{genericTypeName[..genericTypeName.IndexOf('`')]}<{genericArgs}>";
    }
}

/// <summary>
/// Defines the properties required for API member information.
/// </summary>
public interface IApiMemberInfo
{
    /// <summary>
    /// Gets the name of the member.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the member.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the type name of the member.
    /// </summary>
    string TypeName { get; }

    /// <summary>
    /// Gets the default value of the member.
    /// </summary>
    string Default { get; }

    /// <summary>
    /// Gets a value indicating whether the member is static.
    /// </summary>
    bool IsStatic { get; }

    /// <summary>
    /// Gets a value indicating whether the member is inherited.
    /// </summary>
    bool IsInherited { get; }
}


