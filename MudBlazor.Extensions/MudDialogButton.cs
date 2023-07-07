using System;
using Microsoft.JSInterop;

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

    /// <summary>
    /// Represents an action within a MudBlazor dialog that returns a specific dialog result.
    /// </summary>
    public class MudExDialogResultAction
    {
        /// <summary>
        /// Gets or sets the DialogResult for this action.
        /// </summary>
        public DialogResult Result { get; set; }

        /// <summary>
        /// Gets or sets the text displayed on the action button.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the visual style variant for the action button.
        /// </summary>
        public Variant Variant { get; set; }

        /// <summary>
        /// Gets or sets the color for the action button.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Creates an array with a single "Ok" action.
        /// </summary>
        /// <param name="confirmText">The text for the "Ok" button. Defaults to "Ok".</param>
        /// <returns>An array with a single "Ok" action.</returns>
        public static MudExDialogResultAction[] Ok(string confirmText = "Ok") => new[] { OkCancel(confirmText).Last() };

        /// <summary>
        /// Creates an array with a single "Cancel" action.
        /// </summary>
        /// <param name="confirmText">The text for the "Cancel" button. Defaults to "Ok".</param>
        /// <returns>An array with a single "Cancel" action.</returns>
        public static MudExDialogResultAction[] Cancel(string confirmText = "Ok") => new[] { OkCancel(confirmText).First() };

        /// <summary>
        /// Creates an array with both "Ok" and "Cancel" actions.
        /// </summary>
        /// <param name="confirmText">The text for the "Ok" button. Defaults to "Ok".</param>
        /// <param name="cancelText">The text for the "Cancel" button. Defaults to "Cancel".</param>
        /// <returns>An array with both "Ok" and "Cancel" actions.</returns>
        public static MudExDialogResultAction[] OkCancel(string confirmText = "Ok", string cancelText = "Cancel")
        {
            var actions = new[]
            {
            new MudExDialogResultAction
            {
                Label = cancelText,
                Variant = Variant.Text,
                Result = DialogResult.Cancel()
            },
            new MudExDialogResultAction
            {
                Label = confirmText,
                Color = Color.Error,
                Variant = Variant.Filled,
                Result = DialogResult.Ok(true)
            },
        };
            return actions;
        }
    }

}