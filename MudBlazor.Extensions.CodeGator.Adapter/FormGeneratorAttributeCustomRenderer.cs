using System.Reflection;
using CG.Blazor.Forms.Attributes;
using CG.Blazor.Forms.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor.Extensions.Api;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.CodeGator.Adapter;

public class FormGeneratorAttributeCustomRenderer : ICustomRenderer
{
    private readonly FormGeneratorAttribute _attribute;

    public FormGeneratorAttributeCustomRenderer(FormGeneratorAttribute attribute)
    {
        _attribute = attribute;
    }

    public void Render(RenderTreeBuilder builder, IHandleEvent eventTarget, ObjectEditPropertyMeta propertyMeta)
    {
        Stack<object> path = new Stack<object>();
        path.Push(propertyMeta.ReferenceHolder);
        try
        {
            propertyMeta.Value ??= ApiMemberInfo.DefaultValue(propertyMeta.PropertyInfo) ?? propertyMeta.PropertyInfo.PropertyType.CreateInstance();
        }
        catch { /* Ignored */ }
        if(propertyMeta.Value == null && propertyMeta.PropertyInfo.PropertyType == typeof(string))
            propertyMeta.Value = string.Empty;
        path.Push(propertyMeta.Value);
        _attribute.Generate(builder, 0, eventTarget, path, propertyMeta.PropertyInfo, new Logger<IFormGenerator>(NullLoggerFactory.Instance));
    }
}


public class CGBlazorFormsRenderDataProvider : IDefaultRenderDataProvider
{
    public IRenderData GetRenderData(ObjectEditPropertyMeta propertyMeta)
    {
        var attribute = propertyMeta?.PropertyInfo?.GetCustomAttribute<FormGeneratorAttribute>();
        return attribute != null ? RenderData.For(new FormGeneratorAttributeCustomRenderer(attribute)) : null;
    }
}

public static class MudExObjectEditCGFormsAdapterExtensions
{
    public static ObjectEditPropertyMeta RenderWith(this ObjectEditPropertyMeta meta, FormGeneratorAttribute attribute)
        => meta.RenderWith(new FormGeneratorAttributeCustomRenderer(attribute));

    public static IServiceCollection AddMudExObjectEditCGBlazorFormsAdapter(this IServiceCollection services)
    {
        return services.AddSingleton<IDefaultRenderDataProvider, CGBlazorFormsRenderDataProvider>();
    }
}
