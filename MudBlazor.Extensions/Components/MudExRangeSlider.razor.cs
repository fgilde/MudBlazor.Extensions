using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Contracts;
using Nextended.Core.Extensions;
using Nextended.Core.Types;
using Nextended.Core.Types.Ranges.Math;
using System.Globalization;

namespace MudBlazor.Extensions.Components
{
    public partial class MudExRangeSlider<T> where T : struct, IComparable<T>
    {
        [Parameter] public IRangeMath<T>? MathAdapter { get; set; }

        private ElementReference _trackRef;
        private readonly string _startId = $"start_{Guid.NewGuid():N}";
        private readonly string _endId = $"end_{Guid.NewGuid():N}";

        [Parameter]
        public Func<T, IRange<T>, RangeLength<T>, int, SnapPolicy, T>? StepResolver { get; set; }

        private T ResolveStep(T v, int steps = 0, SnapPolicy policy = SnapPolicy.Nearest)
            => StepResolver?.Invoke(v, SizeRange, StepLength, steps, policy)
               ?? (steps == 0 ? M.SnapToStep(v, SizeRange, StepLength, policy)
                              : M.AddSteps(v, StepLength, steps));

        private T Snap(T v, SnapPolicy policy = SnapPolicy.Nearest)
            => ResolveStep(v, 0, policy);

        private T AddStepsLocal(T v, int steps)
            => ResolveStep(v, steps);

        [Parameter, SafeCategory("Appearance")]
        public SliderOrientation Orientation { get; set; } = SliderOrientation.Horizontal;

        private bool IsHorizontal
            => Orientation is SliderOrientation.Horizontal;

        [Parameter, SafeCategory("Appearance")]
        public bool IsInverted { get; set; }

        [Parameter, SafeCategory("Appearance")]
        public Size Size { get; set; } = Size.Medium;

        [Parameter, SafeCategory("Appearance")]
        public MudExColor ThumbColor { get; set; } = MudExColor.Primary;

        [Parameter, SafeCategory("Appearance")]
        public MudExColor TrackColor { get; set; } = new MudExColor("#0000001f");

        [Parameter, SafeCategory("Appearance")]
        public MudExColor SelectionColor { get; set; } = MudExColor.Primary;

        // Templates
        [Parameter, SafeCategory("Templates")]
        public RenderFragment? TrackTemplate { get; set; }

        [Parameter, SafeCategory("Templates")]
        public RenderFragment? SelectionTemplate { get; set; }

        [Parameter, SafeCategory("Templates")]
        public RenderFragment? ThumbStartTemplate { get; set; }

        [Parameter, SafeCategory("Templates")]
        public RenderFragment? ThumbEndTemplate { get; set; }

        private bool HasTrackTemplate => TrackTemplate != null;
        private bool HasSelectionTemplate => SelectionTemplate != null;
        private bool HasThumbStartTemplate => ThumbStartTemplate != null;
        private bool HasThumbEndTemplate => ThumbEndTemplate != null;

        private double P(T v)
            => Math.Clamp(M.Percent(v, SizeRange), 0, 1);

        private double PForCss(T v)
            => IsInverted ? 1 - P(v) : P(v);

        protected string _startPct
            => PForCss(Value.Start).ToString(CultureInfo.InvariantCulture);

        protected string _endPct
            => PForCss(Value.End).ToString(CultureInfo.InvariantCulture);

        protected string _minPct
            => Math.Min(PForCss(Value.Start), PForCss(Value.End)).ToString(CultureInfo.InvariantCulture);

        protected string _maxPct
            => Math.Max(PForCss(Value.Start), PForCss(Value.End)).ToString(CultureInfo.InvariantCulture);

        protected string SizeClass
            => $"mud-ex-size-{Size.ToString().ToLower()}";

        protected string TrackStyle
            => $"--p-start:{_startPct}; --p-end:{_endPct}; --p-min:{_minPct}; --p-max:{_maxPct}; " +
               $"--ex-track-color:{TrackColor.ToCssStringValue()}; " +
               $"--ex-thumb-color:{ThumbColor.ToCssStringValue()}; " +
               $"--ex-selection-color:{SelectionColor.ToCssStringValue()};";

        // ---------- Binding ----------
        [Parameter, SafeCategory("Data")]
        public IRange<T> Value { get; set; } = new MudExRange<T>(default, default);

        [Parameter]
        public EventCallback<IRange<T>> ValueChanged { get; set; }

        [Parameter, SafeCategory("Data")]
        public IRange<T> SizeRange { get; set; } = new MudExRange<T>(default, default);

        [Parameter]
        public EventCallback<IRange<T>> SizeRangeChanged { get; set; }

        [Parameter, SafeCategory("Data")]
        public RangeLength<T> StepLength { get; set; } = new RangeLength<T>(0);

        [Parameter, SafeCategory("Data")]
        public RangeLength<T>? MinLength { get; set; }

        [Parameter, SafeCategory("Data")]
        public RangeLength<T>? MaxLength { get; set; }

        [Parameter, SafeCategory("Behavior")]
        public bool Immediate { get; set; } = true;

        [Parameter, SafeCategory("Behavior")]
        public bool Disabled { get; set; }

        [Parameter, SafeCategory("Behavior")]
        public bool ReadOnly { get; set; }

        [Parameter, SafeCategory("Behavior")]
        public bool ShowInputs { get; set; } = true;

        [Parameter, SafeCategory("Behavior")]
        public bool AllowWholeRangeDrag { get; set; } = true;

        [Parameter, SafeCategory("Appearance")]
        public string? AriaLabelledBy { get; set; }

        [Parameter]
        public EventCallback<IRange<T>> OnChange { get; set; }

        [Parameter]
        public EventCallback<IRange<T>> OnInput { get; set; }

        private IRangeMath<T> M
        {
            get => MathAdapter ?? field;
        } = RangeMathFactory.For<T>();

        private DragMode _dragMode = DragMode.None;
        private double _trackStartCoord;
        private double _trackLength;
        private double _dragStartClientPrimary;
        private (double start, double end) _dragStartRange;

        private string StartString
            => Value.Start.ToString() ?? string.Empty;

        private string EndString
            => Value.End.ToString() ?? string.Empty;

        private string SizeRangeStringStart
            => SizeRange.Start.ToString() ?? string.Empty;

        private string SizeRangeStringEnd
            => SizeRange.End.ToString() ?? string.Empty;

        public override object[] GetJsArguments()
            => new object[] { ElementReference, _trackRef, CreateDotNetObjectReference() };

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
                await JsReference.InvokeVoidAsync("init");
        }

        protected override void OnParametersSet()
        {
            var sizeSpan = M.Span(SizeRange);
            if (sizeSpan <= 0) return;

            if (StepLength.Delta <= 0 || StepLength.Delta > sizeSpan)
            {
                var corr = Math.Clamp(StepLength.Delta, 1e-12, sizeSpan);
                StepLength = new RangeLength<T>(corr);
            }

            if (MinLength.HasValue && MaxLength.HasValue
                && Math.Abs(MinLength.Value.Delta) > Math.Abs(MaxLength.Value.Delta))
            {
                (MinLength, MaxLength) = (MaxLength, MinLength);
            }

            var s = Snap(Value.Start);
            var e = Snap(Value.End);
            var snapped = new MudExRange<T>(s, e).Normalize();
            var bounded = M.EnforceMinMaxLength(snapped, SizeRange, MinLength, MaxLength, Thumb.End);

            if (bounded.Start.CompareTo(bounded.End) > 0)
                bounded = new MudExRange<T>(bounded.End, bounded.Start);

            Value = bounded;
        }

        // ------------- Measuring -------------
        private async Task MeasureAsync()
        {
            var rect = await JsReference.InvokeAsync<DomRect>("measureTrack");
            if (IsHorizontal)
            {
                _trackStartCoord = rect.Left;
                _trackLength = Math.Max(1, rect.Width);
            }
            else
            {
                _trackStartCoord = rect.Top;
                _trackLength = Math.Max(1, rect.Height);
            }
        }

        private double PctFromClient(double clientX, double clientY)
        {
            var client = IsHorizontal ? clientX : clientY;
            var rawPct = Math.Clamp((client - _trackStartCoord) / _trackLength, 0, 1);
            return IsInverted ? (1 - rawPct) : rawPct;
        }

        // ------------- Interaction -------------
        private double GetClientPrimary(PointerEventArgs e)
            => IsHorizontal ? e.ClientX : e.ClientY;

        private async Task OnRangeDragPointerDown(PointerEventArgs e)
        {
            if (!AllowWholeRangeDrag || Disabled || ReadOnly) return;

            _dragMode = DragMode.WholeRange;
            _dragStartClientPrimary = GetClientPrimary(e);
            _dragStartRange = (M.ToDouble(Value.Start), M.ToDouble(Value.End));

            await MeasureAsync();
            await JsReference.InvokeVoidAsync("startCapture");
        }

        private async Task OnTrackPointerDown(PointerEventArgs e)
        {
            if (Disabled || ReadOnly) return;

            await MeasureAsync();
            var pct = PctFromClient(e.ClientX, e.ClientY);
            var target = Snap(M.Lerp(SizeRange, pct));

            var dStart = Math.Abs(M.ToDouble(target) - M.ToDouble(Value.Start));
            var dEnd = Math.Abs(M.ToDouble(target) - M.ToDouble(Value.End));

            if (dStart <= dEnd)
                await SetStartAsync(target, true);
            else
                await SetEndAsync(target, true);
        }

        [JSInvokable]
        public async Task OnPointerMove(double clientX, double clientY)
        {
            if (Disabled || ReadOnly) return;
            if (_dragMode == DragMode.None) return;

            if (_dragMode == DragMode.WholeRange)
            {
                var curClient = IsHorizontal ? clientX : clientY;
                var deltaPx = curClient - _dragStartClientPrimary;
                var deltaPct = deltaPx / _trackLength;
                var total = M.Difference(SizeRange.Start, SizeRange.End);
                var signedDelta = total * (IsInverted ? -deltaPct : deltaPct);

                var span = _dragStartRange.end - _dragStartRange.start;
                var newStart = _dragStartRange.start + signedDelta;
                var newEnd = newStart + span;

                var minD = M.ToDouble(SizeRange.Start);
                var maxD = M.ToDouble(SizeRange.End);

                if (newStart < minD)
                {
                    newStart = minD;
                    newEnd = minD + span;
                }
                if (newEnd > maxD)
                {
                    newEnd = maxD;
                    newStart = maxD - span;
                }

                var s = Snap(M.FromDouble(newStart));
                var e = Snap(M.FromDouble(newEnd));
                var newRange = new MudExRange<T>(M.Clamp(s, SizeRange), M.Clamp(e, SizeRange)).Normalize();
                newRange = (MudExRange<T>)M.EnforceMinMaxLengthSymmetric(newRange, SizeRange, MinLength, MaxLength);

                Value = newRange;
                if (Immediate) await OnInput.InvokeAsync(Value);
                StateHasChanged();
                return;
            }

            var pct = PctFromClient(clientX, clientY);
            var snapped = Snap(M.Lerp(SizeRange, pct));

            if (_dragMode == DragMode.StartThumb)
            {
                var clampedStart = M.Clamp(snapped, new MudExRange<T>(SizeRange.Start, Value.End));
                var r = M.EnforceMinMaxLength(new MudExRange<T>(clampedStart, Value.End), SizeRange, MinLength, MaxLength, Thumb.Start);

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
                var r = M.EnforceMinMaxLength(new MudExRange<T>(Value.Start, clampedEnd), SizeRange, MinLength, MaxLength, Thumb.End);

                if (!Equals(r.Start, Value.Start) || !Equals(r.End, Value.End))
                {
                    Value = r;
                    if (Immediate) await OnInput.InvokeAsync(Value);
                    StateHasChanged();
                }
            }
        }

        private async Task OnThumbPointerDownAsync(PointerEventArgs e, Thumb which)
        {
            if (Disabled || ReadOnly) return;

            _dragMode = which == Thumb.Start ? DragMode.StartThumb : DragMode.EndThumb;
            await MeasureAsync();
            await JsReference.InvokeVoidAsync("startCapture");
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
                if (commit)
                {
                    await ValueChanged.InvokeAsync(Value);
                    await OnChange.InvokeAsync(Value);
                }
                else if (Immediate)
                    await OnInput.InvokeAsync(Value);
            }
        }

        private async Task SetEndAsync(T v, bool commit)
        {
            var e = M.Clamp(Snap(v), new MudExRange<T>(Value.Start, SizeRange.End));
            var r = M.EnforceMinMaxLength(new MudExRange<T>(Value.Start, e), SizeRange, MinLength, MaxLength, Thumb.End);

            if (!Equals(r.Start, Value.Start) || !Equals(r.End, Value.End))
            {
                Value = r;
                if (commit)
                {
                    await ValueChanged.InvokeAsync(Value);
                    await OnChange.InvokeAsync(Value);
                }
                else if (Immediate)
                    await OnInput.InvokeAsync(Value);
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