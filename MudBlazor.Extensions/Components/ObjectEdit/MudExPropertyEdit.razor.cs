using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Utilities;
using Newtonsoft.Json;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// Editor for a property of an object. Used internally inside the MudExObjectEdit
/// </summary>
public partial class MudExPropertyEdit
{

    /// <summary>
    /// If this is set all properties will be readonly depending on the value otherwise the property settings for meta configuration will be used
    /// </summary>
    [Parameter] public bool? ReadOnlyOverwrite { get; set; }

    /// <summary>
    /// If this is true, the component adds the value if possible to url and reads it automatically if its present in Url
    /// </summary>
    [Parameter] public bool StoreAndReadValueFromUrl { get; set; }

    /// <summary>
    /// A prefix that is used for the key in the url
    /// </summary>
    [Parameter]
    public string UriPrefixForKey { get; set; } = string.Empty;

    /// <summary>
    /// Set this to true to display whole property path as title
    /// </summary>
    [Parameter] public bool ShowPathAsTitle { get; set; }

    /// <summary>
    /// Gets or sets the IsLoading state
    /// </summary>
    [Parameter] public bool IsLoading { get; set; }

    /// <summary>
    /// Creates a skeleton while loading if this is true
    /// </summary>
    [Parameter] public bool AutoSkeletonOnLoad { get; set; }

    /// <summary>
    /// ActiveFiler string
    /// </summary>
    [Parameter] public string ActiveFilterTerm { get; set; }

    /// <summary>
    /// Settings for the property reset behavior
    /// </summary>
    [Parameter] public PropertyResetSettings PropertyResetSettings { get; set; }

    /// <summary>
    /// PropertyMeta from ObjectEdit where Value is present in
    /// </summary>
    [Parameter][IgnoreOnObjectEdit] public ObjectEditPropertyMeta PropertyMeta { get; set; }

    /// <summary>
    /// EventCallback if value changed
    /// </summary>
    [Parameter] public EventCallback<ObjectEditPropertyMeta> PropertyValueChanged { get; set; }

    /// <summary>
    /// If this setting is true and no RenderData is available there will no fallback in a textedit rendered
    /// </summary>
    [Parameter] public bool DisableFieldFallback { get; set; }

    /// <summary>
    /// The rendered component
    /// </summary>
    public DynamicComponent Editor
    {
        get => _editor;
        private set => _editor = value;
    }


    private object _valueBackup;
    private bool _urlSetDone;

    private DynamicComponent _editor;
    //private Expression<Func<TPropertyType>> CreateFieldForExpression<TPropertyType>()
    //    => Check.TryCatch<Expression<Func<TPropertyType>>, Exception>(() => Expression.Lambda<Func<TPropertyType>>(Expression.Property(Expression.Constant(PropertyMeta.ReferenceHolder, PropertyMeta.ReferenceHolder.GetType()), PropertyMeta.PropertyInfo)));

    private Expression<Func<TPropertyType>> CreateFieldForExpression<TPropertyType>()
    {
        try
        {
            return Expression.Lambda<Func<TPropertyType>>(Expression.Property(
                Expression.Constant(PropertyMeta.ReferenceHolder, PropertyMeta.ReferenceHolder.GetType()),
                PropertyMeta.PropertyInfo));
        }
        catch
        {
            return null;
        }
    }

    private object CreateFieldForExpressionPropertyType()
    {
        MethodInfo createFieldForExpression = GetType().GetMethod(nameof(CreateFieldForExpression), BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo genericMethod = createFieldForExpression?.MakeGenericMethod(PropertyMeta.ComponentFieldType);
        return genericMethod?.Invoke(this, Array.Empty<object>()) ?? CreateFieldForExpression<string>();
    }


    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && PropertyMeta != null)
        {
            _valueBackup = await GetBackupAsync(PropertyMeta.Value);
            if (PropertyMeta is { RenderData: RenderData renderData })
            {
                renderData.ComponentReference = Editor?.Instance;
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    /// <inheritdoc />
    protected override Task OnFinishedRenderAsync()
    {
        if (!_urlSetDone)
        {
            SetValueFromUrlIf();
            _urlSetDone = true;
        }

        return base.OnFinishedRenderAsync();
    }


    /// <inheritdoc />
    protected override void OnInitialized()
    {
        RenderDataDefaults.AddRenderDataProvider(ServiceProvider);
        base.OnInitialized();
    }

    private IDictionary<string, object> GetPreparedAttributes()
    {
        var result = PropertyMeta.RenderData
            .InitValueBinding(PropertyMeta, OnPropertyValueChanged)
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.For), CreateFieldForExpressionPropertyType())
            .TrySetAttributeIfAllowed(nameof(MudExSelect<string>.ForMultiple), CreateFieldForExpressionPropertyType())
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.Label), () => PropertyMeta.Settings.LabelFor(LocalizerToUse), PropertyMeta.Settings.LabelBehaviour is LabelBehaviour.Both or LabelBehaviour.DefaultComponentLabeling)
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.HelperText), () => PropertyMeta.Settings.DescriptionFor(LocalizerToUse))
            .TrySetAttributeIfAllowed(nameof(Class), () => Class)
            .TrySetAttributeIfAllowed(nameof(Style), () => Style)
            .TrySetAttributeIfAllowed(nameof(Localizer), Localizer)
            .TrySetAttributeIfAllowed(nameof(RenderKey), RenderKey)
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.ReadOnly), () => !PropertyMeta.Settings.IsEditable);

        if (ReadOnlyOverwrite.HasValue)
            result.TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.ReadOnly), ReadOnlyOverwrite.Value);

        return result.Attributes;
    }

    private Task OnPropertyValueChanged()
    {
        AddValueToUrlIf();
        return RaiseValueChanged();
    }

    /// <summary>
    /// Raises the ValueChanged Event
    /// </summary>
    public Task RaiseValueChanged() => PropertyValueChanged.InvokeAsync(PropertyMeta);

    private void SetValueFromUrlIf()
    {
        var navigation = Get<NavigationManager>();
        if (!StoreAndReadValueFromUrl || navigation is null)
            return;

        try
        {
            var uriBuilder = new UriBuilder(navigation.Uri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            var key = UriPrefixForKey + PropertyMeta.PropertyName;
            if (query.AllKeys.Contains(key))
            {
                var value = query[key];
                if (value != null)
                {
                    var deserializeObject = JsonConvert.DeserializeObject(value, PropertyMeta.PropertyInfo.PropertyType);
                    var propertyMetaValue = deserializeObject.MapTo(PropertyMeta.PropertyInfo.PropertyType);
                    PropertyMeta.Value = propertyMetaValue;
                    Invalidate();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void AddValueToUrlIf()
    {
        var navigation = Get<NavigationManager>();
        if (!StoreAndReadValueFromUrl || !IsFullyRendered || navigation is null)
            return;

        try
        {
            var uriBuilder = new UriBuilder(navigation.Uri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            var valueForUrl = JsonConvert.SerializeObject(PropertyMeta.Value);
            var key = UriPrefixForKey + PropertyMeta.PropertyName;
            query[key] = valueForUrl;
            uriBuilder.Query = query.ToString() ?? string.Empty;

            navigation.NavigateTo(uriBuilder.ToString(), false);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private PropertyResetSettings GetResetSettings()
        => PropertyMeta.Settings?.ResetSettings ?? (PropertyResetSettings ??= new PropertyResetSettings());

    /// <summary>
    /// Renders the editor with a CustomRenderer
    /// </summary>
    protected void RenderAs(RenderTreeBuilder renderTreeBuilder, ObjectEditPropertyMeta meta)
        => meta.RenderData.CustomRenderer.Render(renderTreeBuilder, this, meta);

    private async Task<object> GetBackupAsync(object value)
    {
        try
        {
            var t = PropertyMeta.PropertyInfo.PropertyType;
            if (value == null)
                return GetDefault(t);
            if (t.IsValueType || t.IsPrimitive || t == typeof(string))
                return value;
            if (t == typeof(MudColor))
                return new MudColor(value.ToString());
            
            return await value.MapToAsync(t);
        }
        catch
        {
            return value;
        }
    }

    private static object GetDefault(Type type)
    {
        if (type == typeof(string))
            return string.Empty;
        if (type == typeof(MudColor))
            return new MudColor(0, 0, 0, 0);
        return type.CreateInstance();
    }

    /// <summary>
    /// Resets the Editor
    /// </summary>
    public Task ResetAsync() => ClearOrResetAsync(true);

    /// <summary>
    /// Clears the Editor
    /// </summary>
    public Task ClearAsync() => ClearOrResetAsync(false);

    private async Task ClearOrResetAsync(bool reset)
    {
        try
        {
            if (PropertyMeta.PropertyInfo.CanWrite)
            {
                PropertyMeta.Value = reset
                    ? await GetBackupAsync(_valueBackup)
                    : PropertyMeta.RenderData.ConvertToPropertyValue(
                        GetDefault(PropertyMeta.PropertyInfo.PropertyType));

                CallStateHasChanged();
            }
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// Refreshes the UI
    /// </summary>
    public void Invalidate(bool useRefresh = false)
    {
        if (useRefresh) 
            Refresh();
        else
            CallStateHasChanged();
    }

    /// <summary>
    /// Returns the current Value independent of the PropertyMeta for example if binding is disabled
    /// </summary>
    public object GetCurrentValue()
        => Editor.Instance?.GetType().GetProperty(PropertyMeta.RenderData.ValueField)?.GetValue(Editor.Instance);

    /// <summary>
    /// Sets a Value independent of the PropertyMeta for example if binding is disabled
    /// </summary>
    public void SetValue(object value)
    {
        PropertyMeta.RenderData.SetValue(value);
        //Editor.Instance?.GetType().GetProperty(PropertyMeta.RenderData.ValueField)?.SetValue(Editor.Instance, value);
    }

    private string Title()
        => ShowPathAsTitle ? PropertyMeta.PropertyName.Replace(".", " > ") : null;
}