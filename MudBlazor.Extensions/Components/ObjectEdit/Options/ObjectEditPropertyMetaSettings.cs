using System.Reflection;
using Microsoft.Extensions.Localization;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

public class ObjectEditPropertyMetaSettings
{
    public static Func<PropertyInfo, string> DefaultLabelResolverFn = info => info.Name;
    public static Func<PropertyInfo, string> DefaultDescriptionResolverFn = _ => string.Empty;
    private readonly List<(Type modelType, Func<object, bool> condition, Action<ObjectEditPropertyMetaSettings> trueFn, Action<ObjectEditPropertyMetaSettings> falseFn)> _conditions = new();

    public ObjectEditPropertyMeta Owner { get; internal set; }

    public ObjectEditPropertyMetaSettings(ObjectEditPropertyMeta owner)
    {
        Owner = owner;
    }

    public int Order { get; set; } = int.MaxValue;
    public IStringLocalizer Localizer { get; set; }
    public bool IsEditable { get; set; }
    public bool Ignored { get; set; }
    public PropertyResetSettings ResetSettings { get; set; }
    public LabelBehaviour LabelBehaviour { get; set; } = LabelBehaviour.DefaultComponentLabeling;
    public Func<PropertyInfo, string> LabelResolverFn { get; set; } = DefaultLabelResolverFn;
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
        => _conditions.Where(c => c.modelType == typeof(TModel)).Apply(condition => (condition.condition(model) ? condition.trueFn : condition.falseFn)(this));

    public void AddCondition<TModel>(Func<TModel, bool> condition, Action<ObjectEditPropertyMetaSettings> trueFn, Action<ObjectEditPropertyMetaSettings> falseFn) 
        => _conditions.Add((typeof(TModel), model => condition((TModel)model), trueFn, falseFn));
}

public enum LabelBehaviour
{
    DefaultComponentLabeling,
    SeparateLabelComponentOnly,
    Both,
    NoLabel
}