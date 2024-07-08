using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Api;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Api view component to display the methods and properties of a type
/// </summary>
public partial class MudExApiView
{
    private bool _loaded;
    private string _searchString;
    private MudExpansionPanel _methodPanel;
    private MudExpansionPanel _propertyPanel;

    /// <summary>
    /// Link path to the api page where this component is used
    /// </summary>
    [Parameter] public string ApiLinkPath { get; set; } = "api";
    
    /// <summary>
    /// Type to display the api of
    /// </summary>
    [Parameter] public Type Type { get; set; }

    /// <summary>
    /// Show the header
    /// </summary>
    [Parameter] public bool ShowHeader { get; set; } = true;

    /// <summary>
    /// Show the inheritance path
    /// </summary>
    [Parameter] public bool ShowInheritance { get; set; } = true;
    
    /// <summary>
    /// Should the tools be shown
    /// </summary>
    [Parameter] public bool ShowTools { get; set; } = true;
    
    /// <summary>
    /// Show the methods
    /// </summary>
    [Parameter] public bool ShowMethods { get; set; } = true;
    
    /// <summary>
    /// Show the properties
    /// </summary>
    [Parameter] public bool ShowProperties { get; set; } = true;
    
    /// <summary>
    /// Show the inherited methods and properties
    /// </summary>
    [Parameter] public bool ShowInherited { get; set; }
    
    /// <summary>
    /// Should the view be compact
    /// </summary>
    [Parameter] public bool Compact { get; set; }
    
    /// <summary>
    /// Only show the specified methods and properties
    /// </summary>
    [Parameter] public string[] ShowOnly { get; set; }

    [Inject] IDialogService DialogService { get; set; }
    [Inject] IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// Is the panel initially expanded
    /// </summary>
    [Parameter]
    public bool Expanded { get; set; } = true;

    /// <summary>
    /// Search string
    /// </summary>
    [Parameter]
    public string Search { get; set; }

    private HashSet<ApiMemberInfo<PropertyInfo>> _properties;
    private HashSet<ApiMemberInfo<MethodInfo>> _methods;
    private Lazy<List<BreadcrumbItem>> Inheritance => new(GetInheritancePath(Type));

    private List<BreadcrumbItem> GetInheritancePath(Type type)
    {
        var breadcrumbItems = new List<BreadcrumbItem>();
        while (type != null)
        {
            breadcrumbItems.Insert(0, new BreadcrumbItem(ApiMemberInfo.GetGenericFriendlyTypeName(type) ?? string.Empty, href: $"/{ApiLinkPath.EnsureEndsWith("/")}{type.Name}", Type == type));
            type = type.BaseType;
        }
        breadcrumbItems.Reverse();
        return breadcrumbItems;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Expanded)
        {
            await LoadInfos();
        }

        if (firstRender && !string.IsNullOrEmpty(Search))
        {
            _searchString = Search;
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);    
    }


    private async Task OnExpandedChanged(bool arg)
    {
        if (arg && (_properties == null || _methods == null))
        {
            await LoadInfos();
        }
    }

    private async Task LoadInfos()
    {
        _loaded = false;
        _properties ??= await ApiMemberInfo<PropertyInfo>.AllPropertiesOf(Type);
        _methods ??= await ApiMemberInfo<MethodInfo>.AllMethodsOf(Type); 
        _loaded = true;
        await InvokeAsync(StateHasChanged);
    }

    private Task HandleLinkClicked((bool isLink, string text, string linkHref) segment)
    {
        var nav = ServiceProvider.GetService<NavigationManager>();
        if (nav != null)
        {
            nav.NavigateTo(segment.linkHref);
        }
        return Task.CompletedTask;
    }
}