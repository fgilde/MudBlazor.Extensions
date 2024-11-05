using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using Nextended.Core.Extensions;

namespace MainSample.WebAssembly.ObjectEditMetaConfig;

public class DisplayMediaOptionsMetaConfiguration : IObjectMetaConfiguration<DisplayMediaOptions>
{
    public Task ConfigureAsync(ObjectEditMeta<DisplayMediaOptions> meta)
    {
        meta.Properties(
                o => o.Audio.DeviceId,
                o => o.Audio.GroupId,
                o => o.Video.DeviceId,
                o => o.Video.GroupId
                ).Ignore();
        meta.Property(o => o.Audio).Children.Recursive(om => om.Children).Except(meta.Properties(o => o.Audio.DeviceId, o => o.Audio.GroupId))
            .IgnoreIf<DisplayMediaOptions>(o => o.SystemAudio != IncludeExclude.Include);

        meta.WrapEachInMudItem(i =>
        {
            i.xl = 6;
            i.lg = 6;
            i.xs = 12;
        });

        return Task.CompletedTask;
    }
}