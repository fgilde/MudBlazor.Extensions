using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;


public static partial class MudExObjectEditExtensions
{

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMeta to render with a specified component type, supporting conversion between property and field types and allowing for additional component configuration.
    /// <typeparam name="TComponent">The component type to render.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property to render.</typeparam>
    /// <typeparam name="TFieldType">The field type used by the component.</typeparam>
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="valueField">An expression identifying the component's value field.</param>
    /// <param name="options">An action for configuring the component instance.</param>
    /// <param name="toFieldTypeConverter">An optional converter from the property type to the field type.</param>
    /// <param name="toPropertyTypeConverter">An optional converter from the field type back to the property type.</param>
    /// <returns>The extended collection of ObjectEditPropertyMeta.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent, TPropertyType, TFieldType>(this IEnumerable<ObjectEditPropertyMeta> metas, Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, options, toFieldTypeConverter, toPropertyTypeConverter));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMeta to render with a specified component type, directly specifying instance attributes and supporting conversion between property and field types.
    /// <typeparam name="TComponent">The component type to render.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property to render.</typeparam>
    /// <typeparam name="TFieldType">The field type used by the component.</typeparam>
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="valueField">An expression identifying the component's value field.</param>
    /// <param name="instanceForAttributes">An instance of the component to specify attributes.</param>
    /// <param name="toFieldTypeConverter">An optional converter from the property type to the field type.</param>
    /// <param name="toPropertyTypeConverter">An optional converter from the field type back to the property type.</param>
    /// <returns>The extended collection of ObjectEditPropertyMeta.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent, TPropertyType, TFieldType>(this IEnumerable<ObjectEditPropertyMeta> metas, Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, instanceForAttributes, toFieldTypeConverter, toPropertyTypeConverter));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMeta to render with a specified component type, supporting conversion between property and field types without additional configuration.
    /// <typeparam name="TComponent">The component type to render.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property to render.</typeparam>
    /// <typeparam name="TFieldType">The field type used by the component.</typeparam>
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="valueField">An expression identifying the component's value field.</param>
    /// <param name="toFieldTypeConverter">An optional converter from the property type to the field type.</param>
    /// <param name="toPropertyTypeConverter">An optional converter from the field type back to the property type.</param>
    /// <returns>The extended collection of ObjectEditPropertyMeta.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent, TPropertyType, TFieldType>(this IEnumerable<ObjectEditPropertyMeta> metas, Expression<Func<TComponent, TFieldType>> valueField, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null)
        => metas.Apply(m => m.RenderWith(valueField, toFieldTypeConverter, toPropertyTypeConverter));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMeta to render with a specified component type, allowing for additional component configuration.
    /// <typeparam name="TComponent">The component type to render.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property to render.</typeparam>
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="valueField">An expression identifying the component's value field.</param>
    /// <param name="options">An action for configuring the component instance.</param>
    /// <returns>The extended collection of ObjectEditPropertyMeta.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent, TPropertyType>(this IEnumerable<ObjectEditPropertyMeta> metas, Expression<Func<TComponent, TPropertyType>> valueField, Action<TComponent> options) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, options));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMeta to render with a specified component type, directly specifying instance attributes.
    /// <typeparam name="TComponent">The component type to render.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property to render.</typeparam>
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="valueField">An expression identifying the component's value field.</param>
    /// <param name="instanceForAttributes">An instance of the component to specify attributes.</param>
    /// <returns>The extended collection of ObjectEditPropertyMeta.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent, TPropertyType>(this IEnumerable<ObjectEditPropertyMeta> metas, Expression<Func<TComponent, TPropertyType>> valueField, TComponent instanceForAttributes) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, instanceForAttributes));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMeta to render with a specified component type without additional configuration or converters.
    /// <typeparam name="TComponent">The component type to render.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property to render.</typeparam>
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="valueField">An expression identifying the component's value field.</param>
    /// <returns>The extended collection of ObjectEditPropertyMeta.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent, TPropertyType>(this IEnumerable<ObjectEditPropertyMeta> metas, Expression<Func<TComponent, TPropertyType>> valueField)
        => metas.Apply(m => m.RenderWith(valueField));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMeta to render with a specified component type, allowing for additional configuration through options without specifying property or field types.
    /// <typeparam name="TComponent">The component type to render.</typeparam>
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="options">An action for configuring the component instance.</param>
    /// <returns>The extended collection of ObjectEditPropertyMeta.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, Action<TComponent> options) where TComponent : new()
        => metas.Apply(m => m.RenderWith(options));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMeta to render with a specified component type, applying custom attributes to the component.
    /// <typeparam name="TComponent">The component type to render.</typeparam>
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="attributes">A dictionary of attributes to apply to the component.</param>
    /// <returns>The extended collection of ObjectEditPropertyMeta.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith<TComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, IDictionary<string, object> attributes)
        => metas.Apply(m => m.RenderWith<TComponent>(attributes));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMeta to render with a specified component type, applying custom attributes.
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="componentType">The type of the component to render.</param>
    /// <param name="attributes">An optional dictionary of attributes to apply to the component.</param>
    /// <returns>The extended collection of ObjectEditPropertyMeta.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith(this IEnumerable<ObjectEditPropertyMeta> metas, Type componentType, IDictionary<string, object> attributes = null)
        => metas.Apply(m => m.RenderWith(componentType, attributes));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMeta to render with a custom renderer implementation.
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="renderer">The custom renderer to use for rendering.</param>
    /// <returns>The extended collection of ObjectEditPropertyMeta.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> RenderWith(this IEnumerable<ObjectEditPropertyMeta> metas, ICustomRenderer renderer)
        => metas.Apply(m => m.RenderWith(renderer));

    /// <summary>
    /// Sets the specified properties within the collection of ObjectEditPropertyMeta to read-only based on the provided condition.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="isReadOnly">Indicates whether the properties should be read-only.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with read-only properties as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> AsReadOnly(this IEnumerable<ObjectEditPropertyMeta> metas, bool isReadOnly = true)
        => metas.Apply(m => m.AsReadOnly(isReadOnly));

    /// <summary>
    /// Disables the underline for the specified properties within the collection of ObjectEditPropertyMeta.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="disableUnderline">Indicates whether the underline should be disabled.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with underline disabled as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> DisableUnderline(this IEnumerable<ObjectEditPropertyMeta> metas, bool disableUnderline = true)
        => metas.Apply(m => m.DisableUnderline(disableUnderline));

    /// <summary>
    /// Ignores the specified properties within the collection of ObjectEditPropertyMeta based on the provided condition.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="ignore">Indicates whether the properties should be ignored.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with properties ignored as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> Ignore(this IEnumerable<ObjectEditPropertyMeta> metas, bool ignore = true)
        => metas.Apply(m => m.Ignore(ignore));

    /// <summary>
    /// Configures reset options for the specified properties within the collection of ObjectEditPropertyMeta.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="allowReset">Indicates whether the reset functionality should be enabled.</param>
    /// <param name="resetIcon">The icon to use for the reset button.</param>
    /// <param name="showResetText">Indicates whether text should be shown next to the reset icon.</param>
    /// <param name="resetText">The text to display next to the reset icon.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with reset options configured as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithResetOptions(this IEnumerable<ObjectEditPropertyMeta> metas, bool allowReset, string resetIcon, bool showResetText, string resetText)
        => metas.Apply(m => m.WithResetOptions(allowReset, resetIcon, showResetText, resetText));

    /// <summary>
    /// Configures reset options for the specified properties within the collection of ObjectEditPropertyMeta using a PropertyResetSettings object.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="resetSettings">The PropertyResetSettings object containing reset configuration.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with reset options configured as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithResetOptions(this IEnumerable<ObjectEditPropertyMeta> metas, PropertyResetSettings resetSettings)
        => metas.Apply(m => m.WithResetOptions(resetSettings));

    /// <summary>
    /// Configures reset options for the specified properties within the collection of ObjectEditPropertyMeta using a delegate action.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="resetSettingsAction">An action delegate to configure the reset settings.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with reset options configured as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithResetOptions(this IEnumerable<ObjectEditPropertyMeta> metas, Action<PropertyResetSettings> resetSettingsAction)
        => metas.Apply(m => m.WithResetOptions(resetSettingsAction));

    /// <summary>
    /// Marks the specified properties within the collection of ObjectEditPropertyMeta as resettable based on the provided condition.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="resettable">Indicates whether the properties should be resettable.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta marked as resettable as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> AreResettable(this IEnumerable<ObjectEditPropertyMeta> metas, bool resettable = true)
        => metas.Apply(m => m.IsResettable(resettable));

    /// <summary>
    /// Marks the specified properties within the collection of ObjectEditPropertyMeta as not resettable.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta marked as not resettable.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> NotResettable(this IEnumerable<ObjectEditPropertyMeta> metas)
        => metas.Apply(m => m.NotResettable());

    /// <summary>
    /// Applies custom settings to the specified properties within the collection of ObjectEditPropertyMeta using a delegate action.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="settingsAction">An action delegate to configure the settings.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with custom settings applied as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithSettings(this IEnumerable<ObjectEditPropertyMeta> metas, Action<ObjectEditPropertyMetaSettings> settingsAction)
        => metas.Apply(m => m.WithSettings(settingsAction));

    /// <summary>
    /// Separates the label component for the specified properties within the collection of ObjectEditPropertyMeta.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with separate label components.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithSeparateLabelComponent(this IEnumerable<ObjectEditPropertyMeta> metas)
        => metas.Apply(m => m.WithSeparateLabelComponent());

    /// <summary>
    /// Separates the validation component for the specified properties within the collection of ObjectEditPropertyMeta.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with separate validation components.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithSeparateValidationComponent(this IEnumerable<ObjectEditPropertyMeta> metas)
        => metas.Apply(m => m.WithSeparateValidationComponent());

    /// <summary>
    /// Removes the label for the specified properties within the collection of ObjectEditPropertyMeta.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta without labels.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithoutLabel(this IEnumerable<ObjectEditPropertyMeta> metas)
        => metas.Apply(m => m.WithoutLabel());

    /// <summary>
    /// Applies only a separate label component for the specified properties within the collection of ObjectEditPropertyMeta, excluding other configurations.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with only separate label components.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithSeparateLabelComponentOnly(this IEnumerable<ObjectEditPropertyMeta> metas)
        => metas.Apply(m => m.WithSeparateLabelComponentOnly());

    /// <summary>
    /// Applies default labeling for the specified properties within the collection of ObjectEditPropertyMeta.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with default labeling applied.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithDefaultLabeling(this IEnumerable<ObjectEditPropertyMeta> metas)
        => metas.Apply(m => m.WithDefaultLabeling());

    /// <summary>
    /// Orders the specified properties within the collection of ObjectEditPropertyMeta.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="order">The order in which the properties should be displayed.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta ordered as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithOrder(this IEnumerable<ObjectEditPropertyMeta> metas, int order)
        => metas.Apply(m => m.WithOrder(order));

    /// <summary>
    /// Labels the specified properties within the collection of ObjectEditPropertyMeta.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="label">The label to apply to the properties.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with specified labels.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithLabel(this IEnumerable<ObjectEditPropertyMeta> metas, string label)
        => metas.Apply(m => m.WithLabel(label));

    /// <summary>
    /// Describes the specified properties within the collection of ObjectEditPropertyMeta.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="description">The description to apply to the properties.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with specified descriptions.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithDescription(this IEnumerable<ObjectEditPropertyMeta> metas, string description)
        => metas.Apply(m => m.WithDescription(description));

    /// <summary>
    /// Applies a label resolver for the specified properties within the collection of ObjectEditPropertyMeta.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="resolverFunc">The function to resolve labels dynamically.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with dynamic label resolution.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithLabelResolver(this IEnumerable<ObjectEditPropertyMeta> metas, Func<PropertyInfo, string> resolverFunc)
        => metas.Apply(m => m.WithLabelResolver(resolverFunc));

    /// <summary>
    /// Applies a description resolver for the specified properties within the collection of ObjectEditPropertyMeta.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="resolverFunc">The function to resolve descriptions dynamically.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with dynamic description resolution.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithDescriptionResolver(this IEnumerable<ObjectEditPropertyMeta> metas, Func<PropertyInfo, string> resolverFunc)
        => metas.Apply(m => m.WithDescriptionResolver(resolverFunc));

    /// <summary>
    /// Applies a label localizer pattern for the specified properties within the collection of ObjectEditPropertyMeta.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="pattern">The pattern to use for localizing labels.</param>
    /// <param name="localizer">An optional IStringLocalizer to apply for localization.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with localized labels based on the specified pattern.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithLabelLocalizerPattern(this IEnumerable<ObjectEditPropertyMeta> metas, string pattern = "Label_{0}", IStringLocalizer localizer = null)
        => metas.Apply(m => m.WithLabelLocalizerPattern(pattern, localizer));

    /// <summary>
    /// Applies a description localizer pattern for the specified properties within the collection of ObjectEditPropertyMeta.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="pattern">The pattern to use for localizing descriptions.</param>
    /// <param name="localizer">An optional IStringLocalizer to apply for localization.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with localized descriptions based on the specified pattern.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithDescriptionLocalizerPattern(this IEnumerable<ObjectEditPropertyMeta> metas, string pattern = "Description_{0}", IStringLocalizer localizer = null)
        => metas.Apply(m => m.WithDescriptionLocalizerPattern(pattern, localizer));

    /// <summary>
    /// Groups the specified properties within the collection of ObjectEditPropertyMeta under a specified group name.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="groupName">The name of the group to assign the properties to.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta grouped as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithGroup(this IEnumerable<ObjectEditPropertyMeta> metas, string groupName)
        => metas.Apply(m => m.WithGroup(groupName));

    /// <summary>
    /// Groups the specified properties within the collection of ObjectEditPropertyMeta under a specified group information object.
    /// <param name="metas">The collection of property metadata to modify.</param>
    /// <param name="groupInfo">The ObjectEditPropertyMetaGroupInfo object containing group information.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta grouped as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithGroup(this IEnumerable<ObjectEditPropertyMeta> metas, ObjectEditPropertyMetaGroupInfo groupInfo)
        => metas.Apply(m => m.WithGroup(groupInfo));

    /// <summary>
    /// Wraps the collection of ObjectEditPropertyMeta within a MudExValidationWrapper component, binding it to a specific property.
    /// <typeparam name="TPropertyType">The type of the property to be validated.</typeparam>
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="forExpression">An expression that identifies the property to be validated.</param>
    /// <returns>The extended collection of ObjectEditPropertyMeta wrapped within a MudExValidationWrapper.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WrapInValidationWrapper<TPropertyType>(this IEnumerable<ObjectEditPropertyMeta> metas, Expression<Func<TPropertyType>> forExpression) =>
        metas.Apply(m => m.WrapIn<MudExValidationWrapper<TPropertyType>>(w => w.For = forExpression));

    /// <summary>
    /// Wraps the collection of ObjectEditPropertyMeta within a MudExValidationWrapper component, configuring it with a set of options.
    /// <typeparam name="TPropertyType">The type of the property to be validated.</typeparam>
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="options">A set of actions to configure the MudExValidationWrapper instance.</param>
    /// <returns>The extended collection of ObjectEditPropertyMeta wrapped within a MudExValidationWrapper.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WrapInValidationWrapper<TPropertyType>(this IEnumerable<ObjectEditPropertyMeta> metas, Action<MudExValidationWrapper<TPropertyType>>[] options)
        => metas.Apply(m => m.WrapIn(options));

    /// <summary>
    /// Wraps each IRenderData in the collection within a new instance of a specified wrapper component type, allowing for additional configuration through options.
    /// <typeparam name="TWrapperComponent">The component type to use as a wrapper.</typeparam>
    /// <param name="renderData">The collection of IRenderData to be wrapped.</param>
    /// <param name="options">A set of actions to configure each wrapper component instance.</param>
    /// <returns>The collection of IRenderData with each item wrapped in the specified component.</returns>
    /// </summary>
    public static IEnumerable<IRenderData> WrapIn<TWrapperComponent>(this IEnumerable<IRenderData> renderData, params Action<TWrapperComponent>[] options) where TWrapperComponent : new()
        => renderData.Apply(rd => rd.WrapIn(options));

    /// <summary>
    /// Wraps each ObjectEditPropertyMeta in the collection within a new instance of a specified wrapper component type, allowing for additional configuration through options.
    /// <typeparam name="TWrapperComponent">The component type to use as a wrapper.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be wrapped.</param>
    /// <param name="options">A set of actions to configure each wrapper component instance.</param>
    /// <returns>The collection of IRenderData with each ObjectEditPropertyMeta wrapped in the specified component.</returns>
    /// </summary>
    public static IEnumerable<IRenderData> WrapIn<TWrapperComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, params Action<TWrapperComponent>[] options) where TWrapperComponent : new()
        => metas.Select(m => m.WrapIn(options)).ToList();

    /// <summary>
    /// Wraps each ObjectEditPropertyMeta in the collection within a new instance of a specified wrapper component type, allowing for complex configuration involving both the meta and the component.
    /// <typeparam name="TWrapperComponent">The component type to use as a wrapper.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be wrapped.</param>
    /// <param name="options">A set of actions to configure each wrapper component instance with access to both meta and component.</param>
    /// <returns>The collection of IRenderData with each ObjectEditPropertyMeta wrapped in the specified component.</returns>
    /// </summary>
    public static IEnumerable<IRenderData> WrapIn<TWrapperComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, params Action<ObjectEditPropertyMeta, TWrapperComponent>[] options) where TWrapperComponent : new()
    {
        return metas.Select(meta =>
        {
            return meta.WrapIn<TWrapperComponent>(cmp =>
            {
                foreach (var option in options)
                {
                    option(meta, cmp);
                }
            });
        }).ToList();
    }

    /// <summary>
    /// Wraps each ObjectEditPropertyMeta in the collection within a MudItem component, allowing for additional configuration through options.
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be wrapped.</param>
    /// <param name="options">A set of actions to configure each MudItem wrapper component instance.</param>
    /// <returns>The collection of IRenderData with each ObjectEditPropertyMeta wrapped in a MudItem component.</returns>
    /// </summary>
    public static IEnumerable<IRenderData> WrapInMudItem(this IEnumerable<ObjectEditPropertyMeta> metas, params Action<MudItem>[] options)
        => metas.Select(m => m.WrapInMudItem(options)).ToList();

    /// <summary>
    /// Adds additional attributes to each ObjectEditPropertyMeta in the collection.
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="attributes">Key-value pairs representing the additional attributes to add.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with additional attributes.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes(this IEnumerable<ObjectEditPropertyMeta> metas, params KeyValuePair<string, object>[] attributes)
        => metas.Apply(m => m.WithAdditionalAttributes(attributes));

    /// <summary>
    /// Adds additional attributes to each ObjectEditPropertyMeta in the collection, with an option to overwrite existing attributes.
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="overwriteExisting">Indicates whether to overwrite existing attributes.</param>
    /// <param name="attributes">Key-value pairs representing the additional attributes to add or overwrite.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with additional attributes.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes(this IEnumerable<ObjectEditPropertyMeta> metas, bool overwriteExisting, params KeyValuePair<string, object>[] attributes)
        => metas.Apply(m => m.WithAdditionalAttributes(overwriteExisting, attributes));

    /// <summary>
    /// Adds additional attributes to each ObjectEditPropertyMeta in the collection from a dictionary, with an option to overwrite existing attributes.
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="attributes">A dictionary of attributes to add or overwrite.</param>
    /// <param name="overwriteExisting">Indicates whether to overwrite existing attributes.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with additional attributes.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes(this IEnumerable<ObjectEditPropertyMeta> metas, IDictionary<string, object> attributes, bool overwriteExisting = false)
        => metas.Apply(m => m.WithAdditionalAttributes(attributes, overwriteExisting));

    /// <summary>
    /// Adds additional attributes to each ObjectEditPropertyMeta in the collection from a dictionary, with an option to overwrite existing attributes.
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="attributes">A dictionary of attributes to add or overwrite.</param>
    /// <param name="overwriteExisting">Indicates whether to overwrite existing attributes.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with additional attributes.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes(this IEnumerable<ObjectEditPropertyMeta> metas, Dictionary<string, object> attributes, bool overwriteExisting = false)
        => metas.Apply(m => m.WithAdditionalAttributes(attributes, overwriteExisting));

    /// <summary>
    /// Adds a single additional attribute to each ObjectEditPropertyMeta in the collection, with an option to overwrite an existing attribute of the same key.
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="key">The key of the attribute to add or overwrite.</param>
    /// <param name="value">The value of the attribute.</param>
    /// <param name="overwriteExisting">Indicates whether to overwrite an existing attribute of the same key.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with the additional attribute.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttribute(this IEnumerable<ObjectEditPropertyMeta> metas, string key, object value, bool overwriteExisting = false)
        => metas.Apply(m => m.WithAdditionalAttribute(key, value, overwriteExisting));

    /// <summary>
    /// Adds additional attributes to each ObjectEditPropertyMeta in the collection using component-specific options, with an option to overwrite existing attributes.
    /// <typeparam name="TComponent">The component type for which the attributes are applicable.</typeparam>
    /// <param name="metas">The collection of property metadata to extend.</param>
    /// <param name="overwriteExisting">Indicates whether to overwrite existing attributes.</param>
    /// <param name="options">A set of actions to configure the component instance for additional attributes.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with additional attributes.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes<TComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, bool overwriteExisting, params Action<TComponent>[] options) where TComponent : new()
        => metas.Apply(m => m.WithAdditionalAttributes(overwriteExisting, options));

    /// <summary>
    /// Adds additional attributes to each component in the collection of ObjectEditPropertyMeta, using specified options for configuration.
    /// <typeparam name="TComponent">The component type to which the attributes will be added.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be extended.</param>
    /// <param name="options">Actions to configure the attributes of the component.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with additional attributes.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes<TComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, params Action<TComponent>[] options) where TComponent : new()
        => metas.Apply(m => m.WithAdditionalAttributes(options));

    /// <summary>
    /// Adds additional attributes to each component in the collection of ObjectEditPropertyMeta, using an instance for attribute specification and optionally overwriting existing attributes.
    /// <typeparam name="TComponent">The component type to which the attributes will be added.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be extended.</param>
    /// <param name="instanceForAttributes">The component instance used for specifying attributes.</param>
    /// <param name="overwriteExisting">Whether to overwrite existing attributes.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with additional attributes.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithAdditionalAttributes<TComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, TComponent instanceForAttributes, bool overwriteExisting = false) where TComponent : new()
        => metas.Apply(m => m.WithAdditionalAttributes(instanceForAttributes, overwriteExisting));

    /// <summary>
    /// Conditionally ignores properties in the collection of ObjectEditPropertyMeta based on a specified condition.
    /// <typeparam name="TModel">The model type used to evaluate the condition.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be filtered.</param>
    /// <param name="condition">The condition that determines if a property should be ignored.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with certain properties ignored based on the condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> IgnoreIf<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition)
        => metas.Apply(m => m.IgnoreIf(condition));

    /// <summary>
    /// Conditionally sets properties in the collection of ObjectEditPropertyMeta to read-only based on a specified condition.
    /// <typeparam name="TModel">The model type used to evaluate the condition.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be modified.</param>
    /// <param name="condition">The condition that determines if a property should be set to read-only.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with certain properties set to read-only based on the condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> AsReadOnlyIf<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition)
        => metas.Apply(m => m.AsReadOnlyIf(condition));

    /// <summary>
    /// Adds attributes to properties in the collection of ObjectEditPropertyMeta based on a specified condition, using a set of key-value pairs.
    /// <typeparam name="TModel">The model type used to evaluate the condition.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be extended.</param>
    /// <param name="condition">The condition that determines if attributes should be added.</param>
    /// <param name="attributes">The attributes to add if the condition is met.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with additional attributes based on the condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithAttributesIf<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition, params KeyValuePair<string, object>[] attributes)
        => metas.Apply(m => m.WithAttributesIf(condition, attributes));

    /// <summary>
    /// Adds attributes to properties in the collection of ObjectEditPropertyMeta based on a specified condition, using configuration options for a component.
    /// <typeparam name="TModel">The model type used to evaluate the condition.</typeparam>
    /// <typeparam name="TComponent">The component type for which the attributes are being added.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be extended.</param>
    /// <param name="condition">The condition that determines if attributes should be added.</param>
    /// <param name="options">Actions to configure the component if the condition is met.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with additional attributes based on the condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithAttributesIf<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition, params Action<TComponent>[] options) where TComponent : new()
        => metas.Apply(m => m.WithAttributesIf(condition, options));

    /// <summary>
    /// Adds attributes to properties in the collection of ObjectEditPropertyMeta based on a specified condition, using a dictionary of attributes.
    /// <typeparam name="TModel">The model type used to evaluate the condition.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be extended.</param>
    /// <param name="condition">The condition that determines if attributes should be added.</param>
    /// <param name="attributes">The dictionary of attributes to add if the condition is met.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with additional attributes based on the condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithAttributesIf<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition, IDictionary<string, object> attributes)
        => metas.Apply(m => m.WithAttributesIf(condition, attributes));

    /// <summary>
    /// Adds attributes to properties in the collection of ObjectEditPropertyMeta based on a specified condition, using a dictionary of attributes.
    /// <typeparam name="TModel">The model type used to evaluate the condition.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be extended.</param>
    /// <param name="condition">The condition that determines if attributes should be added.</param>
    /// <param name="attributes">The dictionary of attributes to add if the condition is met.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with additional attributes based on the condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithAttributesIf<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition, Dictionary<string, object> attributes)
        => metas.Apply(m => m.WithAttributesIf(condition, attributes));

    /// <summary>
    /// Ignores properties on export for the specified model type within the collection of ObjectEditPropertyMeta based on a boolean flag.
    /// <typeparam name="TModel">The model type for which properties may be ignored on export.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be modified.</param>
    /// <param name="ignore">A boolean flag indicating whether to ignore properties on export.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with specified properties ignored on export.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> IgnoreOnExport<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, bool ignore = true)
        => metas.Apply(m => m.IgnoreOnExport(ignore));

    /// <summary>
    /// Ignores properties on import for the specified model type within the collection of ObjectEditPropertyMeta based on a boolean flag.
    /// <typeparam name="TModel">The model type for which properties may be ignored on import.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be modified.</param>
    /// <param name="ignore">A boolean flag indicating whether to ignore properties on import.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with specified properties ignored on import.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> IgnoreOnImport<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, bool ignore = true)
        => metas.Apply(m => m.IgnoreOnImport(ignore));

    /// <summary>
    /// Ignores properties on both export and import for the specified model type within the collection of ObjectEditPropertyMeta based on a boolean flag.
    /// <typeparam name="TModel">The model type for which properties may be ignored on both export and import.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be modified.</param>
    /// <param name="ignore">A boolean flag indicating whether to ignore properties on both export and import.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with specified properties ignored on both export and import.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> IgnoreOnExportAndImport<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, bool ignore = true)
        => metas.Apply(m => m.IgnoreOnImport(ignore).IgnoreOnExport(ignore));

    /// <summary>
    /// Conditionally sets properties as disabled within the collection of ObjectEditPropertyMeta based on a specified condition.
    /// <typeparam name="TModel">The model type used to evaluate the condition.</typeparam>
    /// <param name="metas">The collection of ObjectEditPropertyMeta to be modified.</param>
    /// <param name="condition">The condition that determines if a property should be disabled.</param>
    /// <returns>The modified collection of ObjectEditPropertyMeta with certain properties set as disabled based on the condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> AsDisabledIf<TModel>(this IEnumerable<ObjectEditPropertyMeta> metas, Func<TModel, bool> condition)
        => metas.Apply(m => m.AsDisabledIf(condition));

    /// <summary>
    /// Protects the property from being edited by the user until a confirmation is received.
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithEditConfirmation(this IEnumerable<ObjectEditPropertyMeta> metas, string message,
        AdditionalComponentRenderPosition position = AdditionalComponentRenderPosition.After) => metas.WithEditConfirmation(ConfirmationProtection.CheckBox(message), position);

    /// <summary>
    /// Protects the property from being edited by the user until a confirmation is received.
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMeta> WithEditConfirmation(this IEnumerable<ObjectEditPropertyMeta> metas, IConfirmationProtection protection,
        AdditionalComponentRenderPosition position = AdditionalComponentRenderPosition.After)
        => metas.Apply(m => m.WithEditConfirmation(protection, position));

}
