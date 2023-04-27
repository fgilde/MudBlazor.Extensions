using MainSample.WebAssembly.Types;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Options;

namespace MainSample.WebAssembly.ObjectEditMetaConfig;

public class SomeClassWithUserMetaConfiguration : IObjectMetaConfiguration<SomeClassWithUser>
{
    public Task ConfigureAsync(ObjectEditMeta<SomeClassWithUser> meta)
    {
        meta.Properties(m => m.Users)
            .RenderWith<MudExCollectionEditor<UserModel>, ICollection<UserModel>>(ce => ce.Items, editor => editor.DialogOptions = new DialogOptionsEx
            {
                MaximizeButton = true,
                CloseButton = true,
                FullHeight = true,
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                DragMode = MudDialogDragMode.Simple,
                Animations = new[] { AnimationType.SlideIn },
                Position = DialogPosition.CenterRight,
                DisableSizeMarginY = true,
                DisablePositionMargin = true
            });
        
        meta.Property(m => m.MainUser).RenderWith<MudExObjectEditForm<UserModel>, UserModel>(mudExObjectEdit => mudExObjectEdit.Value);
        return Task.CompletedTask;
    }
}