using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.ObjectEdit;

namespace MudBlazor.Extensions.Components
{
    /// <summary>
    /// Simple component to expand a Search field 
    /// </summary>
    public partial class MudExToggleableSearch
    {
        /// <summary>
        /// The Toggle mode
        /// </summary>
        [Parameter] public PropertyFilterMode FilterMode { get; set; } = PropertyFilterMode.Toggleable;

        /// <summary>
        /// The filter value itself
        /// </summary>
        [Parameter] public string Filter { get; set; }

        /// <summary>
        /// If true, the input will update the Value immediately on typing.
        /// If false, the Value is updated only on Enter.
        /// </summary>
        [Parameter] public bool Immediate { get; set; } = true;
        
        /// <summary>
        /// Show clear button
        /// </summary>
        [Parameter] public bool Clearable { get; set; } = true;
        
        /// <summary>
        /// Icon 
        /// </summary>
        [Parameter] public string SearchIcon { get; set; } = Icons.Material.Outlined.Search;
        
        /// <summary>
        /// Color for toggle button
        /// </summary>
        [Parameter] public Color SearchButtonColor { get; set; } = Color.Inherit;
        
        /// <summary>
        /// Event callback if filter changed
        /// </summary>
        [Parameter] public EventCallback<string> FilterChanged { get; set; }
        
        /// <summary>
        ///  If true and search toggleable and open, search with closed on blur
        /// </summary>
        [Parameter] public bool AutoCloseOnBlur { get; set; } = true;
        
        /// <summary>
        /// Fired on the KeyUp event.
        /// </summary>
        [Parameter]
        public EventCallback<KeyboardEventArgs> OnKeyUp { get; set; }

        private bool _searchBoxBlur;
        private bool _searchActive;
        private MudTextField<string> _searchBox;

        private void ToggleSearchBox()
        {
            if (_searchBoxBlur)
                return;
            _searchActive = !_searchActive;
            _searchBox.FocusAsync();
        }

        private Task FilterKeyPress(KeyboardEventArgs arg)
        {
            if (arg.Key == "Escape")
            {
                if (!string.IsNullOrWhiteSpace(Filter))
                    return OnFilterChanged(string.Empty);
                _searchActive = false;
            }
            return Task.CompletedTask;
        }

        private Task FilterBoxBlur(FocusEventArgs arg)
        {
            if (AutoCloseOnBlur)
            {
                _searchBoxBlur = true;
                _searchActive = false;
                Task.Delay(300).ContinueWith(t => _searchBoxBlur = false);
            }
            return Task.CompletedTask;
        }

        private Task OnFilterChanged(string arg)
        {
            Filter = arg;
            return FilterChanged.InvokeAsync(arg);
        }

    }
}
