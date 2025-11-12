using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Contracts;
using Nextended.Core.Types;
using Nextended.Core.Types.Ranges.Math;
using System;
using System.Globalization;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components
{
    public partial class MudExRangeSlider<T> where T : struct, IComparable<T>
    {
        [Parameter]
        public IRangeMath<T> MathAdapter { get; set; }

        private ElementReference _trackRef;
        private readonly string _startId = $"start_{Guid.NewGuid():N}";
        private readonly string _endId = $"end_{Guid.NewGuid():N}";


        [Parameter]
        public Func<T, IRange<T>, RangeLength<T>, int, SnapPolicy, T>? StepResolver { get; set; }
        
        private T ResolveStep(T v, int steps = 0, SnapPolicy policy = SnapPolicy.Nearest)
            => StepResolver?.Invoke(v, SizeRange, StepLength, steps, policy) ?? (steps == 0
                ? M.SnapToStep(v, SizeRange, StepLength, policy)
                : M.AddSteps(v, StepLength, steps));

        private T Snap(T v, SnapPolicy policy = SnapPolicy.Nearest) => ResolveStep(v, 0, policy);

        private T AddStepsLocal(T v, int steps) =>ResolveStep(v, steps);

        // ---------- Binding ----------
        [Parameter, SafeCategory("Data")] public IRange<T> Value { get; set; } = new MudExRange<T>(default, default);
        [Parameter] public EventCallback<IRange<T>> ValueChanged { get; set; }

        [Parameter, SafeCategory("Data")] public IRange<T> SizeRange { get; set; } = new MudExRange<T>(default, default);
        [Parameter] public EventCallback<IRange<T>> SizeRangeChanged { get; set; }

        // NEU: Step als Länge
        [Parameter, SafeCategory("Data")] public RangeLength<T> StepLength { get; set; } = new RangeLength<T>(0);

        // NEU: Min/Max-Länge des ausgewählten Wertes (optional)
        [Parameter, SafeCategory("Data")] public RangeLength<T>? MinLength { get; set; }
        [Parameter, SafeCategory("Data")] public RangeLength<T>? MaxLength { get; set; }

        [Parameter, SafeCategory("Behavior")] public bool Immediate { get; set; } = true;
        [Parameter, SafeCategory("Behavior")] public bool Disabled { get; set; }
        [Parameter, SafeCategory("Behavior")] public bool ReadOnly { get; set; }
        [Parameter, SafeCategory("Behavior")] public bool ShowInputs { get; set; } = true;
        [Parameter, SafeCategory("Behavior")] public bool AllowWholeRangeDrag { get; set; } = true;

        [Parameter, SafeCategory("Appearance")] public Color Color { get; set; } = Color.Primary;
        [Parameter, SafeCategory("Appearance")] public string? AriaLabelledBy { get; set; }

        [Parameter] public EventCallback<IRange<T>> OnChange { get; set; }
        [Parameter] public EventCallback<IRange<T>> OnInput { get; set; }

        private readonly IRangeMath<T> _defaultAdapter = RangeMathFactory.For<T>();
        private IRangeMath<T> M => MathAdapter ?? _defaultAdapter;


        private DragMode _dragMode = DragMode.None;
        private double _trackLeft;
        private double _trackWidth;
        private double _dragStartClientX;
        private (double start, double end) _dragStartRange;


        // Prozent (CSS-Variablen) ohne vorheriges Messen
        protected string _startPct => Math.Clamp(M.Percent(Value.Start, SizeRange), 0, 1).ToString(CultureInfo.InvariantCulture);
        protected string _endPct => Math.Clamp(M.Percent(Value.End, SizeRange), 0, 1).ToString(CultureInfo.InvariantCulture);

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
            // Validierungen
            var sizeSpan = M.Span(SizeRange);
            if (sizeSpan <= 0)
                return; // keine Korrektur möglich

            if (StepLength.Delta <= 0 || StepLength.Delta > sizeSpan)
            {
                // Schritt korrigieren (mind. 1 Raster, max. sizeSpan)
                var corr = Math.Clamp(StepLength.Delta, 1e-12, sizeSpan);
                StepLength = new RangeLength<T>(corr);
            }

            if (MinLength.HasValue && MaxLength.HasValue && Math.Abs(MinLength.Value.Delta) > Math.Abs(MaxLength.Value.Delta))
            {
                // swap
                var tmp = MinLength;
                MinLength = MaxLength;
                MaxLength = tmp;
            }

            // Value normalisieren: snap + clamp + min/max Länge
            var snapped = M.SnapRange(Value, SizeRange, StepLength); 
            var bounded = M.EnforceMinMaxLength(snapped, SizeRange, MinLength, MaxLength, Thumb.End);
            if (bounded.Start.CompareTo(bounded.End) > 0)
                bounded = new MudExRange<T>(bounded.End, bounded.Start);
            Value = bounded;
        }

        // ------------- Measuring -------------

        private async Task MeasureAsync()
        {
            var rect = await JsReference.InvokeAsync<DomRect>("measureTrack");
            _trackLeft = rect.Left; _trackWidth = Math.Max(1, rect.Width);
        }

        // ------------- Interaction -------------

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
            _dragStartRange = (M.ToDouble(Value.Start), M.ToDouble(Value.End));
            await MeasureAsync();
            await JsReference.InvokeVoidAsync("startCapture");
        }

        private async Task OnTrackPointerDown(PointerEventArgs e)
        {
            if (Disabled || ReadOnly) return;
            await MeasureAsync();

            var pct = Math.Clamp((e.ClientX - _trackLeft) / _trackWidth, 0, 1);
            var target = Snap(M.Lerp(SizeRange, pct));
            var dStart = Math.Abs(M.ToDouble(target) - M.ToDouble(Value.Start));
            var dEnd = Math.Abs(M.ToDouble(target) - M.ToDouble(Value.End));

            if (dStart <= dEnd) await SetStartAsync(target, true);
            else await SetEndAsync(target, true);
        }

        [JSInvokable]
        public async Task OnPointerMove(double clientX)
        {
            if (Disabled || ReadOnly) return;
            if (_dragMode == DragMode.None) return;

            if (_dragMode == DragMode.WholeRange)
            {
                // Länge beibehalten, Range verschieben
                var deltaPx = clientX - _dragStartClientX;
                var deltaPct = deltaPx / _trackWidth;
                var total = M.Difference(SizeRange.Start, SizeRange.End);
                var delta = total * deltaPct;

                var span = _dragStartRange.end - _dragStartRange.start;
                var newStart = _dragStartRange.start + delta;
                var newEnd = newStart + span;

                // clamp in Size
                var minD = M.ToDouble(SizeRange.Start);
                var maxD = M.ToDouble(SizeRange.End);
                if (newStart < minD) { newStart = minD; newEnd = minD + span; }
                if (newEnd > maxD) { newEnd = maxD; newStart = maxD - span; }

                var s = M.FromDouble(newStart);
                var e = M.FromDouble(newEnd);

                var newRange = new MudExRange<T>(Snap(s), Snap(e)).Normalize();
                // Whole drag: Länge bleibt, also Min/Max-Länge ignorable
                Value = newRange;

                if (Immediate) await OnInput.InvokeAsync(Value);
                StateHasChanged();
                return;
            }

            // Thumb-Drag
            var pct = Math.Clamp((clientX - _trackLeft) / _trackWidth, 0, 1);
            var snapped = Snap(M.Lerp(SizeRange, pct));

            if (_dragMode == DragMode.StartThumb)
            {
                var clampedStart = M.Clamp(snapped, new MudExRange<T>(SizeRange.Start, Value.End));
                var r = M.EnforceMinMaxLength(new MudExRange<T>(clampedStart, Value.End),
                                                       SizeRange, MinLength, MaxLength, Thumb.Start);
                if (!Equals(r.Start, Value.Start) || !Equals(r.End, Value.End))
                {
                    Value = r;
                    if (Immediate) await OnInput.InvokeAsync(Value);
                    StateHasChanged();
                }
            }
            else if (_dragMode == DragMode.EndThumb)
            {
                var clampedEnd = M.Clamp(snapped, new MudExRange<T>(Value.Start, SizeRange.End));
                var r = M.EnforceMinMaxLength(new MudExRange<T>(Value.Start, clampedEnd),
                                                     SizeRange, MinLength, MaxLength, Thumb.End);
                if (!Equals(r.Start, Value.Start) || !Equals(r.End, Value.End))
                {
                    Value = r;
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
            var s = M.Clamp(Snap(v), new MudExRange<T>(SizeRange.Start, Value.End));
            var r = M.EnforceMinMaxLength(new MudExRange<T>(s, Value.End), SizeRange, MinLength, MaxLength, Thumb.Start);

            if (!Equals(r.Start, Value.Start) || !Equals(r.End, Value.End))
            {
                Value = r;
                if (commit) { await ValueChanged.InvokeAsync(Value); await OnChange.InvokeAsync(Value); }
                else if (Immediate) await OnInput.InvokeAsync(Value);
            }
        }

        private async Task SetEndAsync(T v, bool commit)
        {
            var e = M.Clamp(Snap(v), new MudExRange<T>(Value.Start, SizeRange.End));
            var r = M.EnforceMinMaxLength(new MudExRange<T>(Value.Start, e), SizeRange, MinLength, MaxLength, Thumb.End);

            if (!Equals(r.Start, Value.Start) || !Equals(r.End, Value.End))
            {
                Value = r;
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
                var target = home ? SizeRange.Start
                                  : end ? Value.End
                                        : AddStepsLocal(Value.Start, deltaSteps);
                await SetStartAsync(target, true);
            }
            else
            {
                var target = home ? Value.Start
                                  : end ? SizeRange.End
                                        : AddStepsLocal(Value.End, deltaSteps);
                await SetEndAsync(target, true);
            }
        }

    }
}
