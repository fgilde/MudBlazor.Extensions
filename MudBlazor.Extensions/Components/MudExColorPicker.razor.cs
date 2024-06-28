﻿using System.Reflection;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
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
        protected override async Task OnPickerOpenedAsync()
        {

            await base.OnPickerOpenedAsync();
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
        protected override Task OnPickerClosedAsync()
        {
            if (ShouldDelay)
                _originalValueChanged.InvokeAsync(Value);
            return base.OnPickerClosedAsync();
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
                    _ = SetTextAsync(InitialColor.ToString(ShowAlpha ? MudColorOutputFormats.HexA : MudColorOutputFormats.Hex), false);
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
