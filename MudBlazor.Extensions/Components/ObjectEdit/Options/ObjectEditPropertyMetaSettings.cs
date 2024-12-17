using System.Reflection;
using Microsoft.Extensions.Localization;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Property settings for an ObjectEditPropertyMeta instance.
/// </summary>
public class ObjectEditPropertyMetaSettings
{
    /// <summary>
    /// Default Function to resolve the label for the property.
    /// </summary>
    public static Func<PropertyInfo, string> DefaultLabelResolverFn = info => info.Name;
    
    /// <summary>
    /// Default Function to resolve the description for the property.
    /// </summary>
    public static Func<PropertyInfo, string> DefaultDescriptionResolverFn = _ => string.Empty;
    private readonly List<(Type modelType, Func<object, bool> condition, Action<ObjectEditPropertyMetaSettings> trueFn, Action<ObjectEditPropertyMetaSettings> falseFn)> _conditions = new();

    /// <summary>
    /// Owner of the settings.
    /// </summary>
    public ObjectEditPropertyMeta Owner { get; internal set; }

    /// <summary>
    /// Creates a new instance of ObjectEditPropertyMetaSettings.
    /// </summary>
    public ObjectEditPropertyMetaSettings(ObjectEditPropertyMeta owner)
    {
        Owner = owner;
    }

    /// <summary>
    /// Order of the property.
    /// </summary>
    public int Order { get; set; } = int.MaxValue;
    
    /// <summary>
    /// Localizer to use for the property.
    /// </summary>
    public IStringLocalizer Localizer { get; set; }
    
    /// <summary>
    /// Determines if the property is editable.
    /// </summary>
    public bool IsEditable { get; set; }

    /// <summary>
    /// Should have Auto focus.
    /// </summary>
    public bool AutoFocus { get; set; }

    /// <summary>
    /// Determines if the property is ignored.
    /// </summary>
    public bool Ignored { get; set; }
    
    /// <summary>
    /// Determines if the property is ignored on export.
    /// </summary>
    public bool IgnoreOnExport { get; set; }
    
    /// <summary>
    /// Determines if the property is ignored on import.
    /// </summary>
    public bool IgnoreOnImport { get; set; }
    
    /// <summary>
    /// Settings for resetting the property.
    /// </summary>
    public PropertyResetSettings ResetSettings { get; set; }
    
    /// <summary>
    /// Should the property be validated by a separate validation component.
    /// </summary>
    public bool ValidationComponent { get; set; }
    
    /// <summary>
    /// Behaviour of the label.
    /// </summary>
    public LabelBehaviour LabelBehaviour { get; set; } = LabelBehaviour.DefaultComponentLabeling;
    
    /// <summary>
    /// Function to resolve the label for the property.
    /// </summary>
    public Func<PropertyInfo, string> LabelResolverFn { get; set; } = DefaultLabelResolverFn;
    
    /// <summary>
    /// function to resolve the description for the property.
    /// </summary>
    public Func<PropertyInfo, string> DescriptionResolverFn { get; set; } = DefaultDescriptionResolverFn;

    internal string LabelFor(IStringLocalizer localizer)
        => TextFor(localizer, LabelResolverFn);

    internal string DescriptionFor(IStringLocalizer localizer)
        => TextFor(localizer, DescriptionResolverFn);

    private string TextFor(IStringLocalizer localizer, Func<PropertyInfo, string> resolverFunc)
    {
        var localizerToUse = Localizer ?? localizer;
        var res = resolverFunc(Owner.PropertyInfo);
        return localizerToUse != null ? localizerToUse[res] : res;
    }

    internal void UpdateConditionalSettings<TModel>(TModel model)
    {
        _conditions.Where(c => c.modelType == typeof(TModel)).Apply(condition => (condition.condition(model) ? condition.trueFn : condition.falseFn)(this));
        _conditions.Where(c => c.modelType == typeof(ObjectEditPropertyMeta)).Apply(condition => (condition.condition(Owner) ? condition.trueFn : condition.falseFn)(this));
        _conditions.Where(c => c.modelType == typeof(PropertyInfo)).Apply(condition => (condition.condition(Owner.PropertyInfo) ? condition.trueFn : condition.falseFn)(this));
    }

    /// <summary>
    /// Adds a condition to the settings.
    /// </summary>
    public void AddCondition<TModel>(Func<TModel, bool> condition, Action<ObjectEditPropertyMetaSettings> trueFn, Action<ObjectEditPropertyMetaSettings> falseFn) 
        => _conditions.Add((typeof(TModel), model => condition((TModel)model), trueFn, falseFn));
}

/// <summary>
/// Label behaviour for a property.
/// </summary>
public enum LabelBehaviour
{
    /// <summary>
    /// Default component labeling.
    /// </summary>
    DefaultComponentLabeling,
    
    /// <summary>
    /// Separate label component only.
    /// </summary>
    SeparateLabelComponentOnly,
    
    /// <summary>
    /// Default component labeling and separate label component.
    /// </summary>
    Both,
    
    /// <summary>
    /// Render no label.
    /// </summary>
    NoLabel
}