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
        [Parameter] public PropertyFilterMode FilterMode { get; set; } = PropertyFilterMode.Toggleable;

        [Parameter]
        public string Filter { get; set; }

        [Parameter] public bool Immediate { get; set; } = true;
        [Parameter] public bool Clearable { get; set; } = true;
        [Parameter] public string SearchIcon { get; set; } = Icons.Material.Outlined.Search;
        [Parameter] public Color SearchButtonColor { get; set; } = Color.Inherit;
        [Parameter] public EventCallback<string> FilterChanged { get; set; }

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
            _searchBoxBlur = true;
            _searchActive = false;
            Task.Delay(300).ContinueWith(t => _searchBoxBlur = false);
            return Task.CompletedTask;
        }

        private Task OnFilterChanged(string arg)
        {
            Filter = arg;
            return FilterChanged.InvokeAsync(arg);
        }

    }
}
