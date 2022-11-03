using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;


public static partial class MudExObjectEditExtensions
{
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent, TPropertyType, TFieldType>(this IEnumerable<ObjectEditPropertyMeta> metas, Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, options, toFieldTypeConverter, toPropertyTypeConverter));
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent, TPropertyType, TFieldType>(this IEnumerable<ObjectEditPropertyMeta> metas, Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, instanceForAttributes, toFieldTypeConverter, toPropertyTypeConverter));
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent, TPropertyType, TFieldType>(this IEnumerable<ObjectEditPropertyMeta> metas, Expression<Func<TComponent, TFieldType>> valueField, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null)
        => metas.Apply(m => m.RenderWith(valueField, toFieldTypeConverter, toPropertyTypeConverter));
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent, TPropertyType>(this IEnumerable<ObjectEditPropertyMeta> metas, Expression<Func<TComponent, TPropertyType>> valueField, Action<TComponent> options) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, options));
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent, TPropertyType>(this IEnumerable<ObjectEditPropertyMeta> metas, Expression<Func<TComponent, TPropertyType>> valueField, TComponent instanceForAttributes) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, instanceForAttributes));
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent, TPropertyType>(this IEnumerable<ObjectEditPropertyMeta> metas, Expression<Func<TComponent, TPropertyType>> valueField)
        => metas.Apply(m => m.RenderWith(valueField));
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, Action<TComponent> options) where TComponent : new()
        => metas.Apply(m => m.RenderWith<TComponent>(options));
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, IDictionary<string, object> attributes)
        => metas.Apply(m => m.RenderWith<TComponent>(attributes));
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith(this IEnumerable<ObjectEditPropertyMeta> metas, Type componentType, IDictionary<string, object> attributes = null)
        => metas.Apply(m => m.RenderWith(componentType, attributes));
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith(this IEnumerable<ObjectEditPropertyMeta> metas, ICustomRenderer renderer)
        => metas.Apply(m => m.RenderWith(renderer));
    public static IEnumerable<ObjectEditPropertyMeta> AsReadOnly(this IEnumerable<ObjectEditPropertyMeta> metas, bool isReadOnly = true)
        => metas.Apply(m => m.AsReadOnly(isReadOnly));
    public static IEnumerable<ObjectEditPropertyMeta> DisableUnderline(this IEnumerable<ObjectEditPropertyMeta> metas, bool disableUnderline = true)
        => metas.Apply(m => m.DisableUnderline(disableUnderline));
    public static IEnumerable<ObjectEditPropertyMeta> Ignore(this IEnumerable<ObjectEditPropertyMeta> metas, bool ignore = true)
        => metas.Apply(m => m.Ignore(ignore));
    public static IEnumerable<ObjectEditPropertyMeta> WithResetOptions(this IEnumerable<ObjectEditPropertyMeta> metas, bool allowReset, string resetIcon, bool showResetText, string resetText)
        => metas.Apply(m => m.WithResetOptions(allowReset, resetIcon, showResetText, resetText));
    public static IEnumerable<ObjectEditPropertyMeta> WithResetOptions(this IEnumerable<ObjectEditPropertyMeta> metas, PropertyResetSettings resetSettings)
        => metas.Apply(m => m.WithResetOptions(resetSettings));
    public static IEnumerable<ObjectEditPropertyMeta> WithResetOptions(this IEnumerable<ObjectEditPropertyMeta> metas, Action<PropertyResetSettings> resetSettingsAction)
        => metas.Apply(m => m.WithResetOptions(resetSettingsAction));
    public static IEnumerable<ObjectEditPropertyMeta> AreResettable(this IEnumerable<ObjectEditPropertyMeta> metas, bool resettable = true)
        => metas.Apply(m => m.IsResettable(resettable));
    public static IEnumerable<ObjectEditPropertyMeta> NotResettable(this IEnumerable<ObjectEditPropertyMeta> metas)
        => metas.Apply(m => m.NotResettable());
    public static IEnumerable<ObjectEditPropertyMeta> WithSettings(this IEnumerable<ObjectEditPropertyMeta> metas, Action<ObjectEditPropertyMetaSettings> settingsAction)
        => metas.Apply(m => m.WithSettings(settingsAction));
    public static IEnumerable<ObjectEditPropertyMeta> WithSeparateLabelComponent(this IEnumerable<ObjectEditPropertyMeta> metas)
        => metas.Apply(m => m.WithSeparateLabelComponent());
    public static IEnumerable<ObjectEditPropertyMeta> WithSeparateValidationComponent(this IEnumerable<ObjectEditPropertyMeta> metas)
        => metas.Apply(m => m.WithSeparateValidationComponent());
    public static IEnumerable<ObjectEditPropertyMeta> WithoutLabel(this IEnumerable<ObjectEditPropertyMeta> metas)
        => metas.Apply(m => m.WithoutLabel());
    public static IEnumerable<ObjectEditPropertyMeta> WithSeparateLabelComponentOnly(this IEnumerable<ObjectEditPropertyMeta> metas)
        => metas.Apply(m => m.WithSeparateLabelComponentOnly());
    public static IEnumerable<ObjectEditPropertyMeta> WithDefaultLabeling(this IEnumerable<ObjectEditPropertyMeta> metas)
        => metas.Apply(m => m.WithDefaultLabeling());
    public static IEnumerable<ObjectEditPropertyMeta> WithOrder(this IEnumerable<ObjectEditPropertyMeta> metas, int order)
        => metas.Apply(m => m.WithOrder(order));
    public static IEnumerable<ObjectEditPropertyMeta> WithLabel(this IEnumerable<ObjectEditPropertyMeta> metas, string label)
        => metas.Apply(m => m.WithLabel(label));
    public static IEnumerable<ObjectEditPropertyMeta> WithDescription(this IEnumerable<ObjectEditPropertyMeta> metas, string description)
        => metas.Apply(m => m.WithDescription(description));
    public static IEnumerable<ObjectEditPropertyMeta> WithLabelResolver(this IEnumerable<ObjectEditPropertyMeta> metas, Func<PropertyInfo, string> resolverFunc)
        => metas.Apply(m => m.WithLabelResolver(resolverFunc));
    public static IEnumerable<ObjectEditPropertyMeta> WithDescriptionResolver(this IEnumerable<ObjectEditPropertyMeta> metas, Func<PropertyInfo, string> resolverFunc)
        => metas.Apply(m => m.WithDescriptionResolver(resolverFunc));
    public static IEnumerable<ObjectEditPropertyMeta> WithLabelLocalizerPattern(this IEnumerable<ObjectEditPropertyMeta> metas, string pattern = "Label_{0}", IStringLocalizer localizer = null)
        => metas.Apply(m => m.WithLabelLocalizerPattern(pattern, localizer));
    public static IEnumerable<ObjectEditPropertyMeta> WithDescriptionLocalizerPattern(this IEnumerable<ObjectEditPropertyMeta> metas, string pattern = "Description_{0}", IStringLocalizer localizer = null)
        => metas.Apply(m => m.WithDescriptionLocalizerPattern(pattern, localizer));
    public static IEnumerable<ObjectEditPropertyMeta> WithGroup(this IEnumerable<ObjectEditPropertyMeta> metas, string groupName)
        => metas.Apply(m => m.WithGroup(groupName));
    public static IEnumerable<ObjectEditPropertyMeta> WithGroup(this IEnumerable<ObjectEditPropertyMeta> metas, ObjectEditPropertyMetaGroupInfo groupInfo)
        => metas.Apply(m => m.WithGroup(groupInfo));
    public static IEnumerable<IRenderData> WrapIn<TWrapperComponent>(this IEnumerable<IRenderData> renderDatas, params Action<TWrapperComponent>[] options) where TWrapperComponent : new()
        => renderDatas.Apply(rd => rd.WrapIn<TWrapperComponent>(options));
    public static IEnumerable<IRenderData> WrapIn<TWrapperComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, params Action<TWrapperComponent>[] options) where TWrapperComponent : new()
        => metas.Select(m => m.WrapIn<TWrapperComponent>(options)).ToList();
    public static IEnumerable<IRenderData> WrapInMudItem(this IEnumerable<ObjectEditPropertyMeta> metas, params Action<MudItem>[] options)
        => metas.Select(m => m.WrapInMudItem(options)).ToList();
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes(this IEnumerable<ObjectEditPropertyMeta> metas, params KeyValuePair<string, object>[] attributes)
        => metas.Apply(m => m.WithAdditionalAttributes(attributes));
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes(this IEnumerable<ObjectEditPropertyMeta> metas, bool overwriteExisting, params KeyValuePair<string, object>[] attributes)
        => metas.Apply(m => m.WithAdditionalAttributes(overwriteExisting, attributes));
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes(this IEnumerable<ObjectEditPropertyMeta> metas, IDictionary<string, object> attributes, bool overwriteExisting = false)
        => metas.Apply(m => m.WithAdditionalAttributes(attributes, overwriteExisting));
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes(this IEnumerable<ObjectEditPropertyMeta> metas, Dictionary<string, object> attributes, bool overwriteExisting = false)
        => metas.Apply(m => m.WithAdditionalAttributes(attributes, overwriteExisting));
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttribute(this IEnumerable<ObjectEditPropertyMeta> metas, string key, object value, bool overwriteExisting = false)
        => metas.Apply(m => m.WithAdditionalAttribute(key, value, overwriteExisting));
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes<TComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, bool overwriteExisting, params Action<TComponent>[] options) where TComponent : new()
        => metas.Apply(m => m.WithAdditionalAttributes<TComponent>(overwriteExisting, options));
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes<TComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, params Action<TComponent>[] options) where TComponent : new()
        => metas.Apply(m => m.WithAdditionalAttributes<TComponent>(options));
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes<TComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, TComponent instanceForAttributes, bool overwriteExisting = false) where TComponent : new()
        => metas.Apply(m => m.WithAdditionalAttributes<TComponent>(instanceForAttributes, overwriteExisting));
    public static IEnumerable<ObjectEditPropertyMeta> IgnoreIf<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition)
        => metas.Apply(m => m.IgnoreIf(condition));
    public static IEnumerable<ObjectEditPropertyMeta> AsReadOnlyIf<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition)
        => metas.Apply(m => m.AsReadOnlyIf(condition));
    public static IEnumerable<ObjectEditPropertyMeta> WithAttributesIf<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition, params KeyValuePair<string, object>[] attributes)
        => metas.Apply(m => m.WithAttributesIf(condition, attributes));
    public static IEnumerable<ObjectEditPropertyMeta> WithAttributesIf<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition, params Action<TComponent>[] options) where TComponent : new()
        => metas.Apply(m => m.WithAttributesIf<TModel, TComponent>(condition, options));
    public static IEnumerable<ObjectEditPropertyMeta> WithAttributesIf<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition, IDictionary<string, object> attributes)
        => metas.Apply(m => m.WithAttributesIf(condition, attributes));
    public static IEnumerable<ObjectEditPropertyMeta> WithAttributesIf<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition, Dictionary<string, object> attributes)
        => metas.Apply(m => m.WithAttributesIf(condition, attributes));

    public static IEnumerable<ObjectEditPropertyMeta> IgnoreOnExport<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, bool ignore = true)
        => metas.Apply(m => m.IgnoreOnExport(ignore));
    public static IEnumerable<ObjectEditPropertyMeta> IgnoreOnImport<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, bool ignore = true)
        => metas.Apply(m => m.IgnoreOnImport(ignore));
    public static IEnumerable<ObjectEditPropertyMeta> IgnoreOnExportAndImport<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, bool ignore = true)
        => metas.Apply(m => m.IgnoreOnImport(ignore).IgnoreOnExport(ignore));
    //public static IEnumerable<ObjectEditPropertyMeta> WithAttributesIf<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition, TComponent instanceForAttributes) where TComponent : new()
    //    => metas.Apply(m => m.WithAttributesIf<TModel, TComponent>(condition, instanceForAttributes));
}
