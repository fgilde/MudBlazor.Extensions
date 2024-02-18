using BlazorJS.JsInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Interop;
using MudBlazor.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component to Edit Icon values
/// </summary>
public partial class MudExIconPicker
{
    private double _iconCardWidth = 136.88; // single icon card width including margins
    private float _iconCardHeight = 144; // single icon card height including margins
    private IStringLocalizer<MudExIconPicker> FallbackLocalizer => ServiceProvider.GetService<IStringLocalizer<MudExIconPicker>>();
    private bool _rendered;
    private string _propertyName;
    private int _cardsPerRow = 3;
    private string _elementId = "picker" + Guid.NewGuid().ToString()[..8];
    
    [Inject] IResizeObserver ResizeObserver { get; set; }
    
    //[Inject] private IKeyInterceptorFactory KeyInterceptorFactory { get; set; }
    //private IEventListener _throttledEventManager;
    //[Inject] IEventListenerFactory ThrottledEventManagerFactory { get; set; }


    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> to be used for dependency injection.
    /// </summary>
    [Inject]
    protected IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// Gets the <see cref="IStringLocalizer"/> to be used for localizing strings.
    /// </summary>
    protected IStringLocalizer LocalizerToUse => Localizer ?? FallbackLocalizer;

    /// <summary>
    /// Gets or sets the <see cref="IStringLocalizer"/> to be used for localizing strings.
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public IStringLocalizer Localizer { get; set; }

    /// <summary>
    /// The width of the picker in pixels.
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public int PickerWidth { get; set; } = 500;

    /// <summary>
    /// Set this to true to disable the list view
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool DisableList { get; set; }

    /// <summary>
    /// The height of the picker in pixels.
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public int PickerHeight { get; set; } = 600;

    /// <summary>
    /// Gets or sets the variant filter.
    /// </summary>
    [Parameter, SafeCategory("Filtering")]
    public Variant FilterVariant { get; set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
                return;
            AdornmentIcon = value;
            _value = value;
            _propertyName = MudExSvg.SvgPropertyNameForValue(value, IconTypes) ?? value;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Gets or sets the callback method when the value of the picker is changed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the callback method when the property name for the value of the is changed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public EventCallback<string> PropertyNameChanged { get; set; }

    /// <summary>
    /// Available Icons in this Picker. If not set they will be filled automatically from <see cref="IconTypes"/>
    /// </summary>
    [IgnoreOnObjectEdit]
    [Parameter, SafeCategory("Appearance")]
    public IDictionary<string, string> Available { get; set; }

    /// <summary>
    /// Here you can specify the class of icons you want to show. Default is MudBlazor.Icons
    /// But if you have for example installed MudBlazor.MaterialDesignIcons you can also pass typeof(MaterialDesignIcons) 
    /// </summary>
    [IgnoreOnObjectEdit]
    [Parameter, SafeCategory("Appearance")]
    public Type[] IconTypes { get; set; } = { typeof(Icons), typeof(MudExIcons) };


    /// <summary>
    /// Gets or sets the filter for the icons.
    /// </summary>
    [Parameter, SafeCategory("Filtering")]
    public string Filter { get; set; }


    /// <summary>
    /// Gets or sets a value indicating whether to delay the value change of the picker until the picker is closed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool DelayValueChangeToPickerClose { get; set; }

    /// <summary>
    /// Gets or sets the auto close behavior.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AutoCloseOnSelect { get; set; } = false;

    /// <summary>
    /// The visible text value is by default the property path if possible set this to true to show always the value
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AlwaysShowValue { get; set; }


    /// <summary>
    /// Gets or sets the string name of the property for the value.
    /// </summary>
    [IgnoreOnObjectEdit][Parameter, SafeCategory("Data")]
    public string PropertyName
    {
        get => _propertyName;
        set
        {
            if (_propertyName == value)
                return;
            _propertyName = value;
            _value = MudExSvg.SvgPropertyValueForName(_propertyName, IconTypes);
            StateHasChanged();
        }
    }

    /// <summary>
    /// Localizes a text
    /// </summary>
    public string TryLocalize(string text, params object[] args) => LocalizerToUse.TryLocalize(text, args);

    private string OnGetValueFromName(string name) => Value = MudExSvg.SvgPropertyValueForName(name, IconTypes);

    private string PropertyNameLabel => !string.IsNullOrEmpty(PropertyName) && IsValueName(PropertyName) ? PropertyName : TryLocalize("Custom");

    private string OnSet(string value)
    {
        RaiseChangedIf();
        return Text = AlwaysShowValue ? value : MudExSvg.SvgPropertyNameForValue(_value, IconTypes); 
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Editable = true;
        Converter = new DefaultConverter<string>();
        Converter.GetFunc = OnGetValueFromName;
        Converter.SetFunc = OnSet;
        Class = string.IsNullOrEmpty(Class) || !Class.Contains("mud-ex-icon-picker") ? $"{Class} mud-ex-icon-picker" : Class;
        base.OnInitialized();
    }


    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (PickerVariant is PickerVariant.Static)
        {
            EnsureAvailable();
            SetCardsPerRow();
        }
        Text = AlwaysShowValue ? _value : MudExSvg.SvgPropertyNameForValue(_value, IconTypes);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender || !_rendered)
        {
            _rendered = true;
            await ResizeObserver.Observe(killZone);
            ResizeObserver.OnResized += OnResized;
          //  _jsEvent = await new BlazorJSEventInterop<PointerEventArgs>(ServiceProvider.GetService<IJSRuntime>()).OnBlur(LostFocusAsync, $".{_elementId}");
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private string GetClass()
    {
        return MudExCssBuilder.Default.AddClass(_elementId).Build();
    }


    private void EnsureAvailable()
    {
        if (Available is not { Count: not 0 })
        {
            Available = MudExSvg.GetAllSvgProperties(IconTypes);
        }
    }
    private List<Dictionary<string, string>> SelectedIcons
    {
        get
        {
            EnsureAvailable();
            return string.IsNullOrWhiteSpace(Filter)
                ? GetVirtualizedIcons(Available)
                : GetVirtualizedIcons(Available.Where(m => m.Key.Contains(Filter, StringComparison.OrdinalIgnoreCase)));
        }
    }

    private List<Dictionary<string, string>> GetVirtualizedIcons(IEnumerable<KeyValuePair<string, string>> iconList) 
        => iconList.Chunk(Math.Max(_cardsPerRow, 1)).Select(row => row.ToDictionary(pair => pair.Key, pair => pair.Value)).ToList();

    private string GetKillZoneStyle() => "height:65vh;width:100%;position:sticky;top:0px;";

    private async void OnResized(IDictionary<ElementReference, BoundingClientRect> changes)
    {
        SetCardsPerRow();
        await InvokeAsync(StateHasChanged);
    }
    private void SetCardsPerRow()
    {
        var width = ResizeObserver.GetWidth(killZone);
        if (width <= 0)
            width = PickerWidth;
        _cardsPerRow = Convert.ToInt32(width / _iconCardWidth);
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnPickerClosed()
    {
        if (DelayValueChangeToPickerClose)
            RaiseChanged();
    }
    
    /// <inheritdoc />
    protected override void OnPickerOpened()
    {
        EnsureAvailable();
        SetCardsPerRow();
        base.OnPickerOpened();
    }

    /// <inheritdoc />
    protected override async Task StringValueChanged(string value)
    {
        if (!_rendered)
            return;
        Touched = true;
        Value = IsValueName(value) ? Converter.Get(value) : value;

        await SetTextAsync(AlwaysShowValue ? Value : PropertyName, false);

        RaiseChangedIf();
    }


    //private bool canClose = true;
    
    //protected override void OnClosed()
    //{
    //    if (!canClose && PickerVariant == PickerVariant.Dialog)
    //    {
    //        IsOpen = true;
    //        return;
    //    }
    //    base.OnClosed();
    //}



    //public override ValueTask FocusAsync()
    //{
    //    if (PickerVariant == PickerVariant.Dialog)
    //        canClose = false;
    //    return base.FocusAsync();
    //}

    //private ElementReference element;
    //private async Task LostFocusAsync(PointerEventArgs arg)
    //{
    //    var js = ServiceProvider.GetService<IJSRuntime>();
    //    if (PickerVariant == PickerVariant.Dialog && IsOpen && !(await js.IsWithin(arg, element)))
    //    {
    //        canClose = true;
    //    }
    //}

    private void RaiseChanged()
    {
        ValueChanged.InvokeAsync(Value);
        PropertyNameChanged.InvokeAsync(PropertyName);
    }


    private void RaiseChangedIf()
    {
        if (DelayValueChangeToPickerClose && IsOpen)
            return;
        RaiseChanged();
    }



    private void CloseIf()
    {
        if (AutoCloseOnSelect)
        {
           // canClose = true;
            Close();
        }
    }

    private async Task Select(string value)
    {
        Value = value;
        await SetTextAsync(AlwaysShowValue ? Value : PropertyName, false);

        RaiseChangedIf();
        CloseIf();
    }


    private static bool IsValueName(string s) => !s.StartsWith("<");


    private Task OnMouseDown(MouseEventArgs arg)
    {
        
        return Task.CompletedTask;
    }
}