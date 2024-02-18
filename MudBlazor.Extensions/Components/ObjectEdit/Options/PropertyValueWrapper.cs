using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Wraps the value of a property to be edited, allowing for additional configuration through options.
/// </summary>
public class PropertyValueWrapper<T> : PropertyValueWrapper
{
    private readonly bool _mapInsteadOfCast;

    internal PropertyValueWrapper(ObjectEditPropertyMeta meta, bool mapInsteadOfCast) : base(typeof(T), meta)
    {
        _mapInsteadOfCast = mapInsteadOfCast;
    }

    /// <summary>
    /// Value of the property to be edited.
    /// </summary>
    public new T Value
    {
        get => !_mapInsteadOfCast ? (T) Meta.Value : Meta.Value.MapTo<T>();
        set => Meta.Value = _mapInsteadOfCast ? value.MapTo(Meta.PropertyInfo.PropertyType) : value;
    }
}

/// <summary>
/// Wraps the value of a property to be edited, allowing for additional configuration through options.
/// </summary>
public class PropertyValueWrapper
{
    private readonly Type _propertyType;
    
    /// <summary>
    /// Metadata of the property to be edited.
    /// </summary>
    protected readonly ObjectEditPropertyMeta Meta;

    internal PropertyValueWrapper(Type propertyType, ObjectEditPropertyMeta meta)
    {
        _propertyType = propertyType;
        Meta = meta;
    }
    
    /// <summary>
    /// Value of the property to be edited.
    /// </summary>
    public object Value
    {
        get => Meta.Value.MapTo(_propertyType);
        set => Meta.Value = value.MapTo(Meta.PropertyInfo.PropertyType);
    }
}