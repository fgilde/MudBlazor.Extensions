﻿using MudBlazor.Extensions.Helper;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace MainSample.WebAssembly.Shared;

internal sealed class ApiMemberInfo<TMemberInfo> : IApiMemberInfo
    where TMemberInfo : MemberInfo
{
    public ApiMemberInfo(TMemberInfo memberInfo)
    {
        MemberInfo = memberInfo;
        (LoadTask = LoadDescription()).ContinueWith(_ => DescriptionLoaded = true);
    }

    public Task LoadTask { get; private set; }
    public bool DescriptionLoaded { get; private set; }
    public bool IsStatic => MemberInfo is PropertyInfo info && info.GetAccessors().Any(p => p.IsStatic);
    public TMemberInfo MemberInfo { get; }
    public string Name => MemberInfo is MethodInfo info ? MethodToString(info) : MemberInfo.Name;
    
    public string Description { get; set; }
    public string TypeName => GetTypeName(MemberInfo is PropertyInfo info ? info.PropertyType : (MemberInfo as MethodInfo)!.ReturnType);
    public string Default => DefaultValue();

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
            var instance = Activator.CreateInstance(info.DeclaringType);
            var res = info.GetValue(instance);
            return res?.ToString() ?? "null";
        }
        catch (Exception)
        {
            return "Unknown";
        }
    }

}

internal interface IApiMemberInfo
{
    string Name { get; }
    string Description { get; }
    string TypeName { get; }
    string Default { get; }
    bool IsStatic { get; }
}