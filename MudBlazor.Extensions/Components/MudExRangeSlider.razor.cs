using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using Nextended.Core.Contracts;
using System.Numerics;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Types;


namespace MudBlazor.Extensions.Components
{
    public partial class MudExRangeSlider<T> where T : struct, INumber<T>
    {
        private ElementReference _trackRef;
        private string _startId = $"start_" + Guid.NewGuid().ToString("N");
        private string _endId = $"end_" + Guid.NewGuid().ToString("N");


        [Parameter, SafeCategory("Data")] public IRange<T> Value { get; set; } = new RangeOf<T>(T.Zero, T.One);
        [Parameter, SafeCategory("Data")] public EventCallback<IRange<T>> ValueChanged { get; set; }


        [Parameter, SafeCategory("Data")] public T Min { get; set; } = T.Zero;
        [Parameter, SafeCategory("Data")] public T Max { get; set; } = T.One;
        [Parameter, SafeCategory("Data")] public T Step { get; set; } = T.One;


        [Parameter, SafeCategory("Behavior")] public bool Immediate { get; set; } = true; // fire as thumb moves
        [Parameter, SafeCategory("Behavior")] public bool Disabled { get; set; }
        [Parameter, SafeCategory("Behavior")] public bool ReadOnly { get; set; }
        [Parameter, SafeCategory("Behavior")] public bool ShowInputs { get; set; } = false;


        [Parameter, SafeCategory("Appearance")] public Color Color { get; set; } = Color.Primary;
        [Parameter, SafeCategory("Appearance")] public string? AriaLabelledBy { get; set; }


        [Parameter] public EventCallback<IRange<T>> OnChange { get; set; } // fires on commit (pointerup)
        [Parameter] public EventCallback<IRange<T>> OnInput { get; set; } // fires while sliding if Immediate

        private enum Thumb { Start, End }
        private Thumb? _activeThumb;
        private double _trackLeft;
        private double _trackWidth;


        private string _startPx => PercentToPx(RangeMath.Percent(Value.Start, Min, Max));
        private string _endPx => PercentToPx(RangeMath.Percent(Value.End, Min, Max));
        private string _leftPx => PercentToPx(RangeMath.Percent(Value.Start, Min, Max));
        private string _widthPx => PercentToPx(RangeMath.Percent(Value.End, Min, Max) - RangeMath.Percent(Value.Start, Min, Max), true);


        private string StartString => $"{Value.Start}";
        private string EndString => $"{Value.End}";
        private string MinString => $"{Min}";
        private string MaxString => $"{Max}";


        private string ColorClass => $"mud-ex-range-slider-{Color.ToString().ToLower()}";


        public override object[] GetJsArguments()
            => new object[] { ElementReference, _trackRef, CreateDotNetObjectReference() };

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
                await JsReference.InvokeVoidAsync("init");
        }


        private string PercentToPx(double percent, bool isWidth = false)
        {
            percent = Math.Clamp(percent, 0, 1);
            if (_trackWidth <= 0)
                return isWidth ? "0px" : "0px"; // before layout
            var px = percent * _trackWidth;
            return isWidth ? $"{px}px" : $"calc({px}px - 8px)"; // 8px = thumb half width for centering
        }


        private async Task MeasureAsync()
        {
            var rect = await JsReference.InvokeAsync<DomRect>("measureTrack");
            _trackLeft = rect.Left;
            _trackWidth = Math.Max(1, rect.Width);
        }


        private async Task OnPointerDownAsync(PointerEventArgs e, Thumb thumb)
        {
            if (Disabled || ReadOnly) return;
            _activeThumb = thumb;
            await MeasureAsync();
            await JsReference.InvokeVoidAsync("startCapture");
        }

        [JSInvokable]
        public async Task OnPointerMove(double clientX)
        {
            if (Disabled || ReadOnly) return;
            if (_activeThumb is null) return;
            var pct = Math.Clamp((clientX - _trackLeft) / _trackWidth, 0, 1);
            var raw = RangeMath.Lerp(Min, Max, pct);
            var snapped = RangeMath.RoundToStep(raw, Step, Min);


            if (_activeThumb == Thumb.Start)
            {
                var newStart = RangeMath.Clamp(snapped, Min, Value.End);
                if (!newStart.Equals(Value.Start))
                {
                    Value = new RangeOf<T>(newStart, Value.End);
                    if (Immediate) await OnInput.InvokeAsync(Value);
                    StateHasChanged();
                }
            }
            else
            {
                var newEnd = RangeMath.Clamp(snapped, Value.Start, Max);
                if (!newEnd.Equals(Value.End))
                {
                    Value = new RangeOf<T>(Value.Start, newEnd);
                    if (Immediate) await OnInput.InvokeAsync(Value);
                    StateHasChanged();
                }
            }
        }


        [JSInvokable]
        public async Task OnPointerUp()
        {
            if (_activeThumb is null) return;
            _activeThumb = null;
            await ValueChanged.InvokeAsync(Value);
            await OnChange.InvokeAsync(Value);
            StateHasChanged();
        }

        private async Task SetStartAsync(T v, bool commit)
        {
            var s = RangeMath.Clamp(RangeMath.RoundToStep(v, Step, Min), Min, Value.End);
            if (!s.Equals(Value.Start))
            {
                Value = new RangeOf<T>(s, Value.End);
                if (commit) { await ValueChanged.InvokeAsync(Value); await OnChange.InvokeAsync(Value); }
                else if (Immediate) await OnInput.InvokeAsync(Value);
            }
        }


        private async Task SetEndAsync(T v, bool commit)
        {
            var e = RangeMath.Clamp(RangeMath.RoundToStep(v, Step, Min), Value.Start, Max);
            if (!e.Equals(Value.End))
            {
                Value = new RangeOf<T>(Value.Start, e);
                if (commit) { await ValueChanged.InvokeAsync(Value); await OnChange.InvokeAsync(Value); }
                else if (Immediate) await OnInput.InvokeAsync(Value);
            }
        }

        private async Task OnKeyDownAsync(KeyboardEventArgs e, Thumb thumb)
        {
            if (Disabled || ReadOnly) return;
            var delta = e.Key switch
            {
                "ArrowLeft" => -1,
                "ArrowDown" => -1,
                "PageDown" => -10,
                "ArrowRight" => 1,
                "ArrowUp" => 1,
                "PageUp" => 10,
                "Home" => int.MinValue,
                "End" => int.MaxValue,
                _ => 0
            };
            if (delta == 0) return;


            if (thumb == Thumb.Start)
            {
                var target = delta == int.MinValue ? Min : delta == int.MaxValue ? Value.End : RangeMath.FromDouble<T>(RangeMath.ToDouble(Value.Start) + RangeMath.ToDouble(Step) * delta);
                await SetStartAsync(target, true);
            }
            else
            {
                var target = delta == int.MinValue ? Value.Start : delta == int.MaxValue ? Max : RangeMath.FromDouble<T>(RangeMath.ToDouble(Value.End) + RangeMath.ToDouble(Step) * delta);
                await SetEndAsync(target, true);
            }
        }

        private async Task OnTrackClick(MouseEventArgs e)
        {
            if (Disabled || ReadOnly) return;
            await MeasureAsync();
            var pct = Math.Clamp((e.ClientX - _trackLeft) / _trackWidth, 0, 1);
            var raw = RangeMath.Lerp(Min, Max, pct);
            var snapped = RangeMath.RoundToStep(raw, Step, Min);


            // choose nearest thumb
            var dStart = Math.Abs(RangeMath.ToDouble(snapped) - RangeMath.ToDouble(Value.Start));
            var dEnd = Math.Abs(RangeMath.ToDouble(snapped) - RangeMath.ToDouble(Value.End));
            if (dStart <= dEnd) await SetStartAsync(snapped, true);
            else await SetEndAsync(snapped, true);
        }


        private record struct DomRect(double Left, double Top, double Width, double Height);
    }

}
