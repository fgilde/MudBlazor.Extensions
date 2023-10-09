using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions;

public static partial class DialogServiceExt
{
    /// <summary>
    /// Show  the MudExThemeEdit inside of a dialog and returns the new theme if user saved it.
    /// </summary>
    public static async Task<TTheme> ShowThemeEditAsync<TTheme>(this IDialogService service, TTheme theme, 
        string title = "Edit Theme", string message = "",
        bool? darkMode = null,
        DialogOptionsEx dialogOptions = null) where TTheme : MudTheme
    {
        var res = await service.ShowComponentInDialogAsync<MudExThemeEdit<TTheme>>(title, message,
            themeEdit =>
            {
                themeEdit.AllowPresetsEdit = false;
                themeEdit.Theme = theme.CloneTheme();
                themeEdit.IsDark = darkMode;
            },
            dialog =>
            {
                dialog.ClassActions = MudExCss.Classes.Dialog.DialogActionsSticky;
                dialog.Icon = Icons.Material.Filled.Palette;
                dialog.Buttons = MudExDialogResultAction.OkCancel();
            }, dialogOptions ?? DialogOptionsEx.SlideInFromRight);

        return res.DialogResult.Canceled ? theme : res.Component.Theme;
    }
}