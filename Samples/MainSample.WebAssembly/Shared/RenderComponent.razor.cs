using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Blazor.Helper;

namespace MainSample.WebAssembly.Shared;

public partial class RenderComponent<T>
{
    private DynamicComponent? dynamicReference;
    private bool rendered;

    [Inject] IDialogService dialogService { get; set; }
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
            Task.Delay(500).ContinueWith(_ => InvokeAsync(StateHasChanged));
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

    private IDictionary<string, object> GetParams()
    {
        var parameters = new Dictionary<string, object>
            {{"ChildContent", DynamicChildContent()}};
        return parameters
            .Where(p => ComponentRenderHelper.IsValidProperty(GetRenderType(), p.Key, p.Value))
            .ToDictionary(p => p.Key, p => p.Value);
    }

    private static RenderFragment DynamicChildContent()
    {
        return builder =>
        {
            var idx = 0;
            builder.OpenElement(idx++, "p");
            builder.AddContent(idx, "This is a dynamically created content");
            builder.CloseElement();
        };
    }
}