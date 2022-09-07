using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Options;
using Nextended.Core.Extensions;

namespace SampleApplication.Client.ObjectEditMetaConfig;

public class DialogOptionsExMetaConfig : IObjectMetaConfiguration<DialogOptionsEx>
{
    public Task ConfigureAsync(ObjectEditMeta<DialogOptionsEx> meta)
    {
        meta.Properties(o => o.AnimationDuration,
            o => o.Buttons,
            o => o.Animation,
            o => o.AnimationTimingFunction,
            o => o.AnimationStyle,
            o => o.JsRuntime,
            o => o.AnimationStyle).Ignore();
        meta.Properties(
            o => o.Animations
        ).WrapInMudItem(i => i.xs = 12);
        meta.Properties(o => o.DragMode, o => o.Position, o => o.MaxWidth, o => o.AnimationDurationInMs).WithOrder(0).WithGroup("Options").WrapInMudItem(i => i.xs = 6);
        meta.Properties().Where(p => p.PropertyInfo.PropertyType == typeof(bool) || p.PropertyInfo.PropertyType.IsNullableBool()).WithGroup("Options").WrapInMudItem(i => i.xs = 3);
        meta.WrapEachInMudItem(i => i.xs = 6);
        return Task.CompletedTask;
    }
}