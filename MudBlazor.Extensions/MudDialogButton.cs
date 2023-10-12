using System;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components;

namespace MudBlazor.Extensions
{
    /// <summary>
    /// Represents a custom dialog button within a MudBlazor dialog.
    /// </summary>
    public class MudDialogButton
    {
        /// <summary>
        /// Constructs a new instance of MudDialogButton.
        /// </summary>
        /// <param name="callBackReference">A .NET object reference to use as a callback.</param>
        /// <param name="callbackName">The name of the callback method.</param>
        public MudDialogButton(DotNetObjectReference<object> callBackReference, string callbackName)
        {
            CallBackReference = callBackReference;
            CallbackName = callbackName;
        }

        /// <summary>
        /// Gets the .NET object reference used as a callback.
        /// </summary>
        public DotNetObjectReference<object> CallBackReference { get; }

        /// <summary>
        /// Gets the name of the callback method.
        /// </summary>
        public string CallbackName { get; }

        /// <summary>
        /// Gets or sets the ID of the button.
        /// </summary>
        public string Id { get; internal set; } = $"mud-dialog-button-{Guid.NewGuid()}";

        /// <summary>
        /// Gets or sets the SVG icon for the button.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the CSS class(es) of the button.
        /// </summary>
        public string Class { get; set; } = "mud-button-root mud-button-maximize mud-icon-button mud-ripple mud-ripple-icon";

        /// <summary>
        /// Gets or sets the HTML content of the button.
        /// </summary>
        public string Html { get; internal set; }

        /// <summary>
        /// Generates the HTML for the button, taking into account its position.
        /// </summary>
        /// <param name="pos">The position of the button.</param>
        /// <returns>A string containing the generated HTML.</returns>
        internal string GetHtml(int pos)
        {
            return $"<button id=\"{Id}\" type=\"button\" class=\"{Class}\"><span class=\"mud-icon-button-label\"><svg class=\"mud-icon-root mud-svg-icon mud-inherit-text mud-icon-size-medium\" focusable=\"false\" viewBox=\"0 0 24 24\" aria-hidden=\"true\">{Icon}</svg></span></button>";
        }
    }
}