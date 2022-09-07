using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Utilities;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public partial class MudExPropertyEdit
{
    [Inject] private IServiceProvider _serviceProvider { get; set; }
    
    [Parameter] public string Class { get; set; }
    [Parameter] public string ActiveFilterTerm { get; set; }
    [Parameter] public PropertyResetSettings PropertyResetSettings { get; set; }
    [Parameter] public ObjectEditPropertyMeta PropertyMeta { get; set; }
    [Parameter] public EventCallback<ObjectEditPropertyMeta> PropertyValueChanged { get; set; }
    [Parameter] public IStringLocalizer Localizer { get; set; }
    [Parameter] public bool DisableFieldFallback { get; set; }
    
    private Expression<Func<string>> CreateFieldForExpression() 
        => Check.TryCatch<Expression<Func<string>>, Exception>(() => Expression.Lambda<Func<string>>(Expression.Property(Expression.Constant(PropertyMeta.ReferenceHolder, PropertyMeta.ReferenceHolder.GetType()), PropertyMeta.PropertyInfo)));

    private object valueBackup;
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
            valueBackup = GetBackup(PropertyMeta.Value);
        base.OnAfterRender(firstRender);
    }

    protected override void OnInitialized()
    {
        RenderDataDefaults.AddRenderDataProvider(_serviceProvider);
        base.OnInitialized();
    }

    private IDictionary<string, object> GetPreparedAttributes()
    {
        return PropertyMeta.RenderData
            .InitValueBinding(PropertyMeta, RaisePropertyValueChanged) // () => ValueChanged.InvokeAsync()
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.For), CreateFieldForExpression)
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.Label), () => PropertyMeta.Settings.LabelFor(Localizer), PropertyMeta.Settings.LabelBehaviour == LabelBehaviour.Both || PropertyMeta.Settings.LabelBehaviour == LabelBehaviour.DefaultComponentLabeling)
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.HelperText), () => PropertyMeta.Settings.DescriptionFor(Localizer))
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.Class), () => Class)
            .TrySetAttributeIfAllowed(nameof(MudBaseInput<string>.ReadOnly), () => !PropertyMeta.Settings.IsEditable).Attributes;
    }

    private Task RaisePropertyValueChanged() 
        => PropertyValueChanged.InvokeAsync(PropertyMeta);

    private PropertyResetSettings GetResetSettings() 
        => PropertyMeta.Settings?.ResetSettings ?? (PropertyResetSettings ??= new PropertyResetSettings());

    protected void RenderAs(RenderTreeBuilder renderTreeBuilder, ObjectEditPropertyMeta meta)
        => meta.RenderData.CustomRenderer.Render(renderTreeBuilder, this, meta);
    
    private object GetBackup(object value)
    {
        var t = PropertyMeta.PropertyInfo.PropertyType;
        if (value == null)
            return GetDefault(t);
        if (t.IsValueType || t.IsPrimitive || t == typeof(string))
            return value;
        
        var res = value.MapTo(t);
        return res;
    }

    private static object GetDefault(Type type)
    {
        if (type == typeof(string))
            return string.Empty;
        if (type == typeof(MudColor))
            return new MudColor(0, 0, 0, 0);
        return type.CreateInstance();
    }

    public Task ResetAsync()
    {
        Check.TryCatch<Exception>(() =>
        {
            if (PropertyMeta.PropertyInfo.CanWrite)
                PropertyMeta.Value = GetBackup(valueBackup);
        });
        StateHasChanged();
        return Task.CompletedTask;
    }

    public void Invalidate()
    {
        StateHasChanged();
    }
}