using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;
using MudBlazor.Utilities;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components
{
    /// <summary>
    /// A Component to quickly select a color
    /// </summary>
    public partial class MudExColorBubble
    {
        /// <summary>
        /// Gets or sets the text for the select color button.
        /// </summary>
        [Parameter, SafeCategory("Data")]
        public string SelectColorText { get; set; } = "Select color";

        /// <summary>
        /// Gets or sets a value indicating whether to show a preview of the selected color in the component.
        /// </summary>
        [Parameter, SafeCategory("Behavior")]
        public bool ShowColorPreview
        {
            get => _showColorPreview;
            set
            {
                _showColorPreview = value;
                UpdateJsOptions();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to allow selecting a color by clicking on the preview of the selected color.
        /// </summary>
        [Parameter, SafeCategory("Behavior")]
        public bool AllowSelectOnPreviewClick
        {
            get => _allowSelectOnPreviewClick;
            set
            {
                _allowSelectOnPreviewClick = value;
                UpdateJsOptions();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to close the selector after selecting a color.
        /// </summary>
        [Parameter, SafeCategory("Behavior")]
        public bool CloseAfterSelect
        {
            get => _closeAfterSelect;
            set
            {
                _closeAfterSelect = value;
                UpdateJsOptions();
            }
        }

        /// <summary>
        /// Gets or sets the minimum luminance of the selected color.
        /// </summary>
        [Parameter, SafeCategory("Data")]
        public int MinLuminance
        {
            get => _minLuminance;
            set
            {
                _minLuminance = Math.Max(0, Math.Min(value, 100));
                UpdateJsOptions();
            }
        }

        /// <summary>
        /// Gets or sets the maximum luminance of the selected color.
        /// </summary>
        [Parameter, SafeCategory("Data")]
        public int MaxLuminance
        {
            get => _maxLuminance;
            set
            {
                _maxLuminance = Math.Max(0, Math.Min(value, 100)); ;
                UpdateJsOptions();
            }
        }

        /// <summary>
        /// Gets or sets the size of the color selector.
        /// </summary>
        [Parameter, SafeCategory("Appearance")]
        public int SelectorSize
        {
            get => _selectorSize;
            set
            {
                _selectorSize = value;
                UpdateJsOptions();
            }
        }

        /// <summary>
        /// Gets or sets the event callback for when the user changes the selected color.
        /// </summary>
        [Parameter, SafeCategory("Click action")]
        public EventCallback<MudColor> ColorChanged { get; set; }

        /// <summary>
        /// Gets or sets the currently selected color.
        /// </summary>
        [Parameter, SafeCategory("Data")]
        public MudColor Color
        {
            get => _color;
            set
            {
                _color = value;
                UpdateJsOptions();
            }
        }

        /// <summary>
        /// Gets or sets the height of the component.
        /// </summary>
        [Parameter, SafeCategory("Appearance")]
        public MudExSize<double> Height { get; set; } = 16;

        /// <summary>
        /// Gets or sets the width of the component.
        /// </summary>
        [Parameter, SafeCategory("Appearance")]
        public MudExSize<double> Width { get; set; } = 16;

        private ElementReference _canvasContainerReference;
        private MudColor _color = new("#000000");
        private int _selectorSize = 161;
        private bool _showColorPreview = true;
        private int _maxLuminance = 86;
        private int _minLuminance = 17;
        private bool _allowSelectOnPreviewClick = true;
        private bool _closeAfterSelect = true;

        /// <summary>
        /// Gets the JavaScript arguments to pass to the component.
        /// </summary>
        public override object[] GetJsArguments()
        {
            return new[] { ElementReference, _canvasContainerReference, CreateDotNetObjectReference(), Options() };
        }

        /// <summary>
        /// Renders the component after a change has been made.
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
                await JsReference.InvokeVoidAsync("init");
        }

        /// <summary>
        /// Event that is triggered when the user changes the color.
        /// </summary>
        /// <param name="color">The new color value.</param>
        [JSInvokable]
        public async Task OnColorChanged(string color)
        {
            Color = new MudColor(color);
            await ColorChanged.InvokeAsync(Color);
            StateHasChanged();
        }

        /// <summary>
        /// Event that is triggered when the user clicks on the preview of the selected color.
        /// </summary>
        [JSInvokable]
        public async Task OnColorPreviewClick()
        {
            await SelectColorWithMudExColorEdit();
        }

        private async Task SelectColorWithMudExColorEdit()
        {
            var dialogOptionsEx = DialogOptionsEx.OverriddenDefaultOptions ? DialogOptionsEx.DefaultDialogOptions : new DialogOptionsEx
            {
                Animation = AnimationType.FlipY,
                DragMode = MudDialogDragMode.Simple,
                CursorPositionOrigin = Origin.BottomCenter,
                ShowAtCursor = true,
                CloseButton = true
            };

            var res = await DialogService?.ShowComponentInDialogAsync<MudExColorEdit>(TryLocalize(SelectColorText), "",
                RenderDataDefaults.ColorPickerOptions()
                    .AddOrUpdate(nameof(MudExColorEdit.PickerVariant), PickerVariant.Static)
                    .AddOrUpdate(nameof(MudExColorEdit.Value), Color.ToString(MudColorOutputFormats.Hex)),
                dialog =>
                {
                    dialog.Icon = Icons.Material.Filled.ColorLens;
                    dialog.Buttons = MudExDialogResultAction.OkCancel();
                }, dialogOptionsEx);

            if (!res.DialogResult.Canceled)
            {
                Color = await res.Component.Value.ToMudColorAsync(JsRuntime);
                await ColorChanged.InvokeAsync(Color);
                StateHasChanged();
            }
        }

        private string StyleStr() => $"background-color: {Color}; height: {Height}; width: {Width}; {Style}";

        private string CanvasContainerStyle() => $"border-radius: {(ShowColorPreview ? "0" : "100%")}; width: {SelectorSize}px; height: {SelectorSize}px";

        private void UpdateJsOptions()
        {
            if (JsReference != null)
                JsReference.InvokeVoidAsync("setOptions", Options());
        }

        private object Options()
        {
            _selectorSize = _selectorSize % 2 != 0 ? _selectorSize : _selectorSize + 1;
            var color = Color?.ToString(MudColorOutputFormats.Hex);
            return new
            {
                Color = color, // Color on js needs to be a string
                MinLuminance,
                CloseAfterSelect,
                AllowSelectOnPreviewClick,
                MaxLuminance,
                ShowColorPreview,
                SelectorSize
            };
        }

        /// <summary>
        /// Shows the color selector as an asynchronous operation.
        /// </summary>
        public async Task ShowSelectorAsync()
        {
            await JsReference.InvokeVoidAsync("showSelector");
        }
    }
}