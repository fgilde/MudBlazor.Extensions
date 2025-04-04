﻿using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components
{
    /// <summary>
    /// Simple component to expand a Search field 
    /// </summary>
    public partial class MudExToggleableSearch
    {
        /// <summary>
        /// The short hint displayed in the input before the user enters a value.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Behavior)]
        public string Placeholder { get; set; } = "Filter";

        /// <summary>
        /// When true chips are placed to the right in the input, if null this is true when toggleable is true.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Behavior)]
        public bool? ChipsRight { get; set; }

        /// <summary>
        /// The short hint displayed in the input before the user enters a value.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Behavior)]
        public string Label { get; set; } = "Filter";

        /// <summary>
        /// Callback when the mouse enters a chip.
        /// </summary>
        [Parameter]
        public EventCallback<ChipMouseEventArgs<string>> OnChipMouseOver { get; set; }

        /// <summary>
        /// Callback when the mouse leaves a chip.
        /// </summary>
        [Parameter]
        public EventCallback<ChipMouseEventArgs<string>> OnChipMouseOut { get; set; }
        
        /// <summary>
        /// Sets the color of the chips.
        /// </summary>
        [Parameter, SafeCategory("Appearance")]
        public MudExColor ChipColor { get; set; } = MudExColor.Primary;

        /// <summary>
        /// Sets the visual variant of the chips.
        /// </summary>
        [Parameter, SafeCategory("Appearance")]
        public Variant ChipVariant { get; set; } = Variant.Filled;
        
        /// <summary>
        /// Set to true to allow multiple values
        /// </summary>
        [Parameter] public bool MultiSearch { get; set; }

        /// <summary>
        /// The Toggle mode
        /// </summary>
        [Parameter] public PropertyFilterMode FilterMode { get; set; } = PropertyFilterMode.Toggleable;

        /// <summary>
        /// The filter value itself
        /// </summary>
        [Parameter] public string Filter { get; set; }

        /// <summary>
        /// The filter value itself
        /// </summary>
        [Parameter] public List<string> Filters { get; set; }

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
        /// Event callback if filter changed
        /// </summary>
        [Parameter] public EventCallback<List<string>> FiltersChanged { get; set; }

        /// <summary>
        /// Event callback if filter input is toggled
        /// </summary>
        [Parameter] public EventCallback<bool> SearchActiveChanged { get; set; }

        /// <summary>
        /// Search is active
        /// </summary>
        [Parameter] public bool SearchActive { get; set; }

        /// <summary>
        ///  If true and search toggleable and open, search with closed on blur
        /// </summary>
        [Parameter] public bool AutoCloseOnBlur { get; set; } = true;

        /// <summary>
        /// Fired on the KeyUp event.
        /// </summary>
        [Parameter]
        public EventCallback<KeyboardEventArgs> OnKeyUp { get; set; }

        /// <summary>
        /// Returns true if the search is active
        /// </summary>
        public bool HasSearchActive => MultiSearch ? Filters?.Count > 0 || !string.IsNullOrWhiteSpace(Filter) : !string.IsNullOrWhiteSpace(Filter);

        private bool _searchBoxBlur;
        private MudTextField<string> _searchBox;
        private MudExTagField<string> _searchTagBox;
        private bool _isMouseOverChip;

        private string GetClassStr()
        {
            return MudExCssBuilder.From(Class)
                .AddClass("mud-ex-hide-inputlabel mud-ex-animate-all-properties mud-ex-property-filter")
                .AddClass("active", SearchActive || FilterMode == PropertyFilterMode.AlwaysVisible)
                .ToString();
        }
        
        private void ToggleSearchBox()
        {
            if (_searchBoxBlur)
                return;
            SearchActive = !SearchActive;
            SearchActiveChanged.InvokeAsync(SearchActive);
            if (SearchActive)            
                FocusAsync();           
        }

        private void FocusAsync()
        {
            _searchBox?.FocusAsync();
            _searchTagBox?.FocusAsync();
        }

        private Task FilterKeyPress(KeyboardEventArgs arg)
        {
            if (arg.Key == "Escape")
            {
                if (!string.IsNullOrWhiteSpace(Filter))
                    return OnFilterChanged(string.Empty);
                SearchActive = false;
                SearchActiveChanged.InvokeAsync(SearchActive);
            }
            return Task.CompletedTask;
        }

        private Task FilterBoxBlur(FocusEventArgs arg)
        {
            if (AutoCloseOnBlur && !_isMouseOverChip)
            {
                _searchBoxBlur = true;
                SearchActive = false;
                SearchActiveChanged.InvokeAsync(SearchActive);
                Task.Delay(300).ContinueWith(_ => _searchBoxBlur = false);
            }
            return Task.CompletedTask;
        }

        private Task OnFilterChanged(string arg)
        {
            Filter = arg;
            return FilterChanged.InvokeAsync(arg);
        }

        private Task OnFiltersChanged(List<string> arg)
        {
            Filters = arg;
            FocusAsync();
            return FiltersChanged.InvokeAsync(arg);
        }

        private string VisualizerStyleStr()
        {
            var right = ChipsRight ?? FilterMode == PropertyFilterMode.Toggleable;
            if(!right)
                return string.Empty;
            return MudExStyleBuilder.Default.WithHeight("20px")
                .WithDisplay("flex")
                .WithMarginTop("-40px")
                .WithWidth("100%")
                .WithJustifyContent("end")
                .WithZIndex(2)
                .Style;
        }
    }
}
