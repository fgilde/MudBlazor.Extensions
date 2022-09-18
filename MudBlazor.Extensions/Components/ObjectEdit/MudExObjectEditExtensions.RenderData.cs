using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public static partial class MudExObjectEditExtensions
{
    public static IRenderData WrapIn<TWrapperComponent>(this IRenderData renderData, params Action<TWrapperComponent>[] options) where TWrapperComponent : new()
    {
        if (renderData != null)
            renderData.Wrapper = RenderData.For(typeof(TWrapperComponent), DictionaryHelper.GetValuesDictionary(true, options));
        return renderData?.Wrapper;
    }

    public static IRenderData WrapIn(this IRenderData renderData, IRenderData wrappingRenderData)
    {
        if(renderData != null)
            renderData.Wrapper = wrappingRenderData;
        return wrappingRenderData;
    }

    public static IRenderData AddComponentAfter<TComponent>(this IRenderData renderData, params Action<TComponent>[] options) where TComponent : new()
        => renderData.WithAdditionalComponent(RenderData.For(typeof(TComponent), DictionaryHelper.GetValuesDictionary(true, options)), true);
    public static IRenderData AddComponentBefore<TComponent>(this IRenderData renderData, params Action<TComponent>[] options) where TComponent : new()
        => renderData.WithAdditionalComponent(RenderData.For(typeof(TComponent), DictionaryHelper.GetValuesDictionary(true, options)), false);
    public static IRenderData AddComponentAfter(this IRenderData renderData, IRenderData afterRenderData) 
        => renderData.WithAdditionalComponent(afterRenderData, true);
    public static IRenderData AddComponentBefore(this IRenderData renderData, IRenderData afterRenderData)
        => renderData.WithAdditionalComponent(afterRenderData, false);

    public static IRenderData WithAdditionalComponent(this IRenderData renderData, IRenderData additionalRenderData, bool renderAfter)
    {
        if (renderData != null)
            (renderAfter ? renderData.RenderDataAfterComponent : renderData.RenderDataBeforeComponent).Add(additionalRenderData);
        return additionalRenderData;
    }

    public static IRenderData WrapInMudItem(this IRenderData renderData, params Action<MudItem>[] options)
        => renderData?.WrapIn(options);
    public static IRenderData WithoutValueBinding(this IRenderData renderData)
        => renderData?.SetProperties(r => r.DisableValueBinding = true);
}