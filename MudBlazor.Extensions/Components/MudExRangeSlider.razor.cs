using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Contracts;
using System;
using System.Globalization;

namespace MudBlazor.Extensions.Components
{
    public partial class MudExRangeSlider<T> where T : struct, IComparable<T>
    {
        private ElementReference _trackRef;
        private readonly string _startId = $"start_{Guid.NewGuid():N}";
        private readonly string _endId = $"end_{Guid.NewGuid():N}";

        [Parameter, SafeCategory("Data")] public IRange<T> Value { get; set; } = new MudExRange<T>(default, default);
        [Parameter] public EventCallback<IRange<T>> ValueChanged { get; set; }

        [Parameter, SafeCategory("Data")] public IRange<T> SizeRange { get; set; } = new MudExRange<T>(default, default);
        [Parameter] public EventCallback<IRange<T>> SizeRangeChanged { get; set; }

        [Parameter, SafeCategory("Data")] public IRange<T> StepRange { get; set; } = new MudExRange<T>(default, default);
        [Parameter] public EventCallback<IRange<T>> StepRangeChanged { get; set; }

        [Parameter, SafeCategory("Behavior")] public bool Immediate { get; set; } = true;
        [Parameter, SafeCategory("Behavior")] public bool Disabled { get; set; }
        [Parameter, SafeCategory("Behavior")] public bool ReadOnly { get; set; }
        [Parameter, SafeCategory("Behavior")] public bool ShowInputs { get; set; } = true;
        [Parameter, SafeCategory("Behavior")] public bool AllowWholeRangeDrag { get; set; } = true;

        [Parameter, SafeCategory("Appearance")] public Color Color { get; set; } = Color.Primary;
        [Parameter, SafeCategory("Appearance")] public string? AriaLabelledBy { get; set; }

        [Parameter] public EventCallback<IRange<T>> OnChange { get; set; }
        [Parameter] public EventCallback<IRange<T>> OnInput { get; set; }

        private enum DragMode { None, StartThumb, EndThumb, WholeRange }
        private DragMode _dragMode = DragMode.None;
        private double _trackLeft;
        private double _trackWidth;
        private double _dragStartClientX;
        private (double start, double end) _dragStartRange;

        protected string _startPct => Math.Clamp(ValueMath<T>.Percent(Value.Start, SizeRange), 0, 1).ToString(CultureInfo.InvariantCulture);
        protected string _endPct => Math.Clamp(ValueMath<T>.Percent(Value.End, SizeRange), 0, 1).ToString(CultureInfo.InvariantCulture);

        private string StartString => Value.Start.ToString() ?? string.Empty;
        private string EndString => Value.End.ToString() ?? string.Empty;
        private string SizeRangeStringStart => SizeRange.Start.ToString() ?? string.Empty;
        private string SizeRangeStringEnd => SizeRange.End.ToString() ?? string.Empty;

        private string ColorClass => $"mud-ex-range-slider-{Color.ToString().ToLower()}";

        public override object[] GetJsArguments() => new object[] { ElementReference, _trackRef, CreateDotNetObjectReference() };

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender) await JsReference.InvokeVoidAsync("init");
        }

        protected override void OnParametersSet()
        {
            var s = ValueMath<T>.Clamp(ValueMath<T>.SnapToStep(Value.Start, SizeRange, StepRange), SizeRange);
            var e = ValueMath<T>.Clamp(ValueMath<T>.SnapToStep(Value.End, SizeRange, StepRange), SizeRange);
            if (s.CompareTo(e) > 0) (s, e) = (e, s);
            Value = new MudExRange<T>(s, e);
        }

        private async Task MeasureAsync()
        {
            var rect = await JsReference.InvokeAsync<DomRect>("measureTrack");
            _trackLeft = rect.Left; _trackWidth = Math.Max(1, rect.Width);
        }

        private enum Thumb { Start, End }

        private async Task OnThumbPointerDownAsync(PointerEventArgs e, Thumb which)
        {
            if (Disabled || ReadOnly) return;
            _dragMode = which == Thumb.Start ? DragMode.StartThumb : DragMode.EndThumb;
            await MeasureAsync();
            await JsReference.InvokeVoidAsync("startCapture");
        }

        private async Task OnRangeDragPointerDown(PointerEventArgs e)
        {
            if (!AllowWholeRangeDrag || Disabled || ReadOnly) return;
            _dragMode = DragMode.WholeRange;
            _dragStartClientX = e.ClientX;
            _dragStartRange = (ValueMath<T>.ToDouble(Value.Start), ValueMath<T>.ToDouble(Value.End));
            await MeasureAsync();
            await JsReference.InvokeVoidAsync("startCapture");
        }

        private async Task OnTrackPointerDown(PointerEventArgs e)
        {
            if (Disabled || ReadOnly) return;
            await MeasureAsync();
            var pct = Math.Clamp((e.ClientX - _trackLeft) / _trackWidth, 0, 1);
            var snapped = ValueMath<T>.SnapToStep(ValueMath<T>.Lerp(SizeRange, pct), SizeRange, StepRange);
            var dStart = Math.Abs(ValueMath<T>.ToDouble(snapped) - ValueMath<T>.ToDouble(Value.Start));
            var dEnd = Math.Abs(ValueMath<T>.ToDouble(snapped) - ValueMath<T>.ToDouble(Value.End));
            if (dStart <= dEnd) await SetStartAsync(snapped, true);
            else await SetEndAsync(snapped, true);
        }

        [JSInvokable]
        public async Task OnPointerMove(double clientX)
        {
            if (Disabled || ReadOnly) return;
            if (_dragMode == DragMode.None) return;

            if (_dragMode == DragMode.WholeRange)
            {
                var deltaPx = clientX - _dragStartClientX;
                var deltaPct = deltaPx / _trackWidth;
                var delta = (ValueMath<T>.ToDouble(SizeRange.End) - ValueMath<T>.ToDouble(SizeRange.Start)) * deltaPct;

                var span = _dragStartRange.end - _dragStartRange.start;
                var newStart = _dragStartRange.start + delta;
                var newEnd = newStart + span;

                var minD = ValueMath<T>.ToDouble(SizeRange.Start);
                var maxD = ValueMath<T>.ToDouble(SizeRange.End);
                if (newStart < minD) { newStart = minD; newEnd = minD + span; }
                if (newEnd > maxD) { newEnd = maxD; newStart = maxD - span; }

                var s = ValueMath<T>.FromDouble(newStart);
                var e = ValueMath<T>.FromDouble(newEnd);
                s = ValueMath<T>.SnapToStep(s, SizeRange, StepRange);
                e = ValueMath<T>.SnapToStep(e, SizeRange, StepRange);
                Value = new MudExRange<T>(s, e);
                if (Immediate) await OnInput.InvokeAsync(Value);
                StateHasChanged();
                return;
            }

            var pct = Math.Clamp((clientX - _trackLeft) / _trackWidth, 0, 1);
            var snappedVal = ValueMath<T>.SnapToStep(ValueMath<T>.Lerp(SizeRange, pct), SizeRange, StepRange);

            if (_dragMode == DragMode.StartThumb)
            {
                var newStart = ValueMath<T>.Clamp(snappedVal, new MudExRange<T>(SizeRange.Start, Value.End));
                if (!Equals(newStart, Value.Start))
                {
                    Value = new MudExRange<T>(newStart, Value.End);
                    if (Immediate) await OnInput.InvokeAsync(Value);
                    StateHasChanged();
                }
            }
            else if (_dragMode == DragMode.EndThumb)
            {
                var newEnd = ValueMath<T>.Clamp(snappedVal, new MudExRange<T>(Value.Start, SizeRange.End));
                if (!Equals(newEnd, Value.End))
                {
                    Value = new MudExRange<T>(Value.Start, newEnd);
                    if (Immediate) await OnInput.InvokeAsync(Value);
                    StateHasChanged();
                }
            }
        }

        [JSInvokable]
        public async Task OnPointerUp()
        {
            if (_dragMode == DragMode.None) return;
            _dragMode = DragMode.None;
            await ValueChanged.InvokeAsync(Value);
            await OnChange.InvokeAsync(Value);
            StateHasChanged();
        }

        private async Task SetStartAsync(T v, bool commit)
        {
            var s = ValueMath<T>.Clamp(ValueMath<T>.SnapToStep(v, SizeRange, StepRange), new MudExRange<T>(SizeRange.Start, Value.End));
            if (!Equals(s, Value.Start))
            {
                Value = new MudExRange<T>(s, Value.End);
                if (commit) { await ValueChanged.InvokeAsync(Value); await OnChange.InvokeAsync(Value); }
                else if (Immediate) await OnInput.InvokeAsync(Value);
            }
        }

        private async Task SetEndAsync(T v, bool commit)
        {
            var e = ValueMath<T>.Clamp(ValueMath<T>.SnapToStep(v, SizeRange, StepRange), new MudExRange<T>(Value.Start, SizeRange.End));
            if (!Equals(e, Value.End))
            {
                Value = new MudExRange<T>(Value.Start, e);
                if (commit) { await ValueChanged.InvokeAsync(Value); await OnChange.InvokeAsync(Value); }
                else if (Immediate) await OnInput.InvokeAsync(Value);
            }
        }

        private async Task OnKeyDownAsync(KeyboardEventArgs e, Thumb thumb)
        {
            if (Disabled || ReadOnly) return;
            int deltaSteps = e.Key switch
            {
                "ArrowLeft" or "ArrowDown" => -1,
                "PageDown" => -10,
                "ArrowRight" or "ArrowUp" => 1,
                "PageUp" => 10,
                _ => 0
            };
            bool home = e.Key == "Home";
            bool end = e.Key == "End";
            if (deltaSteps == 0 && !home && !end) return;

            if (thumb == Thumb.Start)
            {
                var target = home ? SizeRange.Start : end ? Value.End : ValueMath<T>.AddSteps(Value.Start, StepRange, deltaSteps);
                await SetStartAsync(target, true);
            }
            else
            {
                var target = home ? Value.Start : end ? SizeRange.End : ValueMath<T>.AddSteps(Value.End, StepRange, deltaSteps);
                await SetEndAsync(target, true);
            }
        }

        private record struct DomRect(double Left, double Top, double Width, double Height);
    }
}
