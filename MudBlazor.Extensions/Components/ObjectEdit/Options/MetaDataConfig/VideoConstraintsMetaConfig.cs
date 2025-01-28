using MudBlazor.Extensions.Core.W3C;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options.MetaDataConfig;

public class VideoConstraintsMetaConfig : IObjectMetaConfiguration<VideoConstraints>
{
    public Task ConfigureAsync(ObjectEditMeta<VideoConstraints> meta)
    {
        meta.WrapEachInMudItem(i =>
        {
            i.xl = 6;
            i.lg = 6;
            i.xs = 12;
        });
        meta.GroupByCategoryAttribute();
        return Task.CompletedTask;
    }
}