using System;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions
{
    public class MudDialogButton
    {
        public MudDialogButton(DotNetObjectReference<object> callBackReference, string callbackName)
        {
            CallBackReference = callBackReference;
            CallbackName = callbackName;
        }

        public DotNetObjectReference<object> CallBackReference { get; }
        public string CallbackName { get; }

        public string Id { get; internal set; } = $"mud-dialog-button-{Guid.NewGuid()}";
        public string Icon { get; set; }
        public string Class { get; set; } = "mud-button-root mud-button-maximize mud-icon-button mud-ripple mud-ripple-icon";
        public string Html { get; internal set; }

        internal string GetHtml(int pos)
        {
            int left = 40;
            return $"<button id=\"{Id}\" style=\"right: {(pos * left)}px;\" type=\"button\" class=\"{Class}\"><span class=\"mud-icon-button-label\"><svg class=\"mud-icon-root mud-svg-icon mud-inherit-text mud-icon-size-medium\" focusable=\"false\" viewBox=\"0 0 24 24\" aria-hidden=\"true\">{Icon}</svg></span></button>";
        }

    }
}