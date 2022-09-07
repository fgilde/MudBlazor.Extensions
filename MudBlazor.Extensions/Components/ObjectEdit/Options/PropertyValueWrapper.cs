using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

public class PropertyValueWrapper<T> : PropertyValueWrapper
{
    private readonly bool _mapInsteadOfCast;

    internal PropertyValueWrapper(ObjectEditPropertyMeta meta, bool mapInsteadOfCast) : base(typeof(T), meta)
    {
        _mapInsteadOfCast = mapInsteadOfCast;
    }

    public new T Value
    {
        get => !_mapInsteadOfCast ? (T) Meta.Value : Meta.Value.MapTo<T>();
        set => Meta.Value = _mapInsteadOfCast ? value.MapTo(Meta.PropertyInfo.PropertyType) : value;
    }
}

public class PropertyValueWrapper
{
    private readonly Type _propertyType;
    protected readonly ObjectEditPropertyMeta Meta;

    internal PropertyValueWrapper(Type propertyType, ObjectEditPropertyMeta meta)
    {
        _propertyType = propertyType;
        Meta = meta;
    }

    public object Value
    {
        get => Meta.Value.MapTo(_propertyType);
        set => Meta.Value = value.MapTo(Meta.PropertyInfo.PropertyType);
    }
}