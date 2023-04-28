using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;

namespace MainSample.WebAssembly.Shared;

public partial class RenderComponent<T>
{
    private DynamicComponent? dynamicReference;
    private bool rendered;

    public T? Component => (T?)dynamicReference?.Instance;

    [Parameter] public string[] HiddenProperties { get; set; }

    [Parameter] public Action<ObjectEditMeta<T>> MetaConfiguration { get; set; }

    [Parameter] public RenderFragment<T>? ChildContent { get; set; }
    [Parameter] public RenderFragment? Left { get; set; }
    [Parameter] public RenderFragment? Right { get; set; }
    
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (!rendered)
        {
            rendered = true;
            StateHasChanged();
        }
    }

    private Action<ObjectEditMeta<T>> Configure()
    {
        return meta =>
        {
            meta.GroupByCategoryAttribute();
            meta.Properties(HiddenProperties).Ignore();
            MetaConfiguration?.Invoke(meta);
        };
    }

    bool IsGeneric => typeof(T).IsGenericType;

    private Type GetRenderType()
    {
        var res = typeof(T);
        if (res.IsGenericType && res.GetGenericTypeDefinition() == typeof(Nullable<>))
            return res.MakeGenericType(typeof(object));
        return res;
    }
}