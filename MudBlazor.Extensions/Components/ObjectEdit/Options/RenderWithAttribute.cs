using System.Linq.Expressions;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Attribute to specify how the property should be rendered inside a mud ex object edit.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public abstract class RenderWithBaseAttribute : System.Attribute
{
    /// <summary>
    /// Applies the attribute to the given ObjectEditPropertyMeta instance.
    /// </summary>
    public abstract ObjectEditPropertyMeta Apply(ObjectEditPropertyMeta meta);
}

[AttributeUsage(AttributeTargets.Property)]
internal class RenderWithAttribute<TComponent> : RenderWithBaseAttribute where TComponent : new()
{
    private readonly IDictionary<string, object> _attributes;
    //private string _valueFieldName;

    //public RenderWithAttribute(Dictionary<string, object> attributes = null) { }

    //public RenderWithAttribute(string valueField)
    //{
    //    _valueFieldName = valueField;
    //}

    //public RenderWithAttribute(Expression<Func<TComponent, object>> valueField, Dictionary<string, object> attributes = null)
    //    : this(valueField.GetMemberName(), attributes)
    //{ }

    /// <summary>
    /// Applies the attribute to the given ObjectEditPropertyMeta instance.
    /// </summary>
    public override ObjectEditPropertyMeta Apply(ObjectEditPropertyMeta meta)
    {
        return meta.RenderWith(typeof(TComponent), _attributes);
    }
}

[AttributeUsage(AttributeTargets.Property)]
internal class RenderWithAttribute<TComponent, TPropertyType, TFieldType> : RenderWithBaseAttribute where TComponent : new()
{
    private readonly Expression<Func<TComponent, TFieldType>> _valueField;
    private readonly object _instanceOrOptions;
    private readonly Func<TPropertyType, TFieldType> _toFieldTypeConverter;
    private readonly Func<TFieldType, TPropertyType> _toPropertyTypeConverter;

    public RenderWithAttribute(Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options)
    {
        _valueField = valueField;
        _instanceOrOptions = options;
    }

    public RenderWithAttribute(Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes)
    {
        _valueField = valueField;
        _instanceOrOptions = instanceForAttributes;
    }

    public RenderWithAttribute(Expression<Func<TComponent, TFieldType>> valueField, Func<TPropertyType, TFieldType> toFieldTypeConverter, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null)
    {
        _valueField = valueField;
        _toFieldTypeConverter = toFieldTypeConverter;
        _toPropertyTypeConverter = toPropertyTypeConverter;
    }

    public RenderWithAttribute(Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options, Func<TPropertyType, TFieldType> toFieldTypeConverter, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null)
    {
        _valueField = valueField;
        _instanceOrOptions = options;
        _toFieldTypeConverter = toFieldTypeConverter;
        _toPropertyTypeConverter = toPropertyTypeConverter;
    }

    public RenderWithAttribute(Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes, Func<TPropertyType, TFieldType> toFieldTypeConverter, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null)
    {
        _valueField = valueField;
        _instanceOrOptions = instanceForAttributes;
        _toFieldTypeConverter = toFieldTypeConverter;
        _toPropertyTypeConverter = toPropertyTypeConverter;
    }

    public override ObjectEditPropertyMeta Apply(ObjectEditPropertyMeta meta)
    {
        if (_valueField != null && _instanceOrOptions is Action<TComponent> _options && _toFieldTypeConverter != null)
        {
            return meta.RenderWith(_valueField, _options, _toFieldTypeConverter, _toPropertyTypeConverter);
        }

        if (_valueField != null && _instanceOrOptions is TComponent _instance && _toFieldTypeConverter != null)
        {
            return meta.RenderWith(_valueField, _instance, _toFieldTypeConverter, _toPropertyTypeConverter);
        }

        if (_valueField != null && _instanceOrOptions is Action<TComponent> options)
        {
            return meta.RenderWith(_valueField, options);
        }

        if (_valueField != null && _instanceOrOptions is TComponent instance)
        {
            return meta.RenderWith(_valueField, instance);
        }

        if (_valueField != null && _toFieldTypeConverter != null)
        {
            return meta.RenderWith(_valueField, _toFieldTypeConverter, _toPropertyTypeConverter);
        }

        return meta;
    }
}
