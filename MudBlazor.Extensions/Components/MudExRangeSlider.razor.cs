using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Contracts;
using System;

namespace MudBlazor.Extensions.Components
{
    public partial class MudExRangeSlider<T> where T : struct, IComparable<T>
    {
        private ElementReference _trackRef;
        private readonly string _startId = $"start_{Guid.NewGuid():N}";
        private readonly string _endId = $"end_{Guid.NewGuid():N}";

        [Parameter] public IRangeAdapter<T>? Adapter { get; set; }

        [Parameter, SafeCategory("Data")] public IRange<T> Value { get; set; } = new MudExRange<T>(default!, default!);
        [Parameter, SafeCategory("Data")] public EventCallback<IRange<T>> ValueChanged { get; set; }

        [Parameter, SafeCategory("Data")] public T Min { get; set; } = default!;
        [Parameter, SafeCategory("Data")] public T Max { get; set; } = default!;

        [Parameter, SafeCategory("Data")] public T Step { get; set; }
        [Parameter, SafeCategory("Data")] public TimeSpan? StepSpan { get; set; }

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

        private IRangeAdapter<T> A => Adapter ??= RangeAdapters.For<T>(Step, StepSpan);

        protected string _startPct => Math.Clamp(A.Percent(Value.Start, Min, Max), 0, 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        protected string _endPct => Math.Clamp(A.Percent(Value.End, Min, Max), 0, 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

        private string StartString => Value.Start.ToString() ?? string.Empty;
        private string EndString => Value.End.ToString() ?? string.Empty;
        private string MinString => Min.ToString() ?? string.Empty;
        private string MaxString => Max.ToString() ?? string.Empty;

        private string ColorClass => $"mud-ex-range-slider-{Color.ToString().ToLower()}";

        public override object[] GetJsArguments() => new object[] { ElementReference, _trackRef, CreateDotNetObjectReference() };

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender) await JsReference.InvokeVoidAsync("init");
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
            _dragStartRange = (A.ToDouble(Value.Start), A.ToDouble(Value.End));
            await MeasureAsync();
            await JsReference.InvokeVoidAsync("startCapture");
        }

        private async Task OnTrackPointerDown(PointerEventArgs e)
        {
            if (Disabled || ReadOnly) return;
            await MeasureAsync();
            var pct = Math.Clamp((e.ClientX - _trackLeft) / _trackWidth, 0, 1);
            var snapped = A.Snap(A.Lerp(Min, Max, pct), Min, Max);
            var dStart = Math.Abs(A.ToDouble(snapped) - A.ToDouble(Value.Start));
            var dEnd = Math.Abs(A.ToDouble(snapped) - A.ToDouble(Value.End));
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
                var delta = A.Scale(Max, Min, deltaPct);

                var span = _dragStartRange.end - _dragStartRange.start;
                var newStart = _dragStartRange.start + delta;
                var newEnd = newStart + span;

                var minD = A.ToDouble(Min);
                var maxD = A.ToDouble(Max);
                if (newStart < minD) { newStart = minD; newEnd = minD + span; }
                if (newEnd > maxD) { newEnd = maxD; newStart = maxD - span; }

                var s = A.FromDouble(newStart);
                var e = A.FromDouble(newEnd);
                s = A.Snap(s, Min, Max);
                e = A.Snap(e, Min, Max);
                Value = new MudExRange<T>(s, e);
                if (Immediate) await OnInput.InvokeAsync(Value);
                StateHasChanged();
                return;
            }

            var pct = Math.Clamp((clientX - _trackLeft) / _trackWidth, 0, 1);
            var snappedVal = A.Snap(A.Lerp(Min, Max, pct), Min, Max);

            if (_dragMode == DragMode.StartThumb)
            {
                var newStart = A.Clamp(snappedVal, Min, Value.End);
                if (!Equals(newStart, Value.Start))
                {
                    Value = new MudExRange<T>(newStart, Value.End);
                    if (Immediate) await OnInput.InvokeAsync(Value);
                    StateHasChanged();
                }
            }
            else if (_dragMode == DragMode.EndThumb)
            {
                var newEnd = A.Clamp(snappedVal, Value.Start, Max);
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
            var s = A.Clamp(A.Snap(v, Min, Max), Min, Value.End);
            if (!Equals(s, Value.Start))
            {
                Value = new MudExRange<T>(s, Value.End);
                if (commit) { await ValueChanged.InvokeAsync(Value); await OnChange.InvokeAsync(Value); }
                else if (Immediate) await OnInput.InvokeAsync(Value);
            }
        }

        private async Task SetEndAsync(T v, bool commit)
        {
            var e = A.Clamp(A.Snap(v, Min, Max), Value.Start, Max);
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
                var target = home ? Min : end ? Value.End : A.AddSteps(Value.Start, deltaSteps);
                await SetStartAsync(target, true);
            }
            else
            {
                var target = home ? Value.Start : end ? Max : A.AddSteps(Value.End, deltaSteps);
                await SetEndAsync(target, true);
            }
        }

        private record struct DomRect(double Left, double Top, double Width, double Height);
    }
}
