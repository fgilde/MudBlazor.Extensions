using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor.Utilities;
using Newtonsoft.Json.Linq;

namespace MudBlazor.Extensions.Components
{
    /// <summary>
    /// A simple ColorPicker component that inherits the MudColorPicker but adds the possibility to delay the close event and use a native picker component from browser
    /// </summary>
    public partial class MudExColorPicker
    {
        [Parameter] public bool DelayValueChangeToPickerClose { get; set; } = true;
        [Parameter] public bool UseNativeBrowserControl { get; set; }
        [Parameter] public bool UseMudExColorBubble { get; set; }
        [Parameter] public bool UseColorPaletteInNativeBrowserControl { get; set; }
        [Parameter] public MudColor InitialColor { get; set; }

        private string presetId = $"preset-{Guid.NewGuid()}";
        private bool ShouldDelay => DelayValueChangeToPickerClose && PickerVariant != PickerVariant.Static;
        protected RenderFragment Inherited() => builder => base.BuildRenderTree(builder);
        EventCallback<MudColor> _originalValueChanged;

        protected override void OnInitialized()
        {
            SetInitialColorOneTime();
            base.OnInitialized();
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

        public MudBlazor.Converter<MudColor, string> ColorConverter { get; set; } = new()
        {
            GetFunc = s => new MudColor(s),
            SetFunc = c => c.ToString(MudColorOutputFormats.Hex)
        };

    }
}
