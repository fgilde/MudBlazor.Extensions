using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Utilities;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// Editor for a property of an object. Used internally inside the MudExObjectEdit
/// </summary>
public partial class MudExPropertyEdit
{
    [Parameter] public bool ShowPathAsTitle { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public bool AutoSkeletonOnLoad { get; set; }
    [Parameter] public string Class { get; set; }
    [Parameter] public string ActiveFilterTerm { get; set; }
    [Parameter] public PropertyResetSettings PropertyResetSettings { get; set; }
    [Parameter][IgnoreOnObjectEdit] public ObjectEditPropertyMeta PropertyMeta { get; set; }
    [Parameter] public EventCallback<ObjectEditPropertyMeta> PropertyValueChanged { get; set; }
    [Parameter] public bool DisableFieldFallback { get; set; }

    private DynamicComponent editor;
    private Expression<Func<TPropertyType>> CreateFieldForExpression<TPropertyType>()
        => Check.TryCatch<Expression<Func<TPropertyType>>, Exception>(() => Expression.Lambda<Func<TPropertyType>>(Expression.Property(Expression.Constant(PropertyMeta.ReferenceHolder, PropertyMeta.ReferenceHolder.GetType()), PropertyMeta.PropertyInfo)));

    private object CreateFieldForExpressionPropertyType()
    {
        MethodInfo createFieldForExpression = GetType().GetMethod(nameof(CreateFieldForExpression), BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo genericMethod = createFieldForExpression?.MakeGenericMethod(PropertyMeta.ComponentFieldType);
        return genericMethod?.Invoke(this, Array.Empty<object>()) ?? CreateFieldForExpression<string>();
    }

    private object valueBackup;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && PropertyMeta != null)
        {
            valueBackup = await GetBackupAsync(PropertyMeta.Value);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override void OnInitialized()
    {
        RenderDataDefaults.AddRenderDataProvider(ServiceProvider);
        base.OnInitialized();
    }

    private IDictionary<string, object> GetPreparedAttributes()
    {
        return PropertyMeta.RenderData
            .InitValueBinding(PropertyMeta, RaisePropertyValueChanged)
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.For), CreateFieldForExpressionPropertyType())
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.Label), () => PropertyMeta.Settings.LabelFor(LocalizerToUse), PropertyMeta.Settings.LabelBehaviour == LabelBehaviour.Both || PropertyMeta.Settings.LabelBehaviour == LabelBehaviour.DefaultComponentLabeling)
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.HelperText), () => PropertyMeta.Settings.DescriptionFor(LocalizerToUse))
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.Class), () => Class)
            .TrySetAttributeIfAllowed(nameof(Localizer), Localizer)
            .TrySetAttributeIfAllowed(nameof(RenderKey), RenderKey)
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.ReadOnly), () => !PropertyMeta.Settings.IsEditable).Attributes;
    }

    private Task RaisePropertyValueChanged()
        => PropertyValueChanged.InvokeAsync(PropertyMeta);

    private PropertyResetSettings GetResetSettings()
        => PropertyMeta.Settings?.ResetSettings ?? (PropertyResetSettings ??= new PropertyResetSettings());

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

    public Task ResetAsync() => ClearOrResetAsync(true);
    public Task ClearAsync() => ClearOrResetAsync(false);

    private async Task ClearOrResetAsync(bool reset)
    {
        try
        {
            if (PropertyMeta.PropertyInfo.CanWrite)
            {
                PropertyMeta.Value = reset
                    ? await GetBackupAsync(valueBackup)
                    : PropertyMeta.RenderData.ConvertToPropertyValue(GetDefault(PropertyMeta.PropertyInfo.PropertyType));
                
                StateHasChanged();
            }
        }
        catch
        {}
    }


    public void Invalidate(bool useRefresh = false)
    {
        if (useRefresh) 
            Refresh();
        else 
            StateHasChanged();
    }

    public object GetCurrentValue()
        => editor.Instance?.GetType()?.GetProperty(PropertyMeta.RenderData.ValueField)?.GetValue(editor.Instance);

    private string Title()
        => ShowPathAsTitle ? PropertyMeta.PropertyName.Replace(".", " > ") : null;
}