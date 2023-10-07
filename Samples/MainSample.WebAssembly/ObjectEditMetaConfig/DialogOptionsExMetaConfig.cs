using MudBlazor;
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
            o => o.CursorPositionOriginName,
            o => o.AnimationStyle).Ignore();
        meta.Properties(
            o => o.Animations
        ).WrapInMudItem(i => i.xs = 12);
        meta.Property(o => o.ClassBackground).Ignore();
        meta.Properties( o => o.MaxWidth, o => o.MaxHeight).WithOrder(0).WithGroup("Options");
        meta.Properties(o => o.DragMode, o => o.Position, o => o.AnimationDurationInMs).WithOrder(1).WithGroup("Options");
        meta.Properties().Where(p =>
                (p.PropertyInfo.DeclaringType == typeof(DialogOptionsEx) || p.PropertyInfo.DeclaringType == typeof(DialogOptions))
                && (p.PropertyInfo.PropertyType == typeof(bool) || p.PropertyInfo.PropertyType.IsNullableBool()))
            .WithGroup("Options").WithOrder(50).WrapInMudItem(i =>
        {
            i.xl = 6;
            i.lg = 6;
            i.xs = 12;
        });
        meta.Properties(o => o.Position).WrapInMudItem(i =>
        {
            i.xl = 12;
            i.lg = 12;
            i.xs = 12;
        });

        meta.Property(o => o.ShowAtCursor).WithOrder(99);
        meta.Property(o => o.CursorPositionOrigin).WithOrder(100).WithGroup("Options").AsReadOnlyIf(m => !m.ShowAtCursor);

        meta.WrapEachInMudItem(i =>
        {
            i.xl = 6;
            i.lg = 6;
            i.xs = 12;
        });
        return Task.CompletedTask;
    }
}