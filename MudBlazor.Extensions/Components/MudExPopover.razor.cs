using BlazorJS.JsInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components
{
    /// <summary>
    /// A Popover can be used to display some content on top of another.
    /// </summary>
    public partial class MudExPopover : IMudExComponent, IAsyncDisposable
    {
        [Inject] protected IServiceProvider ServiceProvider { get; set; }
        public IJSRuntime JsRuntime => ServiceProvider.GetService<IJSRuntime>();

        //[Parameter] public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(650);
        [Parameter] public string SelectorsForIgnoreBlur { get; set; }

        [Parameter] public AnimationType Animation { get; set; } = AnimationType.SlideIn;
        [Parameter] public AnimationTimingFunction AnimationTimingFunction { get; set; }
        [Parameter] public DialogPosition AnimationPosition { get; set; } = DialogPosition.TopCenter;
        [Parameter] public EventCallback<PointerEventArgs> OnBlur { get; set; }
        [Parameter] public EventCallback<bool> OpenChanged { get; set; }

        private MudPopoverHandler Handler => Service.Handlers.FirstOrDefault(x => x.Fragment == ChildContent);
        private BlazorJSEventInterop<PointerEventArgs> _jsEvent;
        private string _classId = $"mud-ex-popover-{Guid.NewGuid()}";
        private string AnimationStyle() => Open 
            ? $"animation: {new[] { Animation }.GetAnimationCssStyle(TimeSpan.FromMilliseconds(Duration), AnimationDirection.In, AnimationTimingFunction, AnimationPosition)}" 
            : $"";


        protected override async Task OnInitializedAsync()
        {
            EnsureClassId();
            _jsEvent = new BlazorJSEventInterop<PointerEventArgs>(JsRuntime);
            await _jsEvent.OnBlur(OnFocusLeft, $".{_classId}", SelectorsForIgnoreBlur);
            await base.OnInitializedAsync();
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            bool oldOpenValue = Open;

            await base.SetParametersAsync(parameters);

            bool newOpenValue = Open;

            if (oldOpenValue != newOpenValue)
                await OnOpenChanged(newOpenValue);
            
        }

   
        private async Task OnOpenChanged(bool newOpenValue)
        {
            await OpenChanged.InvokeAsync(newOpenValue);
        }
        
        protected override async Task OnParametersSetAsync()
        {
            EnsureClassId();
            await base.OnParametersSetAsync();
            await Handler?.UpdateFragmentAsync(ChildContent ?? (_ => { }), this, $"{PopoverClass} {_classId}", $"{PopoverStyles}{AnimationStyle()}", Open);
        }

        private void EnsureClassId()
        {
            if (string.IsNullOrEmpty(Class))
                Class = _classId;
            else if(!Class.Contains(_classId))
                Class += $" {_classId}";
        }

        private Task OnFocusLeft(PointerEventArgs arg)
        {
            if (Open)
            {
                OnBlur.InvokeAsync(arg);
            }

            return Task.CompletedTask;
        }

        private RenderFragment Inherited() => builder =>
        {
            base.BuildRenderTree(builder);
        };


        public ValueTask DisposeAsync()
        {
            // TODO: Ask MudBlazor Author if they can make DisposeAsync virtual
            _jsEvent.Dispose();
            return base.DisposeAsync();
        }

    }
}
