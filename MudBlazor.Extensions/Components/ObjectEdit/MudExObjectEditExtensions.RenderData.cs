using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Helper.Internal;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public static partial class MudExObjectEditExtensions
{
    /// <summary>
    /// Wraps the current render data within a new component of type <typeparamref name="TWrapperComponent"/>, allowing for additional configuration through options.
    /// <typeparam name="TWrapperComponent">The component type to wrap around the current render data.</typeparam>
    /// <param name="renderData">The current render data to be wrapped.</param>
    /// <param name="options">A set of actions to configure the wrapper component.</param>
    /// <returns>The modified render data wrapped inside the specified component.</returns>
    /// </summary>
    public static IRenderData WrapIn<TWrapperComponent>(this IRenderData renderData, params Action<TWrapperComponent>[] options) where TWrapperComponent : new()
    {
        if (renderData != null)
            renderData.Wrapper = RenderData.For(typeof(TWrapperComponent), PropertyHelper.ValuesDictionary(true, options));
        return renderData?.Wrapper;
    }

    /// <summary>
    /// Wraps the current render data with another provided render data as its wrapper.
    /// <param name="renderData">The current render data to be wrapped.</param>
    /// <param name="wrappingRenderData">The render data to use as a wrapper.</param>
    /// <returns>The wrapper render data, now containing the original render data within it.</returns>
    /// </summary>
    public static IRenderData WrapIn(this IRenderData renderData, IRenderData wrappingRenderData)
    {
        if (renderData != null)
            renderData.Wrapper = wrappingRenderData;
        return wrappingRenderData;
    }

    /// <summary>
    /// Adds a component of type <typeparamref name="TComponent"/> after the current render data, allowing for additional configuration through options.
    /// <typeparam name="TComponent">The component type to add.</typeparam>
    /// <param name="renderData">The current render data before which the new component is to be added.</param>
    /// <param name="options">A set of actions to configure the added component.</param>
    /// <returns>The newly added component's render data.</returns>
    /// </summary>
    public static IRenderData AddComponentAfter<TComponent>(this IRenderData renderData, params Action<TComponent>[] options) where TComponent : new()
        => renderData.WithAdditionalComponent(RenderData.For(typeof(TComponent), PropertyHelper.ValuesDictionary(true, options)), true);

    /// <summary>
    /// Adds a component of type <typeparamref name="TComponent"/> before the current render data, allowing for additional configuration through options.
    /// <typeparam name="TComponent">The component type to add.</typeparam>
    /// <param name="renderData">The current render data after which the new component is to be added.</param>
    /// <param name="options">A set of actions to configure the added component.</param>
    /// <returns>The newly added component's render data.</returns>
    /// </summary>
    public static IRenderData AddComponentBefore<TComponent>(this IRenderData renderData, params Action<TComponent>[] options) where TComponent : new()
        => renderData.WithAdditionalComponent(RenderData.For(typeof(TComponent), PropertyHelper.ValuesDictionary(true, options)), false);

    /// <summary>
    /// Adds an existing render data component after the current render data.
    /// <param name="renderData">The current render data before which the new render data is to be added.</param>
    /// <param name="afterRenderData">The render data to add after the current one.</param>
    /// <returns>The added render data.</returns>
    /// </summary>
    public static IRenderData AddComponentAfter(this IRenderData renderData, IRenderData afterRenderData)
        => renderData.WithAdditionalComponent(afterRenderData, true);

    /// <summary>
    /// Adds an existing render data component before the current render data.
    /// <param name="renderData">The current render data after which the new render data is to be added.</param>
    /// <param name="afterRenderData">The render data to add before the current one.</param>
    /// <returns>The added render data.</returns>
    /// </summary>
    public static IRenderData AddComponentBefore(this IRenderData renderData, IRenderData afterRenderData)
        => renderData.WithAdditionalComponent(afterRenderData, false);

    /// <summary>
    /// Adds an additional component (either before or after) to the current render data.
    /// <param name="renderData">The current render data to which the additional component is to be added.</param>
    /// <param name="additionalRenderData">The additional render data to add.</param>
    /// <param name="renderAfter">Boolean indicating whether the additional component should be rendered after (true) or before (false) the current render data.</param>
    /// <returns>The added render data component.</returns>
    /// </summary>
    public static IRenderData WithAdditionalComponent(this IRenderData renderData, IRenderData additionalRenderData, bool renderAfter)
    {
        if (renderData != null)
            (renderAfter ? renderData.RenderDataAfterComponent : renderData.RenderDataBeforeComponent).Add(additionalRenderData);
        return additionalRenderData;
    }

    /// <summary>
    /// Wraps the current render data within a MudItem component, allowing for additional configuration through options.
    /// <param name="renderData">The current render data to be wrapped.</param>
    /// <param name="options">A set of actions to configure the MudItem wrapper component.</param>
    /// <returns>The modified render data wrapped inside a MudItem component.</returns>
    /// </summary>
    public static IRenderData WrapInMudItem(this IRenderData renderData, params Action<MudItem>[] options)
        => renderData?.WrapIn(options);

    /// <summary>
    /// Disables value binding for the current render data.
    /// <param name="renderData">The render data for which value binding should be disabled.</param>
    /// <returns>The render data with value binding disabled.</returns>
    /// </summary>
    public static IRenderData WithoutValueBinding(this IRenderData renderData)
        => renderData?.SetProperties(r => r.DisableValueBinding = true);
}
