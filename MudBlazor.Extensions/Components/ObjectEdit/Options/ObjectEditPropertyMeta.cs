using MudBlazor.Extensions.Core;
using System.Reflection;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Metadata for a property of an object to be edited.
/// </summary>
public class ObjectEditPropertyMeta: IMudExStyleAppearance, IMudExClassAppearance
{
    /// <summary>
    /// Reference to the main edit meta data.
    /// </summary>
    public ObjectEditMeta MainEditMeta { get; }
    private object _value;
    private IRenderData _renderData;

    /// <summary>
    /// Style
    /// </summary>
    public string Style { get; set; }

    /// <summary>
    /// Class
    /// </summary>
    public string Class { get; set; }

    /// <summary>
    /// Parent property meta data.
    /// </summary>
    public ObjectEditPropertyMeta Parent { get; internal set; }
    
    /// <summary>
    /// Property name as full path.
    /// </summary>
    public string PropertyName => Parent != null ? $"{Parent.PropertyName}.{PropertyInfo.Name}" : PropertyInfo.Name;
    
    /// <summary>
    /// Children property metadata of this property.
    /// </summary>
    public List<ObjectEditPropertyMeta> Children { get; set; } = new();
    
    /// <summary>
    /// Returns true if the property has children.
    /// </summary>
    public bool HasChildren => Children?.Any() == true;
    internal PropertyValueWrapper<T> As<T>(bool map = false) => new(this, map);
    internal PropertyValueWrapper As(Type propertyType) => new(propertyType, this);
    
    /// <summary>
    /// Settings for the property metadata.
    /// </summary>
    public ObjectEditPropertyMetaSettings Settings { get; set; }
    
    /// <summary>
    /// Group information for the property metadata.
    /// </summary>
    public ObjectEditPropertyMetaGroupInfo GroupInfo { get; set; } = new();
    
    /// <summary>
    /// Original reference holder of the property.
    /// </summary>
    public object ReferenceHolder { get; private set; }
    
    /// <summary>
    /// Property information of the property.
    /// </summary>
    public PropertyInfo PropertyInfo { get; }

    internal Type ComponentFieldType => (_renderData as RenderData)?.FieldType ?? PropertyInfo.PropertyType;

    /// <summary>
    /// Creates a new instance of ObjectEditPropertyMeta.
    /// </summary>
    public ObjectEditPropertyMeta(PropertyInfo propertyInfo, object referenceHolder)
    {
        Settings = new ObjectEditPropertyMetaSettings(this);
        PropertyInfo = propertyInfo;
        Settings.IsEditable = propertyInfo?.CanWrite == true;
        ReferenceHolder = referenceHolder;
    }
    
    /// <summary>
    /// Creates a new instance of ObjectEditPropertyMeta.
    /// </summary>
    public ObjectEditPropertyMeta(ObjectEditMeta mainEditMeta, PropertyInfo propertyInfo, object referenceHolder) 
        : this(propertyInfo, referenceHolder)
    {
        MainEditMeta = mainEditMeta;
    }

    /// <summary>
    /// Value of the property.
    /// </summary>
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

    /// <summary>
    /// Render data for the property.
    /// </summary>
    public IRenderData RenderData
    {
        get => _renderData ??= RenderDataDefaults.GetRenderData(this);
        set => _renderData = value;
    }
    
    /// <summary>
    /// Returns true if the property should be rendered.
    /// </summary>
    public bool ShouldRender() => !Settings.Ignored && (!HasChildren || RenderData != null) && Parent?.RenderData == null;

    /// <summary>
    /// Updates all conditional settings for this property metadata.
    /// </summary>
    public void UpdateConditionalSettings<TModel>(TModel model)
    {
        Settings?.UpdateConditionalSettings(model);
        RenderData?.UpdateConditionalSettings(model);
    }

    /// <summary>
    /// Forces an update of the property metadata and the containing editor.
    /// </summary>
    public void ForceUpdate()
    {
        UpdateRequired?.Invoke(this);
    }

    public event Action<ObjectEditPropertyMeta> UpdateRequired;
}