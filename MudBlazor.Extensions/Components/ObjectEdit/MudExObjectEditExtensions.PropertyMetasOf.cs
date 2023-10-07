using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;


public static partial class MudExObjectEditExtensions
{
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent, TPropertyType, TFieldType>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, options, toFieldTypeConverter, toPropertyTypeConverter));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent, TPropertyType, TFieldType>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, instanceForAttributes, toFieldTypeConverter, toPropertyTypeConverter));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent, TPropertyType, TFieldType>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Expression<Func<TComponent, TFieldType>> valueField, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null)
        => metas.Apply(m => m.RenderWith(valueField, toFieldTypeConverter, toPropertyTypeConverter));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent, TPropertyType>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Expression<Func<TComponent, TPropertyType>> valueField, Action<TComponent> options) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, options));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent, TPropertyType>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Expression<Func<TComponent, TPropertyType>> valueField, TComponent instanceForAttributes) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, instanceForAttributes));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent, TPropertyType>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Expression<Func<TComponent, TPropertyType>> valueField)
        => metas.Apply(m => m.RenderWith(valueField));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Action<TComponent> options) where TComponent : new()
        => metas.Apply(m => m.RenderWith<TComponent>(options));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, IDictionary<string, object> attributes)
        => metas.Apply(m => m.RenderWith<TComponent>(attributes));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Type componentType, IDictionary<string, object> attributes = null)
        => metas.Apply(m => m.RenderWith(componentType, attributes));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, ICustomRenderer renderer)
        => metas.Apply(m => m.RenderWith(renderer));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> AsReadOnly<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool isReadOnly = true)
        => metas.Apply(m => m.AsReadOnly(isReadOnly));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> DisableUnderline<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool disableUnderline = true)
        => metas.Apply(m => m.DisableUnderline(disableUnderline));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> Ignore<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool ignore = true)
        => metas.Apply(m => m.Ignore(ignore));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithResetOptions<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool allowReset, string resetIcon, bool showResetText, string resetText)
        => metas.Apply(m => m.WithResetOptions(allowReset, resetIcon, showResetText, resetText));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithResetOptions<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, PropertyResetSettings resetSettings)
        => metas.Apply(m => m.WithResetOptions(resetSettings));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithResetOptions<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Action<PropertyResetSettings> resetSettingsAction)
        => metas.Apply(m => m.WithResetOptions(resetSettingsAction));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> AreResettable<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool resettable = true)
        => metas.Apply(m => m.IsResettable(resettable));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> NotResettable<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas)
        => metas.Apply(m => m.NotResettable());
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithSettings<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Action<ObjectEditPropertyMetaSettings> settingsAction)
        => metas.Apply(m => m.WithSettings(settingsAction));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithSeparateLabelComponent<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas)
        => metas.Apply(m => m.WithSeparateLabelComponent());
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithSeparateValidationComponent<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas)
        => metas.Apply(m => m.WithSeparateValidationComponent());
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithoutLabel<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas)
        => metas.Apply(m => m.WithoutLabel());
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithSeparateLabelComponentOnly<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas)
        => metas.Apply(m => m.WithSeparateLabelComponentOnly());
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithDefaultLabeling<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas)
        => metas.Apply(m => m.WithDefaultLabeling());
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithOrder<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, int order)
        => metas.Apply(m => m.WithOrder(order));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithLabel<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, string label)
        => metas.Apply(m => m.WithLabel(label));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithDescription<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, string description)
        => metas.Apply(m => m.WithDescription(description));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithLabelResolver<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<PropertyInfo, string> resolverFunc)
        => metas.Apply(m => m.WithLabelResolver(resolverFunc));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithDescriptionResolver<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<PropertyInfo, string> resolverFunc)
        => metas.Apply(m => m.WithDescriptionResolver(resolverFunc));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithLabelLocalizerPattern<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, string pattern = "Label_{0}", IStringLocalizer localizer = null)
        => metas.Apply(m => m.WithLabelLocalizerPattern(pattern, localizer));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithDescriptionLocalizerPattern<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, string pattern = "Description_{0}", IStringLocalizer localizer = null)
        => metas.Apply(m => m.WithDescriptionLocalizerPattern(pattern, localizer));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithGroup<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, string groupName)
        => metas.Apply(m => m.WithGroup(groupName));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithGroup<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, ObjectEditPropertyMetaGroupInfo groupInfo)
        => metas.Apply(m => m.WithGroup(groupInfo));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, params KeyValuePair<string, object>[] attributes)
        => metas.Apply(m => m.WithAdditionalAttributes(attributes));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool overwriteExisting, params KeyValuePair<string, object>[] attributes)
        => metas.Apply(m => m.WithAdditionalAttributes(overwriteExisting, attributes));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, IDictionary<string, object> attributes, bool overwriteExisting = false)
        => metas.Apply(m => m.WithAdditionalAttributes(attributes, overwriteExisting));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Dictionary<string, object> attributes, bool overwriteExisting = false)
        => metas.Apply(m => m.WithAdditionalAttributes(attributes, overwriteExisting));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttribute<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, string key, object value, bool overwriteExisting = false)
        => metas.Apply(m => m.WithAdditionalAttribute(key, value, overwriteExisting));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool overwriteExisting, params Action<TComponent>[] options) where TComponent : new()
        => metas.Apply(m => m.WithAdditionalAttributes<TComponent>(overwriteExisting, options));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, params Action<TComponent>[] options) where TComponent : new()
        => metas.Apply(m => m.WithAdditionalAttributes<TComponent>(options));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, TComponent instanceForAttributes, bool overwriteExisting = false) where TComponent : new()
        => metas.Apply(m => m.WithAdditionalAttributes<TComponent>(instanceForAttributes, overwriteExisting));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> IgnoreIf<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition)
        => metas.Apply(m => m.IgnoreIf(condition));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> AsReadOnlyIf<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition)
        => metas.Apply(m => m.AsReadOnlyIf(condition));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAttributesIf<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition, params KeyValuePair<string, object>[] attributes)
        => metas.Apply(m => m.WithAttributesIf(condition, attributes));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAttributesIf<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition, params Action<TComponent>[] options) where TComponent : new()
        => metas.Apply(m => m.WithAttributesIf<TModel, TComponent>(condition, options));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAttributesIf<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition, IDictionary<string, object> attributes)
        => metas.Apply(m => m.WithAttributesIf(condition, attributes));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAttributesIf<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition, Dictionary<string, object> attributes)
        => metas.Apply(m => m.WithAttributesIf(condition, attributes));

    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> IgnoreOnExport<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool ignore = true)
        => metas.Apply(m => m.IgnoreOnExport(ignore));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> IgnoreOnImport<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool ignore = true)
        => metas.Apply(m => m.IgnoreOnImport(ignore));
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> IgnoreOnExportAndImport<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool ignore = true)
        => metas.Apply(m => m.IgnoreOnImport(ignore).IgnoreOnExport(ignore));

    //public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAttributesIf<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition, TComponent instanceForAttributes) where TComponent : new()
    //    => metas.Apply(m => m.WithAttributesIf<TModel, TComponent>(condition, instanceForAttributes));

    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> AsDisabledIf<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition)
        => metas.Apply(m => m.AsDisabledIf(condition));
}
