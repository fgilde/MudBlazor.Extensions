using System.Linq.Expressions;
using System.Reflection;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

public class RenderData : IRenderData
{
    #region Static Factory Methods

    public static RenderData<TPropertyType, TFieldType> For<TComponent, TPropertyType, TFieldType>(
        Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options,
        Func<TPropertyType, TFieldType> toFieldTypeConverter = null,
        Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => new(valueField.GetMemberName(), typeof(TComponent), DictionaryHelper.GetValuesDictionary(options, true))
        {
            ToFieldTypeConverterFn = toFieldTypeConverter, ToPropertyTypeConverterFn = toPropertyTypeConverter
        };

    public static RenderData<TPropertyType, TFieldType> For<TComponent, TPropertyType, TFieldType>(
        Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes,
        Func<TPropertyType, TFieldType> toFieldTypeConverter = null,
        Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => new(valueField.GetMemberName(), typeof(TComponent),
            DictionaryHelper.GetValuesDictionary(instanceForAttributes, true))
        {
            ToFieldTypeConverterFn = toFieldTypeConverter, ToPropertyTypeConverterFn = toPropertyTypeConverter
        };

    public static RenderData<TPropertyType, TFieldType> For<TComponent, TPropertyType, TFieldType>(
        Expression<Func<TComponent, TFieldType>> valueField,
        Func<TPropertyType, TFieldType> toFieldTypeConverter = null,
        Func<TFieldType, TPropertyType> toPropertyTypeConverter = null)
        => new(valueField.GetMemberName(), typeof(TComponent))
        {
            ToFieldTypeConverterFn = toFieldTypeConverter, ToPropertyTypeConverterFn = toPropertyTypeConverter
        };

    public static RenderData<TPropertyType, TPropertyType> For<TComponent, TPropertyType>(
        Expression<Func<TComponent, TPropertyType>> valueField, Action<TComponent> options) where TComponent : new()
        => new(valueField.GetMemberName(), typeof(TComponent), DictionaryHelper.GetValuesDictionary(options, true));

    public static RenderData<TPropertyType, TPropertyType> For<TComponent, TPropertyType>(
        Expression<Func<TComponent, TPropertyType>> valueField, TComponent instanceForAttributes)
        where TComponent : new()
        => new(valueField.GetMemberName(), typeof(TComponent),
            DictionaryHelper.GetValuesDictionary(instanceForAttributes, true));

    public static RenderData<TPropertyType, TPropertyType> For<TComponent, TPropertyType>(
        Expression<Func<TComponent, TPropertyType>> valueField)
        => new(valueField.GetMemberName(), typeof(TComponent));

    public static RenderData For<TComponent>(Action<TComponent> options) where TComponent : new()
        => For(typeof(TComponent), DictionaryHelper.GetValuesDictionary(options, true));

    public static RenderData For<TComponent>(IDictionary<string, object> attributes = null)
        => For(typeof(TComponent), attributes);

    public static RenderData For(Type componentType,
        IDictionary<string, object> attributes = null) => new(componentType, attributes);
    public static RenderData For(ICustomRenderer customRenderer) => new(null) {CustomRenderer = customRenderer};

    #endregion
    public IRenderData Wrapper { get; set; }
    public Type ComponentType { get; set; }
    public IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    public ICustomRenderer CustomRenderer { get; set; }
    public virtual IRenderData InitValueBinding(ObjectEditPropertyMeta propertyMeta, Func<Task> valueChanged) => this;
    public bool IsValidParameterAttribute(string key, object value)
    {
        var propertyInfo = ComponentType?.GetProperty(key, BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo != null && value != null)
            return propertyInfo.PropertyType.IsInstanceOfType(value);
        return propertyInfo != null;
    }

    public IRenderData TrySetAttributeIfAllowed(string key, Func<object> valueFn, bool condition = true) => TrySetAttributeIfAllowed(key, valueFn(), condition);
    protected List<(Type modelType, Func<object, bool> condition, Action<IRenderData> trueFn, Action<IRenderData> falseFn)> _conditions;
    public IDictionary<string, object> ValidAttributes => Attributes.Where(kvp => IsValidParameterAttribute(kvp.Key, kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    public RenderData(Type componentType, IDictionary<string, object> attributes = null)
    {
        ComponentType = componentType;
        SetAttributes(attributes);
    }

    internal virtual Type FieldType => null;
    internal virtual Type PropertyType => null;

    public IRenderData SetAttributes(IDictionary<string, object> attributes)
    {
        if (attributes != null)
        {
            Attributes = attributes;//attributes.ToDictionary(entry => entry.Key, entry => entry.Value);
            RemoveUnsafeAttributes();
        }

        return this;
    }

    public IRenderData AddAttributes(bool overwriteExisting, params KeyValuePair<string, object>[] attributes)
    {
        if (attributes?.Any() == true)
        {
            var attr = attributes; //attributes.ToDictionary(entry => entry.Key, entry => entry.Value);
            if (overwriteExisting)
                attr.Apply(a => Attributes.AddOrUpdate(a.Key, a.Value));
            else
                Attributes.AddRange(attr.Where(a => !Attributes.ContainsKey(a.Key)));
            RemoveUnsafeAttributes();
        }

        return this;
    }

    private void RemoveUnsafeAttributes()
    {
        if (Attributes.ContainsKey(nameof(MudComponentBase.UserAttributes)))
            Attributes.Remove(nameof(MudComponentBase.UserAttributes));
        Attributes.Where(k => !IsValidParameterAttribute(k.Key, k.Value)).Apply(k => Attributes.Remove(k));
    }

    public IRenderData TrySetAttributeIfAllowed(string key, object value, bool condition = true)
    {
        //if (condition && value != null && (!Attributes.ContainsKey(key) || Attributes[key] == null) && IsValidParameterAttribute(key))
        if (condition && value != null && IsValidParameterAttribute(key, value))
            Attributes.AddOrUpdate(key, value);
        
        return this;
    }
    
    public object Clone()
    {
        var result = MemberwiseClone() as RenderData;
        if (result != null && Attributes != null)
            result.Attributes = Attributes.ToDictionary(entry => entry.Key, entry => entry.Value);
        return result;
    }


    public virtual void UpdateConditionalSettings<TModel>(TModel model)
    {
        _conditions?.Where(c => c.modelType == typeof(TModel)).Apply(condition => (condition.condition(model) ? condition.trueFn : condition.falseFn)(this));
        _conditions?.Where(c => c.modelType == ComponentType).Apply(condition =>
        {
            var componentInstanceClone = Attributes.ToObject(ComponentType);
            (condition.condition(componentInstanceClone) ? condition.trueFn : condition.falseFn)(this);
        });
        Wrapper?.UpdateConditionalSettings(model);
    }

    public void AddCondition<TModel>(Func<TModel, bool> condition, Action<IRenderData> trueFn, Action<IRenderData> falseFn)
    {
        _conditions ??= new List<(Type modelType, Func<object, bool> condition, Action<IRenderData> trueFn, Action<IRenderData> falseFn)>();
        _conditions.Add((typeof(TModel), model => condition((TModel) model), trueFn, falseFn));
    }


    public IRenderData AddAttributesIf<TModel>(Func<TModel, bool> condition, bool overwriteExisting, params KeyValuePair<string, object>[] attributes)
    {
        var existing = Attributes.Where(pair => attributes.Any(a => a.Key == pair.Key)).ToArray();
        var toRemove = attributes.Where(pair => existing.All(e => e.Key != pair.Key)).ToArray();
        AddCondition(condition, rd => rd.AddAttributes(overwriteExisting, attributes), rd =>
        {
            rd.AddAttributes(overwriteExisting, existing);
            if (toRemove.Any())
                rd.Attributes.RemoveRange(toRemove);
        });
        return this;
    }

    public IRenderData SetAttributesIf<TModel>(Func<TModel, bool> condition, IDictionary<string, object> attributes)
    {
        var existing = Attributes?.Any() == true ? new Dictionary<string, object>(Attributes) : new Dictionary<string, object>();
        AddCondition(condition, rd => rd.SetAttributes(attributes), rd => rd.SetAttributes(existing));
        return this;
    }

    public virtual object ConvertToPropertyValue(object value) => value;
}