using System.Reflection;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

public class ObjectEditPropertyMeta
{
    public ObjectEditMeta MainEditMeta { get; }
    private object _value;
    private IRenderData _renderData;
    public ObjectEditPropertyMeta Parent { get; internal set; }
    public string PropertyName => Parent != null ? $"{Parent.PropertyName}.{PropertyInfo.Name}" : PropertyInfo.Name;
    public List<ObjectEditPropertyMeta> Children { get; set; } = new();
    public bool HasChildren => Children?.Any() == true;
    internal PropertyValueWrapper<T> As<T>(bool map = false) => new(this, map);
    internal PropertyValueWrapper As(Type propertyType) => new(propertyType, this);
    public ObjectEditPropertyMetaSettings Settings { get; set; }
    public ObjectEditPropertyMetaGroupInfo GroupInfo { get; set; } = new();
    public object ReferenceHolder { get; private set; }
    public PropertyInfo PropertyInfo { get; }

    internal Type ComponentFieldType => (_renderData as RenderData)?.FieldType ?? PropertyInfo.PropertyType;

    public ObjectEditPropertyMeta(PropertyInfo propertyInfo, object referenceHolder)
    {
        Settings = new ObjectEditPropertyMetaSettings(this);
        PropertyInfo = propertyInfo;
        Settings.IsEditable = propertyInfo?.CanWrite == true;
        ReferenceHolder = referenceHolder;
    }
    public ObjectEditPropertyMeta(ObjectEditMeta mainEditMeta, PropertyInfo propertyInfo, object referenceHolder) 
        : this(propertyInfo, referenceHolder)
    {
        MainEditMeta = mainEditMeta;
    }

    public object Value
    {
        get => _value ??= PropertyInfo.GetValue(ReferenceHolder);
        set => PropertyInfo.SetValue(ReferenceHolder, _value = value);
    }

    internal ObjectEditPropertyMeta SetReferenceHolder(object referenceHolder)
    {
        ReferenceHolder = referenceHolder;
        _value = null;
        return this;
    }

    public IRenderData RenderData
    {
        get => _renderData ??= RenderDataDefaults.GetRenderData(this);
        set => _renderData = value;
    }
    
    public bool ShouldRender()
    {
        var res = !Settings.Ignored && (!HasChildren || RenderData != null) && Parent?.RenderData == null;
        return res;
    }

    public void UpdateConditionalSettings<TModel>(TModel model)
    {
        Settings?.UpdateConditionalSettings(model);
        RenderData?.UpdateConditionalSettings(model);
    }
}