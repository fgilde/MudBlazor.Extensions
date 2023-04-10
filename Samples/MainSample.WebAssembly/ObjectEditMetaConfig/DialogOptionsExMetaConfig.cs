using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Options;
using Nextended.Core.Extensions;

namespace MainSample.WebAssembly.ObjectEditMetaConfig;

public class DialogOptionsExMetaConfig : IObjectMetaConfiguration<DialogOptionsEx>
{
    public Task ConfigureAsync(ObjectEditMeta<DialogOptionsEx> meta)
    {
        meta.Properties(o => o.AnimationDuration,
            o => o.Buttons,
            o => o.AnimationTimingFunction,
            o => o.Animation,
            o => o.AnimationStyle,
            o => o.JsRuntime,
            o => o.CursorPositionOrigin,
            o => o.CursorPositionOriginName,
            o => o.AnimationStyle).Ignore();
        meta.Properties(
            o => o.Animations
        ).WrapInMudItem(i => i.xs = 12);
        meta.Properties(o => o.DragMode, o => o.Position, o => o.MaxWidth, o => o.AnimationDurationInMs).WithOrder(0).WithGroup("Options");
        meta.Properties().Where(p => p.PropertyInfo.PropertyType == typeof(bool) || p.PropertyInfo.PropertyType.IsNullableBool()).WithGroup("Options").WrapInMudItem(i =>
        {
            i.xl = 6;
            i.lg = 6;
            i.xs = 12;
        });
        meta.WrapEachInMudItem(i =>
        {
            //i.xl = 6;
            //i.xs = 12;
            i.xl = 6;
            i.lg = 6;
            i.xs = 12;
        });
        return Task.CompletedTask;
    }
}