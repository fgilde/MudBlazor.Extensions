using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Options;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper;

internal static class MudPopoverExtensions
{
    public static Task<MudPopover> AutoHideOnBlur(this MudPopover popover, IJSRuntime jsRuntime, params string[] excludedElementSelectors)
    {
        return popover.ExtendWith(o =>
        {
            o.JsRuntime = jsRuntime;
            o.AutoHideOnBlur = true;
            o.ExcludedBlurSelectors = excludedElementSelectors;
        });
    }

    public static Task<MudPopover> ExtendWith(this MudPopover popover, Action<MudPopoverOptionsEx> options)
    {
        var optionsEx = new MudPopoverOptionsEx();
        options?.Invoke(optionsEx);
        return popover.ExtendWith(optionsEx);
    }
    public static async Task<MudPopover> ExtendWith(this MudPopover popover, MudPopoverOptionsEx options)
    {
        var cssClass = !string.IsNullOrWhiteSpace(popover.Class) ? popover.Class : $"popover-{Guid.NewGuid().ToFormattedId()}";
        popover.Class = cssClass;
        var js = await JsImportHelper.GetInitializedJsRuntime(popover, options.JsRuntime);
        //if (options.AutoHideOnBlur)
        //await new CustomEventInterop<PointerEventArgs>(js).OnBlur(args => Task.FromResult(popover.Open = false), options.ExcludedBlurSelectors.Concat(new []{ popover.Class }).ToArray());


        return popover;
    }
}