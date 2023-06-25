using System.Reflection;
using Microsoft.AspNetCore.Components;
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

        /// <summary>
        /// Set to true to delay value change event to picker close event
        /// </summary>
        [Parameter] public bool DelayValueChangeToPickerClose { get; set; } = true;

        /// <summary>
        /// Set to true to use the browser native control as picker element
        /// </summary>
        [Parameter] public bool UseNativeBrowserControl { get; set; }

        /// <summary>
        /// Set to true to use MudExColorBubble as picker element
        /// </summary>
        [Parameter] public bool UseMudExColorBubble { get; set; }

        /// <summary>
        /// Set to true to use the palette from picker for native browser element as well
        /// </summary>
        [Parameter] public bool UseColorPaletteInNativeBrowserControl { get; set; }

        /// <summary>
        /// The Initial color that should be selected
        /// </summary>
        [Parameter] public MudColor InitialColor { get; set; }
        
        /// <summary>
        /// Render the base component
        /// </summary>
        /// <returns></returns>
        protected RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

        /// <summary>
        /// Converter for string and MudColor
        /// </summary>
        public MudBlazor.Converter<MudColor, string> ColorConverter { get; set; } = new()
        {
            GetFunc = s => new MudColor(s),
            SetFunc = c => c.ToString(MudColorOutputFormats.Hex)
        };

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            SetInitialColorOneTime();
            base.OnInitialized();
        }
        
        /// <inheritdoc />
        protected override void OnPickerOpened()
        {

            base.OnPickerOpened();
            if (!ShouldDelay) return;

            _originalValueChanged = ValueChanged;
            ValueChanged = EventCallback.Factory.Create(this,
                EventCallback.Factory.CreateInferred(this, x =>
                    null,
                    Value
                    )
                );
        }

        /// <inheritdoc />
        protected override void OnPickerClosed()
        {
            if (ShouldDelay)
                _originalValueChanged.InvokeAsync(Value);
            base.OnPickerClosed();
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
                try
                {
                    var c = _value;
                    _value = InitialColor;
                    typeof(MudColorPicker).GetMethod("UpdateBaseColor", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(this, new object[] { });
                    typeof(MudColorPicker).GetMethod("UpdateColorSelectorBasedOnRgb", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(this, new object[] { });
                    typeof(MudColorPicker).GetMethod("UpdateBaseColorSlider", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(this, new object[] { InitialColor.H });
                    typeof(MudColorPicker).GetMethod("SelectPaletteColor", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(this, new object[] { InitialColor });
                    SetTextAsync(InitialColor.ToString(!DisableAlpha ? MudColorOutputFormats.HexA : MudColorOutputFormats.Hex), false).AndForget();
                    FieldChanged(InitialColor);
                    StateHasChanged();
                    _value = c;
                }
                catch { /* ignore */ }
            }
        }
        
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
