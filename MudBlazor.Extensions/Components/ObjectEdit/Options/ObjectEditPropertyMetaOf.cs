using System.Reflection;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

public sealed class ObjectEditPropertyMetaOf<T> : ObjectEditPropertyMeta
{
    public Type ModelType { get; }


    public ObjectEditPropertyMetaOf(PropertyInfo propertyInfo, object referenceHolder) : base(propertyInfo, referenceHolder)
    {
        ModelType = typeof(T);
    }

    public ObjectEditPropertyMetaOf(ObjectEditMeta mainEditMeta, PropertyInfo propertyInfo, object referenceHolder) : base(mainEditMeta, propertyInfo, referenceHolder)
    {
        ModelType = typeof(T);
    }
}