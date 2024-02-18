using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;


public static partial class MudExObjectEditExtensions
{
    /// <summary>
    /// Extends the collection of ObjectEditPropertyMetaOf with a component for rendering, providing custom options, field type converters, and the ability to specify instance attributes for the component.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component to render.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property.</typeparam>
    /// <typeparam name="TFieldType">The field type of the component.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="valueField">The expression to identify the component's value field.</param>
    /// <param name="options">An action to configure the component instance.</param>
    /// <param name="toFieldTypeConverter">An optional converter from the property type to the field type.</param>
    /// <param name="toPropertyTypeConverter">An optional converter from the field type back to the property type.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf including the specified rendering component.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent, TPropertyType, TFieldType>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, options, toFieldTypeConverter, toPropertyTypeConverter));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMetaOf with a component for rendering, specifying instance attributes directly on a component instance, and allowing for custom field type converters.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component to render.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property.</typeparam>
    /// <typeparam name="TFieldType">The field type of the component.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="valueField">The expression to identify the component's value field.</param>
    /// <param name="instanceForAttributes">An instance of the component for specifying attributes.</param>
    /// <param name="toFieldTypeConverter">An optional converter from the property type to the field type.</param>
    /// <param name="toPropertyTypeConverter">An optional converter from the field type back to the property type.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf including the specified rendering component with instance attributes.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent, TPropertyType, TFieldType>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, instanceForAttributes, toFieldTypeConverter, toPropertyTypeConverter));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMetaOf with a component for rendering, allowing for custom field type converters without specifying options or attributes.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component to render.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property.</typeparam>
    /// <typeparam name="TFieldType">The field type of the component.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="valueField">The expression to identify the component's value field.</param>
    /// <param name="toFieldTypeConverter">An optional converter from the property type to the field type.</param>
    /// <param name="toPropertyTypeConverter">An optional converter from the field type back to the property type.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf for rendering with the specified component and converters.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent, TPropertyType, TFieldType>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Expression<Func<TComponent, TFieldType>> valueField, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null)
        => metas.Apply(m => m.RenderWith(valueField, toFieldTypeConverter, toPropertyTypeConverter));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMetaOf with a component for rendering, providing custom options without specifying field type converters.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component to render.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="valueField">The expression to identify the component's value field.</param>
    /// <param name="options">An action to configure the component instance.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf for rendering with the specified component and options.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent, TPropertyType>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Expression<Func<TComponent, TPropertyType>> valueField, Action<TComponent> options) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, options));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMetaOf with a component for rendering, specifying instance attributes directly without custom options or converters.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component to render.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="valueField">The expression to identify the component's value field.</param>
    /// <param name="instanceForAttributes">An instance of the component for specifying attributes.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf for rendering with the specified component and instance attributes.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent, TPropertyType>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Expression<Func<TComponent, TPropertyType>> valueField, TComponent instanceForAttributes) where TComponent : new()
        => metas.Apply(m => m.RenderWith(valueField, instanceForAttributes));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMetaOf with a component for rendering without specifying options, attributes, or converters.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component to render.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="valueField">The expression to identify the component's value field.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf for basic rendering with the specified component.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent, TPropertyType>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Expression<Func<TComponent, TPropertyType>> valueField)
        => metas.Apply(m => m.RenderWith(valueField));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMetaOf with generic component rendering, providing custom options.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component to render.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="options">An action to configure the component instance.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf for rendering with any specified component and options.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Action<TComponent> options) where TComponent : new()
        => metas.Apply(m => m.RenderWith<TComponent>(options));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMetaOf with a generic component rendering, specifying custom attributes.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component to render.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="attributes">A dictionary of attributes to apply to the component.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf for rendering with specified attributes.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, IDictionary<string, object> attributes)
        => metas.Apply(m => m.RenderWith<TComponent>(attributes));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMetaOf with a non-generic component rendering, specifying custom attributes.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="componentType">The type of the component to render.</param>
    /// <param name="attributes">An optional dictionary of attributes to apply to the component.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf for rendering with a specified non-generic component and attributes.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Type componentType, IDictionary<string, object> attributes = null)
        => metas.Apply(m => m.RenderWith(componentType, attributes));

    /// <summary>
    /// Extends the collection of ObjectEditPropertyMetaOf with custom rendering using an ICustomRenderer interface implementation.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="renderer">The custom renderer to use.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf for rendering with a specified custom renderer.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> RenderWith<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, ICustomRenderer renderer)
        => metas.Apply(m => m.RenderWith(renderer));

    /// <summary>
    /// Sets the specified collection of ObjectEditPropertyMetaOf to read-only based on the provided condition.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="isReadOnly">A boolean indicating if the properties should be read-only.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with the read-only setting applied.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> AsReadOnly<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool isReadOnly = true)
        => metas.Apply(m => m.AsReadOnly(isReadOnly));

    /// <summary>
    /// Disables the underline for the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="disableUnderline">A boolean indicating if the underline should be disabled.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with the underline disabled.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> DisableUnderline<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool disableUnderline = true)
        => metas.Apply(m => m.DisableUnderline(disableUnderline));

    /// <summary>
    /// Ignores the specified collection of ObjectEditPropertyMetaOf from being rendered or processed.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="ignore">A boolean indicating if the properties should be ignored.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with the specified properties ignored.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> Ignore<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool ignore = true)
        => metas.Apply(m => m.Ignore(ignore));

    /// <summary>
    /// Configures reset options for the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="allowReset">A boolean indicating if reset functionality should be enabled.</param>
    /// <param name="resetIcon">The icon to use for the reset button.</param>
    /// <param name="showResetText">A boolean indicating if text should be shown next to the reset icon.</param>
    /// <param name="resetText">The text to display next to the reset icon.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with reset options configured.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithResetOptions<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool allowReset, string resetIcon, bool showResetText, string resetText)
        => metas.Apply(m => m.WithResetOptions(allowReset, resetIcon, showResetText, resetText));

    /// <summary>
    /// Configures reset options for the specified collection of ObjectEditPropertyMetaOf using a PropertyResetSettings object.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="resetSettings">The PropertyResetSettings object containing reset configuration.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with reset options configured.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithResetOptions<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, PropertyResetSettings resetSettings)
        => metas.Apply(m => m.WithResetOptions(resetSettings));

    /// <summary>
    /// Configures reset options for the specified collection of ObjectEditPropertyMetaOf using a delegate action.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="resetSettingsAction">An action delegate to configure the reset settings.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with reset options configured.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithResetOptions<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Action<PropertyResetSettings> resetSettingsAction)
        => metas.Apply(m => m.WithResetOptions(resetSettingsAction));

    /// <summary>
    /// Marks the specified collection of ObjectEditPropertyMetaOf as resettable based on the provided condition.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="resettable">A boolean indicating if the properties should be resettable.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf marked as resettable.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> AreResettable<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool resettable = true)
        => metas.Apply(m => m.IsResettable(resettable));

    /// <summary>
    /// Marks the specified collection of ObjectEditPropertyMetaOf as not resettable.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf marked as not resettable.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> NotResettable<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas)
        => metas.Apply(m => m.NotResettable());

    /// <summary>
    /// Applies custom settings to the specified collection of ObjectEditPropertyMetaOf using a delegate action.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="settingsAction">An action delegate to configure the settings.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with custom settings applied.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithSettings<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Action<ObjectEditPropertyMetaSettings> settingsAction)
        => metas.Apply(m => m.WithSettings(settingsAction));

    /// <summary>
    /// Separates the label component for the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with separate label components.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithSeparateLabelComponent<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas)
        => metas.Apply(m => m.WithSeparateLabelComponent());

    /// <summary>
    /// Separates the validation component for the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with separate validation components.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithSeparateValidationComponent<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas)
        => metas.Apply(m => m.WithSeparateValidationComponent());

    /// <summary>
    /// Removes the label for the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf without labels.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithoutLabel<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas)
        => metas.Apply(m => m.WithoutLabel());

    /// <summary>
    /// Applies only a separate label component for the specified collection of ObjectEditPropertyMetaOf, excluding other configurations.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with only separate label components.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithSeparateLabelComponentOnly<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas)
        => metas.Apply(m => m.WithSeparateLabelComponentOnly());

    /// <summary>
    /// Applies default labeling for the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with default labeling applied.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithDefaultLabeling<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas)
        => metas.Apply(m => m.WithDefaultLabeling());

    /// <summary>
    /// Orders the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="order">The order in which the properties should be displayed.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf ordered as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithOrder<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, int order)
        => metas.Apply(m => m.WithOrder(order));

    /// <summary>
    /// Labels the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="label">The label to apply to the properties.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with specified labels.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithLabel<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, string label)
        => metas.Apply(m => m.WithLabel(label));

    /// <summary>
    /// Describes the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="description">The description to apply to the properties.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with specified descriptions.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithDescription<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, string description)
        => metas.Apply(m => m.WithDescription(description));

    /// <summary>
    /// Applies a label resolver for the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="resolverFunc">The function to resolve labels dynamically.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with dynamic label resolution.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithLabelResolver<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<PropertyInfo, string> resolverFunc)
        => metas.Apply(m => m.WithLabelResolver(resolverFunc));

    /// <summary>
    /// Applies a description resolver for the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="resolverFunc">The function to resolve descriptions dynamically.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with dynamic description resolution.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithDescriptionResolver<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<PropertyInfo, string> resolverFunc)
        => metas.Apply(m => m.WithDescriptionResolver(resolverFunc));

    /// <summary>
    /// Applies a label localizer pattern for the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="pattern">The pattern to use for localizing labels.</param>
    /// <param name="localizer">An optional IStringLocalizer to apply for localization.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with localized labels based on the specified pattern.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithLabelLocalizerPattern<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, string pattern = "Label_{0}", IStringLocalizer localizer = null)
        => metas.Apply(m => m.WithLabelLocalizerPattern(pattern, localizer));

    /// <summary>
    /// Applies a description localizer pattern for the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="pattern">The pattern to use for localizing descriptions.</param>
    /// <param name="localizer">An optional IStringLocalizer to apply for localization.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with localized descriptions based on the specified pattern.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithDescriptionLocalizerPattern<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, string pattern = "Description_{0}", IStringLocalizer localizer = null)
        => metas.Apply(m => m.WithDescriptionLocalizerPattern(pattern, localizer));

    /// <summary>
    /// Groups the specified collection of ObjectEditPropertyMetaOf under a specified group name.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="groupName">The name of the group to assign the properties to.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf grouped as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithGroup<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, string groupName)
        => metas.Apply(m => m.WithGroup(groupName));

    /// <summary>
    /// Groups the specified collection of ObjectEditPropertyMetaOf under a specified group information object.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="groupInfo">The ObjectEditPropertyMetaGroupInfo object containing group information.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf grouped as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithGroup<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, ObjectEditPropertyMetaGroupInfo groupInfo)
        => metas.Apply(m => m.WithGroup(groupInfo));

    /// <summary>
    /// Adds additional attributes to the specified collection of ObjectEditPropertyMetaOf.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="attributes">An array of key-value pairs representing additional attributes to add.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with additional attributes added.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, params KeyValuePair<string, object>[] attributes)
        => metas.Apply(m => m.WithAdditionalAttributes(attributes));

    /// <summary>
    /// Adds additional attributes to the specified collection of ObjectEditPropertyMetaOf, with an option to overwrite existing attributes.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="overwriteExisting">A boolean indicating if existing attributes should be overwritten.</param>
    /// <param name="attributes">An array of key-value pairs representing additional attributes to add.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with additional attributes added or overwritten as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool overwriteExisting, params KeyValuePair<string, object>[] attributes)
        => metas.Apply(m => m.WithAdditionalAttributes(overwriteExisting, attributes));

    /// <summary>
    /// Adds additional attributes to the specified collection of ObjectEditPropertyMetaOf, with an option to overwrite existing attributes.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="attributes">A dictionary of attributes to add.</param>
    /// <param name="overwriteExisting">A boolean indicating if existing attributes should be overwritten.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with additional attributes added or overwritten as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, IDictionary<string, object> attributes, bool overwriteExisting = false)
        => metas.Apply(m => m.WithAdditionalAttributes(attributes, overwriteExisting));

    /// <summary>
    /// Adds additional attributes to the specified collection of ObjectEditPropertyMetaOf, with an option to overwrite existing attributes.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="attributes">A dictionary of attributes to add.</param>
    /// <param name="overwriteExisting">A boolean indicating if existing attributes should be overwritten.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with additional attributes added or overwritten as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Dictionary<string, object> attributes, bool overwriteExisting = false)
        => metas.Apply(m => m.WithAdditionalAttributes(attributes, overwriteExisting));

    /// <summary>
    /// Adds a single additional attribute to the specified collection of ObjectEditPropertyMetaOf, with an option to overwrite existing attributes.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="key">The key of the attribute to add.</param>
    /// <param name="value">The value of the attribute to add.</param>
    /// <param name="overwriteExisting">A boolean indicating if the attribute should overwrite an existing attribute with the same key.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with the additional attribute added or overwritten as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttribute<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, string key, object value, bool overwriteExisting = false)
        => metas.Apply(m => m.WithAdditionalAttribute(key, value, overwriteExisting));

    /// <summary>
    /// Adds additional attributes to the specified collection of ObjectEditPropertyMetaOf using component options, with an option to overwrite existing attributes.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component for which attributes are being added.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="overwriteExisting">A boolean indicating if existing attributes should be overwritten.</param>
    /// <param name="options">An array of actions to configure the component instances.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with additional attributes added or overwritten as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool overwriteExisting, params Action<TComponent>[] options) where TComponent : new()
        => metas.Apply(m => m.WithAdditionalAttributes<TComponent>(overwriteExisting, options));

    /// <summary>
    /// Adds additional attributes to the specified collection of ObjectEditPropertyMetaOf using component options.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component for which attributes are being added.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="options">An array of actions to configure the component instances.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with additional attributes added as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, params Action<TComponent>[] options) where TComponent : new()
        => metas.Apply(m => m.WithAdditionalAttributes<TComponent>(options));

    /// <summary>
    /// Adds additional attributes to the specified collection of ObjectEditPropertyMetaOf using a component instance, with an option to overwrite existing attributes.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component for which attributes are being added.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="instanceForAttributes">The component instance for specifying attributes.</param>
    /// <param name="overwriteExisting">A boolean indicating if existing attributes should be overwritten.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with additional attributes added or overwritten as specified.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAdditionalAttributes<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, TComponent instanceForAttributes, bool overwriteExisting = false) where TComponent : new()
        => metas.Apply(m => m.WithAdditionalAttributes<TComponent>(instanceForAttributes, overwriteExisting));

    /// <summary>
    /// Ignores properties in the specified collection of ObjectEditPropertyMetaOf based on a condition.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="condition">A function that determines whether a property should be ignored.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with properties ignored based on the specified condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> IgnoreIf<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition)
        => metas.Apply(m => m.IgnoreIf(condition));

    /// <summary>
    /// Sets properties in the specified collection of ObjectEditPropertyMetaOf to read-only based on a condition.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="condition">A function that determines whether a property should be read-only.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with properties set to read-only based on the specified condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> AsReadOnlyIf<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition)
        => metas.Apply(m => m.AsReadOnlyIf(condition));

    /// <summary>
    /// Adds attributes to the specified collection of ObjectEditPropertyMetaOf based on a condition.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="condition">A function that determines whether attributes should be added.</param>
    /// <param name="attributes">An array of key-value pairs representing attributes to add.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with attributes added based on the specified condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAttributesIf<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition, params KeyValuePair<string, object>[] attributes)
        => metas.Apply(m => m.WithAttributesIf(condition, attributes));

    /// <summary>
    /// Adds attributes to the specified collection of ObjectEditPropertyMetaOf based on a condition, using component options.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component for which attributes are being added.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="condition">A function that determines whether attributes should be added.</param>
    /// <param name="options">An array of actions to configure the component instances.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with attributes added based on the specified condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAttributesIf<TModel, TComponent>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition, params Action<TComponent>[] options) where TComponent : new()
        => metas.Apply(m => m.WithAttributesIf(condition, options));

    /// <summary>
    /// Adds attributes to the specified collection of ObjectEditPropertyMetaOf based on a condition, using a dictionary of attributes.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="condition">A function that determines whether attributes should be added.</param>
    /// <param name="attributes">A dictionary of attributes to add.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with attributes added based on the specified condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAttributesIf<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition, IDictionary<string, object> attributes)
        => metas.Apply(m => m.WithAttributesIf(condition, attributes));

    /// <summary>
    /// Adds attributes to the specified collection of ObjectEditPropertyMetaOf based on a condition, using a dictionary of attributes.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="condition">A function that determines whether attributes should be added.</param>
    /// <param name="attributes">A dictionary of attributes to add.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with attributes added based on the specified condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> WithAttributesIf<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition, Dictionary<string, object> attributes)
        => metas.Apply(m => m.WithAttributesIf(condition, attributes));

    /// <summary>
    /// Ignores properties in the specified collection of ObjectEditPropertyMetaOf during export based on the provided boolean.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="ignore">A boolean indicating if the properties should be ignored during export.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with specified properties ignored during export.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> IgnoreOnExport<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool ignore = true)
        => metas.Apply(m => m.IgnoreOnExport(ignore));

    /// <summary>
    /// Ignores properties in the specified collection of ObjectEditPropertyMetaOf during import based on the provided boolean.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="ignore">A boolean indicating if the properties should be ignored during import.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with specified properties ignored during import.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> IgnoreOnImport<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool ignore = true)
        => metas.Apply(m => m.IgnoreOnImport(ignore));

    /// <summary>
    /// Ignores properties in the specified collection of ObjectEditPropertyMetaOf during both export and import based on the provided boolean.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="ignore">A boolean indicating if the properties should be ignored during both export and import.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with specified properties ignored during both export and import.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> IgnoreOnExportAndImport<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, bool ignore = true)
        => metas.Apply(m => m.IgnoreOnImport(ignore).IgnoreOnExport(ignore));

    /// <summary>
    /// Disables properties in the specified collection of ObjectEditPropertyMetaOf based on a condition.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="metas">The collection of metadata definitions.</param>
    /// <param name="condition">A function that determines whether a property should be disabled.</param>
    /// <returns>A modified IEnumerable of ObjectEditPropertyMetaOf with properties disabled based on the specified condition.</returns>
    /// </summary>
    public static IEnumerable<ObjectEditPropertyMetaOf<TModel>> AsDisabledIf<TModel>(this IEnumerable<ObjectEditPropertyMetaOf<TModel>> metas, Func<TModel, bool> condition)
        => metas.Apply(m => m.AsDisabledIf(condition));
}