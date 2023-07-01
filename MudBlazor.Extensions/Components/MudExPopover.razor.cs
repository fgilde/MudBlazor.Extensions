using BlazorJS.JsInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components
{
    /// <summary>
    /// A Popover can be used to display some content on top of another.
    /// </summary>
    public partial class MudExPopover : IMudExComponent, IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Injected service provider to use when retrieving services.
        /// </summary>
        [Inject]
        protected IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Gets the JSRuntime.
        /// </summary>
        public IJSRuntime JsRuntime => ServiceProvider.GetService<IJSRuntime>();

        /// <summary>
        /// The selectors to ignore when handling blur events.
        /// </summary>
        [Parameter, SafeCategory("Behavior")]
        public string SelectorsForIgnoreBlur { get; set; }

        /// <summary>
        /// Indicates whether the event should be disposed.
        /// </summary>
        [Parameter, SafeCategory("Behavior")]
        public bool DisposeEvent { get; set; } = false;

        /// <summary>
        /// The animation type.
        /// </summary>
        [Parameter, SafeCategory("Appearance")]
        public AnimationType Animation { get; set; } = AnimationType.SlideIn;

        /// <summary>
        /// The animation timing function.
        /// </summary>
        [Parameter, SafeCategory("Appearance")]
        public AnimationTimingFunction AnimationTimingFunction { get; set; }

        /// <summary>
        /// The dialog position.
        /// </summary>
        [Parameter, SafeCategory("Appearance")]
        public DialogPosition AnimationPosition { get; set; } = DialogPosition.TopCenter;

        /// <summary>
        /// Event delegate for handling blur events.
        /// </summary>
        [Parameter, SafeCategory("Behavior")]
        public EventCallback<PointerEventArgs> OnBlur { get; set; }

        /// <summary>
        /// Event delegate for handling open state changes.
        /// </summary>
        [Parameter, SafeCategory("Behavior")]
        public EventCallback<bool> OpenChanged { get; set; }


        private MudPopoverHandler Handler => Service.Handlers.FirstOrDefault(x => x.Fragment == ChildContent);
        private BlazorJSEventInterop<PointerEventArgs> _jsEvent;
        private string _classId = $"mud-ex-popover-{Guid.NewGuid()}";

        private string AnimationStyle() => Open
            ? $"animation: {new[] { Animation }.GetAnimationCssStyle(TimeSpan.FromMilliseconds(Duration), AnimationDirection.In, AnimationTimingFunction, AnimationPosition)}"
            : $"";


        /// <inheritdoc />
        protected override async Task OnInitializedAsync()
        {
            EnsureClassId();
            _jsEvent = new BlazorJSEventInterop<PointerEventArgs>(JsRuntime);
            await _jsEvent.OnBlur(OnFocusLeft, $".{_classId}", SelectorsForIgnoreBlur);
            await base.OnInitializedAsync();
        }

        /// <summary>
        /// Sets the component's parameters asynchronously.
        /// </summary>
        /// <param name="parameters">The parameter view.</param>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            bool oldOpenValue = Open;

            await base.SetParametersAsync(parameters);

            bool newOpenValue = Open;

            if (oldOpenValue != newOpenValue)
                await OnOpenChanged(newOpenValue);

        }

        /// <summary>
        /// Handles open state change event.
        /// </summary>
        /// <param name="newOpenValue">The new open value.</param>
        private async Task OnOpenChanged(bool newOpenValue)
        {
            await OpenChanged.InvokeAsync(newOpenValue);
        }

        protected override async Task OnParametersSetAsync()
        {
            EnsureClassId();
            await base.OnParametersSetAsync();
        }

        /// <summary>
        /// Gets the popover class.
        /// </summary>
        protected override string PopoverClass => $"{base.PopoverClass} {_classId}";

        /// <summary>
        /// Gets the popover styles.
        /// </summary>
        protected override string PopoverStyles => $"{base.PopoverStyles}{AnimationStyle()}";

        private void EnsureClassId()
        {
            if (string.IsNullOrEmpty(Class))
                Class = _classId;
            else if (!Class.Contains(_classId))
                Class += $" {_classId}";
        }

        /// <summary>
        /// Handles focus left event.
        /// </summary>
        /// <param name="arg">The pointer event args.</param>
        private Task OnFocusLeft(PointerEventArgs arg)
        {
            if (Open)
            {
                OnBlur.InvokeAsync(arg);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the inherited render fragment.
        /// </summary>
        private RenderFragment Inherited() => builder =>
        {
            base.BuildRenderTree(builder);
        };

        /// <summary>
        /// Disposes of the object.
        /// </summary>
        public ValueTask DisposeAsync()
        {
            // TODO: Ask MudBlazor Author if they can make DisposeAsync virtual
            DisposeCore();
            return base.DisposeAsync();
        }

        /// <summary>
        /// Disposes of the object.
        /// </summary>
        public void Dispose()
        {
            DisposeCore();
        }

        private void DisposeCore()
        {
            if (DisposeEvent && _jsEvent is not null)
            {
                _jsEvent.Dispose();
                _jsEvent = null;
            }
        }
    }
}
