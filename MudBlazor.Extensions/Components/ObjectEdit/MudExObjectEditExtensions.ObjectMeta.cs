using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public static partial class MudExObjectEditExtensions
{
    ///<summary>
    /// Configures binding flags for property discovery in ObjectEditMeta.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="flags">The BindingFlags to use for property discovery.</param>
    /// <returns>The modified ObjectEditMeta instance.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WithBindingFlags<T>(this ObjectEditMeta<T> meta, BindingFlags flags)
        => meta?.SetProperties(m => m.BindingFlags = flags);

    ///<summary>
    /// Sets a custom function for resolving property labels in ObjectEditMeta.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="resolverFunc">The function to use for resolving labels.</param>
    /// <returns>The modified ObjectEditMeta instance.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WithLabelResolver<T>(this ObjectEditMeta<T> meta, Func<PropertyInfo, string> resolverFunc)
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.Settings.LabelResolverFn == ObjectEditPropertyMetaSettings.DefaultLabelResolverFn).Apply(pm => pm.WithLabelResolver(resolverFunc)));

    ///<summary>
    /// Sets a custom function for resolving property descriptions in ObjectEditMeta.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="resolverFunc">The function to use for resolving descriptions.</param>
    /// <returns>The modified ObjectEditMeta instance.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WithDescriptionResolver<T>(this ObjectEditMeta<T> meta, Func<PropertyInfo, string> resolverFunc)
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.Settings.DescriptionResolverFn == ObjectEditPropertyMetaSettings.DefaultDescriptionResolverFn).Apply(pm => pm.WithDescriptionResolver(resolverFunc)));

    ///<summary>
    /// Applies a localization pattern and optional IStringLocalizer for property labels in ObjectEditMeta.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="pattern">The pattern to use for label localization.</param>
    /// <param name="localizer">Optional. The IStringLocalizer to use for localization.</param>
    /// <returns>The modified ObjectEditMeta instance.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WithLabelLocalizerPattern<T>(this ObjectEditMeta<T> meta, string pattern = "Label_{0}", IStringLocalizer localizer = null)
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.Settings.LabelResolverFn == ObjectEditPropertyMetaSettings.DefaultLabelResolverFn).Apply(pm => pm.WithLabelLocalizerPattern(pattern, localizer)));

    ///<summary>
    /// Applies a localization pattern and optional IStringLocalizer for property descriptions in ObjectEditMeta.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="pattern">The pattern to use for description localization.</param>
    /// <param name="localizer">Optional. The IStringLocalizer to use for localization.</param>
    /// <returns>The modified ObjectEditMeta instance.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WithDescriptionLocalizerPattern<T>(this ObjectEditMeta<T> meta, string pattern = "Description_{0}", IStringLocalizer localizer = null)
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.Settings.DescriptionResolverFn == ObjectEditPropertyMetaSettings.DefaultDescriptionResolverFn).Apply(pm => pm.WithDescriptionLocalizerPattern(pattern, localizer)));

    ///<summary>
    /// Configures property ordering for ObjectEditMeta.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="orderFunc">The function to determine the order of properties.</param>
    /// <param name="ascending">Indicates if the ordering should be ascending (default) or descending.</param>
    /// <returns>The modified ObjectEditMeta instance.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WithOrdering<T>(this ObjectEditMeta<T> meta, Func<ObjectEditPropertyMeta, object> orderFunc, bool ascending = true)
        => meta?.SetProperties(m => m.OrderFn = orderFunc, m => m.OrderAscending = ascending);

    ///<summary>
    /// Configures descending property ordering for ObjectEditMeta.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="orderFunc">The function to determine the order of properties.</param>
    /// <returns>The modified ObjectEditMeta instance with descending ordering.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WithOrderingDescending<T>(this ObjectEditMeta<T> meta, Func<ObjectEditPropertyMeta, object> orderFunc)
        => meta?.WithOrdering(orderFunc, false);

    ///<summary>
    /// Wraps each property in a specified wrapper component in ObjectEditMeta.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <typeparam name="TWrapperComponent">The component type to wrap each property with.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="options">Configuration actions for the wrapper component.</param>
    /// <returns>The modified ObjectEditMeta instance.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WrapEachIn<T, TWrapperComponent>(this ObjectEditMeta<T> meta, params Action<TWrapperComponent>[] options) where TWrapperComponent : new()
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.RenderData is { Wrapper: null }).Apply(pm => pm.WrapIn(options)));

    ///<summary>
    /// Wraps each property in a MudItem component in ObjectEditMeta.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="options">Configuration actions for the MudItem component.</param>
    /// <returns>The modified ObjectEditMeta instance.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WrapEachInMudItem<T>(this ObjectEditMeta<T> meta, params Action<MudItem>[] options)
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.RenderData is { Wrapper: null }).Apply(pm => pm.WrapInMudItem(options)));

    ///<summary>
    /// Configures ObjectEditMeta to use separate label components only, for all properties.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <returns>The modified ObjectEditMeta instance.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WithSeparateLabelComponentOnly<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.WithSeparateLabelComponentOnly()));

    ///<summary>
    /// Configures ObjectEditMeta to use separate label components for all properties.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <returns>The modified ObjectEditMeta instance.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WithSeparateLabelComponent<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.WithSeparateLabelComponent()));

    ///<summary>
    /// Configures ObjectEditMeta to use separate validation components for all properties.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <returns>The modified ObjectEditMeta instance.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WithSeparateValidationComponent<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.WithSeparateValidationComponent()));

    ///<summary>
    /// Configures ObjectEditMeta to exclude labels for all properties.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <returns>The modified ObjectEditMeta instance.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WithoutLabel<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.WithoutLabel()));

    ///<summary>
    /// Creates an ObjectEditMeta instance for a given object with optional configuration actions.
    /// <typeparam name="T">The type of the object to create the ObjectEditMeta instance for.</typeparam>
    /// <param name="value">The object to create the ObjectEditMeta instance for.</param>
    /// <param name="configures">A series of actions to configure the newly created ObjectEditMeta instance.</param>
    /// <returns>An ObjectEditMeta instance configured according to the provided actions.</returns>
    ///</summary>
    public static ObjectEditMeta<T> ObjectEditMeta<T>(this T value, params Action<ObjectEditMeta<T>>[] configures)
        => value == null ? null : Options.ObjectEditMeta.Create(value, configures);

    ///<summary>
    /// Sets all properties within the ObjectEditMeta instance to be read-only.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <returns>The ObjectEditMeta instance with all properties set as read-only.</returns>
    ///</summary>
    public static ObjectEditMeta<T> AsReadOnly<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.AsReadOnly()));

    ///<summary>
    /// Disables the underline for all properties within the ObjectEditMeta instance.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="disableUnderline">Indicates whether the underline should be disabled.</param>
    /// <returns>The ObjectEditMeta instance with all properties having underline disabled.</returns>
    ///</summary>
    public static ObjectEditMeta<T> DisableUnderline<T>(this ObjectEditMeta<T> meta, bool disableUnderline = true)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.DisableUnderline(disableUnderline)));

    ///<summary>
    /// Ignores all read-only fields within the ObjectEditMeta instance.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <returns>The ObjectEditMeta instance with all read-only fields ignored.</returns>
    ///</summary>
    public static ObjectEditMeta<T> IgnoreAllReadOnlyFields<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Where(p => !p.Settings.IsEditable).Apply(p => p.Ignore()));

    ///<summary>
    /// Ignores all inherited fields within the ObjectEditMeta instance, except for those specified.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="except">Expressions specifying which inherited fields to not ignore.</param>
    /// <returns>The ObjectEditMeta instance with specified inherited fields not ignored.</returns>
    ///</summary>
    public static ObjectEditMeta<T> IgnoreAllInheritedFields<T>(this ObjectEditMeta<T> meta, params Expression<Func<T, object>>[] except)
        => meta?.SetProperties(m => m.AllProperties.Where(p => !(except ?? Enumerable.Empty<Expression<Func<T, object>>>()).Select(meta.Property).Contains(p) && p.PropertyInfo.DeclaringType != typeof(T)).Apply(p => p.Ignore()));

    ///<summary>
    /// Conditionally ignores all inherited fields within the ObjectEditMeta instance, except for those specified, based on a condition.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="condition">The condition that determines whether the inherited fields should be ignored.</param>
    /// <param name="except">Expressions specifying which inherited fields to not ignore if the condition is met.</param>
    /// <returns>The ObjectEditMeta instance with specified inherited fields conditionally ignored.</returns>
    ///</summary>
    public static ObjectEditMeta<T> IgnoreAllInheritedFieldsIf<T>(this ObjectEditMeta<T> meta, Func<T, bool> condition, params Expression<Func<T, object>>[] except)
        => meta?.SetProperties(m => m.AllProperties.Where(p => !(except ?? Enumerable.Empty<Expression<Func<T, object>>>()).Select(meta.Property).Contains(p) && p.PropertyInfo.DeclaringType != typeof(T)).Apply(p => p.IgnoreIf(condition)));

    ///<summary>
    /// Ignores all fields marked with the Obsolete attribute within the ObjectEditMeta instance.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <returns>The ObjectEditMeta instance with all obsolete fields ignored.</returns>
    ///</summary>
    public static ObjectEditMeta<T> IgnoreAllObsoleteFields<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.PropertyInfo.GetCustomAttribute<ObsoleteAttribute>() != null).Apply(p => p.Ignore()));

    ///<summary>
    /// Groups properties by their types within the ObjectEditMeta instance.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <returns>The ObjectEditMeta instance with properties grouped by their types.</returns>
    ///</summary>
    public static ObjectEditMeta<T> GroupByTypes<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.WithGroup(p.PropertyInfo.PropertyType.Name)));

    ///<summary>
    /// Groups specified types of properties within the ObjectEditMeta instance.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="types">The types of properties to group.</param>
    /// <returns>The ObjectEditMeta instance with specified types of properties grouped.</returns>
    ///</summary>
    public static ObjectEditMeta<T> GroupByTypes<T>(this ObjectEditMeta<T> meta, params Type[] types)
        => meta?.SetProperties(m => m.AllProperties.Where(p => types.Contains(p.PropertyInfo.PropertyType)).Apply(p => p.WithGroup(p.PropertyInfo.PropertyType.Name)));

    ///<summary>
    /// Conditionally ignores all inherited fields within the ObjectEditMeta instance, except for those specified by name, based on a condition.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="condition">The condition that determines whether the inherited fields should be ignored.</param>
    /// <param name="except">Field names that should not be ignored even if the condition is met.</param>
    /// <returns>The ObjectEditMeta instance with specified inherited fields conditionally ignored.</returns>
    ///</summary>
    public static ObjectEditMeta<T> IgnoreAllInheritedFieldsIf<T>(this ObjectEditMeta<T> meta, Func<T, bool> condition, string[] except)
        => meta?.SetProperties(m => m.AllProperties.Where(p => !(except ?? Enumerable.Empty<string>()).Select(m.Property).Contains(p) && p.PropertyInfo.DeclaringType != typeof(T)).Apply(p => p.IgnoreIf(condition)));

    ///<summary>
    /// Conditionally ignores all fields marked with the Obsolete attribute within the ObjectEditMeta instance based on a condition.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="condition">The condition that determines whether the obsolete fields should be ignored.</param>
    /// <returns>The ObjectEditMeta instance with all obsolete fields conditionally ignored.</returns>
    ///</summary>
    public static ObjectEditMeta<T> IgnoreAllObsoleteFieldsIf<T>(this ObjectEditMeta<T> meta, Func<T, bool> condition)
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.PropertyInfo.GetCustomAttribute<ObsoleteAttribute>() != null).Apply(p => p.IgnoreIf(condition)));
    
    ///<summary>
    /// Groups properties by their types within the ObjectEditMeta instance, with custom names for specified types.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="types">Tuples specifying the types and their custom group names.</param>
    /// <returns>The ObjectEditMeta instance with properties grouped by their types and custom names.</returns>
    ///</summary>
    public static ObjectEditMeta<T> GroupByTypes<T>(this ObjectEditMeta<T> meta, params (Type type, string name)[] types)
        => meta?.SetProperties(m => m.AllProperties.Where(p => types.Select(t => t.type).Contains(p.PropertyInfo.PropertyType)).Apply(p => p.WithGroup(types.FirstOrDefault(t => t.type == p.PropertyInfo.PropertyType).name ?? p.PropertyInfo.PropertyType.Name)));

    ///<summary>
    /// Ignores all fields that are not inherited (declared directly within the type T) within the ObjectEditMeta instance.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <returns>The ObjectEditMeta instance with all non-inherited fields ignored.</returns>
    ///</summary>
    public static ObjectEditMeta<T> IgnoreAllNotInheritedFields<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.PropertyInfo.DeclaringType == typeof(T)).Apply(p => p.Ignore()));

    ///<summary>
    /// Ignores specific fields by name within the ObjectEditMeta instance.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="fieldNames">The names of the fields to ignore.</param>
    /// <returns>The ObjectEditMeta instance with specified fields ignored.</returns>
    ///</summary>
    public static ObjectEditMeta<T> IgnoreFields<T>(this ObjectEditMeta<T> meta, params string[] fieldNames)
        => meta?.SetProperties(m => fieldNames.Apply(f => m.Property(f)?.Ignore()));

    ///<summary>
    /// Ignores specific fields by expressions within the ObjectEditMeta instance.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="fields">Expressions pointing to the fields to ignore.</param>
    /// <returns>The ObjectEditMeta instance with specified fields ignored.</returns>
    ///</summary>
    public static ObjectEditMeta<T> IgnoreFields<T>(this ObjectEditMeta<T> meta, params Expression<Func<T, object>>[] fields)
        => meta?.SetProperties(m => fields.Apply(f => m.Property(f)?.Ignore()));

    ///<summary>
    /// Adds a custom property resolver function to the ObjectEditMeta instance.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="shouldHandle">The function that determines whether a property should be handled.</param>
    /// <returns>The ObjectEditMeta instance with the custom property resolver function added.</returns>
    ///</summary>
    public static ObjectEditMeta<T> WithPropertyResolverFunc<T>(this ObjectEditMeta<T> meta, Func<PropertyInfo, bool> shouldHandle)
        => meta?.SetProperties(m => m.PropertyResolverFunctions.Add(shouldHandle));

    ///<summary>
    /// Groups properties within the ObjectEditMeta instance using a custom grouping function.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="groupFunc">The function to determine the group name for each property.</param>
    /// <returns>The ObjectEditMeta instance with properties grouped according to the custom function.</returns>
    ///</summary>
    public static ObjectEditMeta<T> GroupBy<T>(this ObjectEditMeta<T> meta, Func<ObjectEditPropertyMeta, string> groupFunc)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.WithGroup(groupFunc(p))));

    ///<summary>
    /// Groups properties within the ObjectEditMeta instance using a custom grouping function based on PropertyInfo.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="groupFunc">The function to determine the group name based on PropertyInfo.</param>
    /// <returns>The ObjectEditMeta instance with properties grouped according to the custom function.</returns>
    ///</summary>
    public static ObjectEditMeta<T> GroupBy<T>(this ObjectEditMeta<T> meta, Func<PropertyInfo, string> groupFunc)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.WithGroup(groupFunc(p.PropertyInfo))));

    ///<summary>
    /// Groups properties within the ObjectEditMeta instance based on a specified attribute and a function to determine the group name from that attribute.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <typeparam name="TAttribute">The attribute type to base the grouping on.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="groupFunc">The function that extracts the group name from the attribute.</param>
    /// <returns>The ObjectEditMeta instance with properties grouped based on the specified attribute.</returns>
    ///</summary>
    public static ObjectEditMeta<T> GroupByAttribute<T, TAttribute>(this ObjectEditMeta<T> meta, Func<TAttribute, string> groupFunc)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.WithGroup(groupFunc(p.PropertyInfo.GetAttributes<TAttribute>(true).FirstOrDefault()))));

    ///<summary>
    /// Groups properties within the ObjectEditMeta instance based on the CategoryAttribute, with a fallback group name if the attribute is not present.
    /// <typeparam name="T">The type of the object being edited.</typeparam>
    /// <param name="meta">The ObjectEditMeta instance to configure.</param>
    /// <param name="fallbackGroupName">The fallback group name to use if the CategoryAttribute is not present.</param>
    /// <returns>The ObjectEditMeta instance with properties grouped by the CategoryAttribute or the fallback name.</returns>
    ///</summary>
    public static ObjectEditMeta<T> GroupByCategoryAttribute<T>(this ObjectEditMeta<T> meta, string fallbackGroupName = "Default")
        => meta.GroupByAttribute<T, CategoryAttribute>(attribute => attribute is SafeCategoryAttribute safeCategory
            ? safeCategory.Name
            : attribute?.Name ?? fallbackGroupName);
    
}