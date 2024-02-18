using System.Reflection;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// ObjectEditPropertyMetaOf is a generic version of ObjectEditPropertyMeta, used to store metadata about the model object.
/// </summary>
/// <typeparam name="T">Type of the main model (not the property type)</typeparam>
public sealed class ObjectEditPropertyMetaOf<T> : ObjectEditPropertyMeta
{
    /// <summary>
    /// ModelType is the type of the main model 
    /// </summary>
    public Type ModelType { get; }

    /// <summary>
    /// MainEditMeta is the main ObjectEditMeta instance that contains this property.
    /// </summary>
    public new ObjectEditMeta<T> MainEditMeta => base.MainEditMeta as ObjectEditMeta<T>;

    /// <summary>
    /// Creates a new ObjectEditPropertyMetaOf instance.
    /// </summary>
    public ObjectEditPropertyMetaOf(PropertyInfo propertyInfo, object referenceHolder) : base(propertyInfo, referenceHolder)
    {
        ModelType = typeof(T);
    }

    /// <summary>
    /// Creates a new ObjectEditPropertyMetaOf instance.
    /// </summary>
    public ObjectEditPropertyMetaOf(ObjectEditMeta mainEditMeta, PropertyInfo propertyInfo, object referenceHolder) : base(mainEditMeta, propertyInfo, referenceHolder)
    {
        ModelType = typeof(T);
    }
}