using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Components
{
    /// <summary>
    /// A simple ColorPicker component that inherits the MudColorPicker but adds the possibility to delay the close event and use a native picker component from browser
    /// </summary>
    public partial class MudExColorPicker
    {
        private string presetId = $"preset-{Guid.NewGuid()}";
        private bool ShouldDelay => DelayValueChangeToPickerClose && PickerVariant != PickerVariant.Static;
        private EventCallback<MudColor> _originalValueChanged;
        private bool _delaying;

        /// <summary>
        /// Set to true to delay value change event to picker close event
        /// </summary>
        [Parameter, SafeCategory("Behavior")]
        public bool DelayValueChangeToPickerClose { get; set; } = true;

        /// <summary>
        /// Set to true to use the browser native control as picker element
        /// </summary>
        [Parameter, SafeCategory("Behavior")]
        public bool UseNativeBrowserControl { get; set; }

        /// <summary>
        /// Set to true to use MudExColorBubble as picker element
        /// </summary>
        [Parameter, SafeCategory("Behavior")]
        public bool UseMudExColorBubble { get; set; }

        /// <summary>
        /// Set to true to use the palette from picker for native browser element as well
        /// </summary>
        [Parameter, SafeCategory("Behavior")]
        public bool UseColorPaletteInNativeBrowserControl { get; set; }

        /// <summary>
        /// The Initial color that should be selected
        /// </summary>
        [Parameter, SafeCategory("Data")]
        public MudColor InitialColor { get; set; }

        /// <summary>
        /// Render the base component
        /// </summary>
        /// <returns></returns>
        private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

        /// <summary>
        /// Converter for string and MudColor
        /// </summary>
        public IReversibleConverter<MudColor?, string?> ColorConverter { get; set; } = StringMudColorConverter.Instance;

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            SetInitialColorOneTime();
            base.OnInitialized();
        }
        
        /// <inheritdoc />
        protected override async Task OnPickerOpenedAsync()
        {

            await base.OnPickerOpenedAsync();
            if (!ShouldDelay || _delaying) return;

            // without a callback the picker only changes its own state while open, the parent hears about it on close
            _delaying = true;
            _originalValueChanged = ValueChanged;
            ValueChanged = default;
        }

        /// <inheritdoc />
        protected override async Task OnPickerClosedAsync()
        {
            if (_delaying)
            {
                // the callback is restored, otherwise the next open saves the empty one as original and no change arrives anymore;
                // the picked color is in the parameter state, the Value property still holds what the parent passed
                _delaying = false;
                ValueChanged = _originalValueChanged;
                await ValueChanged.InvokeAsync(this.GetState(x => x.Value));
            }
            await base.OnPickerClosedAsync();
        }

        private Task NativeColorChange(ChangeEventArgs arg)
        {
            Value = new MudColor(arg.Value.ToString());
            return ValueChanged.InvokeAsync(Value);
        }

        private void SetInitialColorOneTime()
        {
            if (InitialColor != null)
            {
                // MudColorPicker initializes spectrum, selector and sliders from Value ?? _lastColor in OnInitializedAsync,
                // which runs after this, so a null Value starts at InitialColor
                LastColor(this) = InitialColor;
                _ = SetTextAsync(InitialColor.ToString(ShowAlpha ? MudColorOutputFormats.HexA : MudColorOutputFormats.Hex), false);
                FieldChanged(InitialColor);
            }
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_lastColor")]
        private static extern ref MudColor LastColor(MudColorPicker picker);
        
        private Task NativeSelectionChange(EventArgs arg)
        {
            if (!DelayValueChangeToPickerClose)
            {
                var args = arg as ChangeEventArgs;
                Value = new MudColor(args.Value.ToString());
                return ValueChanged.InvokeAsync(Value);
            }
            return Task.CompletedTask;
        }
        
    }
}
