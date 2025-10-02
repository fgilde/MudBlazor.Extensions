using System.Linq.Expressions;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class AttributeParameterAttribute: System.Attribute
{
    public string Key { get; }
    public object Value { get; }

    public AttributeParameterAttribute(string key, object value)
    {
        Key = key;
        Value = value;
    }

    public static IDictionary<string, object> GetAttributesFromEnumValue(Enum val)
    {
        return EnumHelper.GetCustomAttributes<AttributeParameterAttribute>(val, false)?.ToDictionary(a => a.Key, a => a.Value) ?? new Dictionary<string, object>();
    }
}

/// <summary>
/// Attribute to specify how the property should be rendered inside a mud ex object edit.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Enum | AttributeTargets.Field)]
public class RenderWithAttribute : System.Attribute
{
    public Type ComponentType { get; }
    public string TypeName { get; }

    public RenderWithAttribute(Type type)
    {
        ComponentType = type;
        TypeName = type.FullName;
    }

    /// <summary>
    /// Applies the attribute to the given ObjectEditPropertyMeta instance.
    /// </summary>
    public virtual ObjectEditPropertyMeta Apply(ObjectEditPropertyMeta meta)
    {
        return meta.RenderWith(ComponentType);
    }

    public static RenderWithAttribute GetRenderWithFromEnumValue(Enum val)
    {
        return EnumHelper.GetCustomAttributes<RenderWithAttribute>(val, false).FirstOrDefault();
    }
}

public class RenderWithAttribute<TComponent> : RenderWithAttribute where TComponent : new()
{
    private readonly IDictionary<string, object> _attributes;
    private string _valueFieldName;

    public RenderWithAttribute(string valueField, Dictionary<string, object> attributes = null) : this(valueField)
    { }

    public RenderWithAttribute(string valueField): base(typeof(TComponent))
    {
        _valueFieldName = valueField;
    }

    public RenderWithAttribute(Expression<Func<TComponent, object>> valueField, Dictionary<string, object> attributes = null)
        : this(valueField.GetMemberName(), attributes)
    { }

    /// <summary>
    /// Applies the attribute to the given ObjectEditPropertyMeta instance.
    /// </summary>
    public override ObjectEditPropertyMeta Apply(ObjectEditPropertyMeta meta)
    {
        return meta.RenderWith(typeof(TComponent), _attributes);
    }

    public RenderWithAttribute() : base(typeof(TComponent))
    {}
}


[AttributeUsage(AttributeTargets.Property)]
public class RenderWithAttribute<TComponent, TPropertyType, TFieldType> : RenderWithAttribute where TComponent : new()
{
    private readonly Expression<Func<TComponent, TFieldType>> _valueField;
    private readonly object _instanceOrOptions;
    private readonly Func<TPropertyType, TFieldType> _toFieldTypeConverter;
    private readonly Func<TFieldType, TPropertyType> _toPropertyTypeConverter;

    public RenderWithAttribute(Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options) : base(typeof(TComponent))
    {
        _valueField = valueField;
        _instanceOrOptions = options;
    }

    public RenderWithAttribute(Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes) : base(typeof(TComponent))
    {
        _valueField = valueField;
        _instanceOrOptions = instanceForAttributes;
    }

    public RenderWithAttribute(Expression<Func<TComponent, TFieldType>> valueField, Func<TPropertyType, TFieldType> toFieldTypeConverter, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) : base(typeof(TComponent))
    {
        _valueField = valueField;
        _toFieldTypeConverter = toFieldTypeConverter;
        _toPropertyTypeConverter = toPropertyTypeConverter;
    }

    public RenderWithAttribute(Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options, Func<TPropertyType, TFieldType> toFieldTypeConverter, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) : base(typeof(TComponent))
    {
        _valueField = valueField;
        _instanceOrOptions = options;
        _toFieldTypeConverter = toFieldTypeConverter;
        _toPropertyTypeConverter = toPropertyTypeConverter;
    }

    public RenderWithAttribute(Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes, Func<TPropertyType, TFieldType> toFieldTypeConverter, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) : base(typeof(TComponent))
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
