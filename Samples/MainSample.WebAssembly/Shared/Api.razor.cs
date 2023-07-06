using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

namespace MainSample.WebAssembly.Shared;

public partial class Api
{
    private BindingFlags _flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
    private bool _loaded;

    [Parameter]
    public Type Type { get; set; }

    [Parameter] public bool ShowHeader { get; set; } = true;

    private IEnumerable<ApiMemberInfo<PropertyInfo>>? Properties;
    private IEnumerable<ApiMemberInfo<MethodInfo>>? Methods;
    private Lazy<List<BreadcrumbItem>> _inheritance => new(GetInheritancePath(Type));

    private List<BreadcrumbItem> GetInheritancePath(Type type)
    {
        var breadcrumbItems = new List<BreadcrumbItem>();
        while (type != null)
        {
            breadcrumbItems.Insert(0, new BreadcrumbItem(GetTypeName(type), href: $"/a/{type.Name}", Type == type));
            type = type.BaseType;
        }
        breadcrumbItems.Reverse();
        return breadcrumbItems;
    }


    private string DefaultValue(PropertyInfo info)
    {
        try
        {
            var instance = Activator.CreateInstance(Type);
            var res = info.GetValue(instance);
            return res?.ToString() ?? "null";
        }
        catch (Exception)
        {
            return "Unknown";
        }
    }

    internal static string? GetTypeName(Type? type)
    {
        if (type == null)
            return null;
        if (!type.IsGenericType)
            return type.Name;
        var genericTypeName = type.GetGenericTypeDefinition().Name;
        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetTypeName).ToArray());

        return $"{genericTypeName[..genericTypeName.IndexOf('`')]}<{genericArgs}>";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && IsInitiallyExpanded)
        {
            await LoadInfos();
        }
        await base.OnAfterRenderAsync(firstRender);    
    }


    private async Task OnExpandedChanged(bool arg)
    {
        if (arg && (Properties == null || Methods == null))
        {
            await LoadInfos();
        }
    }

    private async Task LoadInfos()
    {
        _loaded = false;
        Properties ??= Type.GetProperties(_flags).OrderBy(x => x.Name).Where(i => char.IsUpper(i.Name[0])).Select(i => new ApiMemberInfo<PropertyInfo>(i, Type));
        Methods ??= Type.GetMethods(_flags).OrderBy(x => x.Name).Where(i => char.IsUpper(i.Name[0])).Select(i => new ApiMemberInfo<MethodInfo>(i, Type));
        await Task.WhenAll(Properties.Select(x => x.LoadTask).Concat(Methods.Select(x => x.LoadTask))).ContinueWith(_ => StateHasChanged());
        _loaded = true;
    }
}