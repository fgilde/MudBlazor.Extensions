using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.CompilerServices;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Render data for a component to be used in the ObjectEdit component.
/// </summary>
/// <typeparam name="TPropertyType">Property type</typeparam>
/// <typeparam name="TFieldType">Filed type in the component</typeparam>
public sealed class RenderData<TPropertyType, TFieldType> : RenderData
{
    internal override Type FieldType => typeof(TFieldType);
    internal override Type PropertyType => typeof(TPropertyType);

    /// <summary>
    /// Converter function to convert the property type to the field type.
    /// </summary>
    public Func<TPropertyType, TFieldType> ToFieldTypeConverterFn { get; set; }
    
    /// <summary>
    /// Converter function to convert the field type to the property type.
    /// </summary>
    public Func<TFieldType, TPropertyType> ToPropertyTypeConverterFn { get; set; }

    /// <summary>
    /// Constructor for the RenderData class.
    /// </summary>
    public RenderData(string valueField, Type componentType, IDictionary<string, object> attributes = null)
        : base(componentType, attributes)
    {
        ValueField = valueField;
    }

    /// <summary>
    /// Updates the conditional settings for the current render data.
    /// </summary>
    public override void UpdateConditionalSettings<TModel>(TModel model)
    {
        base.UpdateConditionalSettings(model);
        // fallback if condition not match model, we try property value instead
        Conditions?.Where(c => c.modelType == typeof(TPropertyType)).Apply(condition => (condition.condition(ValueWrapper.Value) ? condition.trueFn : condition.falseFn)(this));
        Conditions?.Where(c => c.modelType == typeof(TFieldType)).Apply(condition => (condition.condition(ToFieldTypeConverterFn(ValueWrapper.Value)) ? condition.trueFn : condition.falseFn)(this));
    }

    /// <summary>
    /// Converts the field type to the property type.
    /// </summary>
    public override object ConvertToPropertyValue(object value) => ToPropertyTypeConverterFn((TFieldType) value);

    /// <summary>
    /// Initializes the value binding for the current render data.
    /// </summary>
    public override IRenderData InitValueBinding(ObjectEditPropertyMeta propertyMeta, Func<Task> valueChanged)
    {
        base.InitValueBinding(propertyMeta, valueChanged);
        ToFieldTypeConverterFn ??= v => v == null ? default : v.MapTo<TFieldType>();
        ToPropertyTypeConverterFn ??= v => v == null ? default : v.MapTo<TPropertyType>();

        ValueWrapper = propertyMeta.As<TPropertyType>();
        Attributes.AddOrUpdate(ValueField, ToFieldTypeConverterFn(ValueWrapper.Value));
        AttachValueChanged(propertyMeta.ReferenceHolder, valueChanged);
        return this;
    }

    /// <summary>
    /// Adds the given attributes to the current render data if the condition returns true.
    /// </summary>
    public RenderData<TPropertyType, TFieldType> AddAttributesIf(Func<TPropertyType, bool> condition, bool overwriteExisting, params KeyValuePair<string, object>[] attributes)
    {
        AddAttributesIf<TPropertyType>(condition, overwriteExisting, attributes);
        return this;
    }

    /// <summary>
    /// Reference to the value field.
    /// </summary>
    public PropertyValueWrapper<TPropertyType> ValueWrapper { get; set; }
    
    /// <summary>
    /// Sets the value of the current property for this render data.
    /// </summary>
    public override void SetValue(object value) => ValueWrapper.Value = ToPropertyTypeConverterFn((TFieldType)value);

    internal bool AttachValueChanged(object eventTarget, Func<Task> valueChanged)
    {
        var eventKeyName = $"{ValueField}Changed";
        if (DisableValueBinding || !IsValidParameterAttribute(eventKeyName, null))
            return false;
        Attributes.AddOrUpdate(eventKeyName, RuntimeHelpers.TypeCheck(
            EventCallback.Factory.Create(
                eventTarget,
                EventCallback.Factory.CreateInferred(
                    eventTarget, async x =>
                    {
                        ValueWrapper.Value = ToPropertyTypeConverterFn(x);
                        if (valueChanged != null)
                            await valueChanged.Invoke();
                    },
                    ToFieldTypeConverterFn(ValueWrapper.Value)
                )
            )
        ));
        return true;
    }
}
