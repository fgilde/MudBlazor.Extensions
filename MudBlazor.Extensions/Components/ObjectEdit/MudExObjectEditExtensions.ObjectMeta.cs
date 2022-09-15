using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public static partial class MudExObjectEditExtensions
{
    // ObjectEditMeta Extensions
    public static ObjectEditMeta<T> WithBindingFlags<T>(this ObjectEditMeta<T> meta, BindingFlags flags)
        => meta?.SetProperties(m => m.BindingFlags = flags);
    public static ObjectEditMeta<T> WithLabelResolver<T>(this ObjectEditMeta<T> meta, Func<PropertyInfo, string> resolverFunc)
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.Settings.LabelResolverFn == ObjectEditPropertyMetaSettings.DefaultLabelResolverFn).Apply(pm => pm.WithLabelResolver(resolverFunc)));
    public static ObjectEditMeta<T> WithDescriptionResolver<T>(this ObjectEditMeta<T> meta, Func<PropertyInfo, string> resolverFunc)
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.Settings.DescriptionResolverFn == ObjectEditPropertyMetaSettings.DefaultDescriptionResolverFn).Apply(pm => pm.WithDescriptionResolver(resolverFunc)));
    public static ObjectEditMeta<T> WithLabelLocalizerPattern<T>(this ObjectEditMeta<T> meta, string pattern = "Label_{0}", IStringLocalizer localizer = null)
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.Settings.LabelResolverFn == ObjectEditPropertyMetaSettings.DefaultLabelResolverFn).Apply(pm => pm.WithLabelLocalizerPattern(pattern, localizer)));
    public static ObjectEditMeta<T> WithDescriptionLocalizerPattern<T>(this ObjectEditMeta<T> meta, string pattern = "Description_{0}", IStringLocalizer localizer = null)
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.Settings.DescriptionResolverFn == ObjectEditPropertyMetaSettings.DefaultDescriptionResolverFn).Apply(pm => pm.WithDescriptionLocalizerPattern(pattern, localizer)));
    public static ObjectEditMeta<T> WithOrdering<T>(this ObjectEditMeta<T> meta, Func<ObjectEditPropertyMeta, object> orderFunc, bool ascending = true)
        => meta?.SetProperties(m => m.OrderFn = orderFunc, m => m.OrderAscending = ascending);
    public static ObjectEditMeta<T> WithOrderingDescending<T>(this ObjectEditMeta<T> meta, Func<ObjectEditPropertyMeta, object> orderFunc)
        => meta?.WithOrdering(orderFunc, false);
    public static ObjectEditMeta<T> WrapEachIn<T, TWrapperComponent>(this ObjectEditMeta<T> meta, params Action<TWrapperComponent>[] options) where TWrapperComponent : new()
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.RenderData is { Wrapper: null }).Apply(pm => pm.WrapIn(options)));
    public static ObjectEditMeta<T> WrapEachInMudItem<T>(this ObjectEditMeta<T> meta, params Action<MudItem>[] options)
        => meta?.SetProperties(m => m.AllProperties.Where(p => p.RenderData is { Wrapper: null }).Apply(pm => pm.WrapInMudItem(options)));
    public static ObjectEditMeta<T> WithSeparateLabelComponentOnly<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.WithSeparateLabelComponentOnly()));
    public static ObjectEditMeta<T> WithSeparateLabelComponent<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.WithSeparateLabelComponent()));
    public static ObjectEditMeta<T> WithSeparateValidationComponent<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.WithSeparateValidationComponent()));
    public static ObjectEditMeta<T> WithoutLabel<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.WithoutLabel()));
    public static ObjectEditMeta<T> ObjectEditMeta<T>(this T value, params Action<ObjectEditMeta<T>>[] configures)
        => value == null ? null : Options.ObjectEditMeta.Create(value, configures);
    public static ObjectEditMeta<T> AsReadOnly<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.AsReadOnly()));
    public static ObjectEditMeta<T> DisableUnderline<T>(this ObjectEditMeta<T> meta, bool disableUnderline = true)
        => meta?.SetProperties(m => m.AllProperties.Apply(p => p.DisableUnderline(disableUnderline)));
    public static ObjectEditMeta<T> IgnoreAllReadOnlyFields<T>(this ObjectEditMeta<T> meta)
        => meta?.SetProperties(m => m.AllProperties.Where(p => !p.Settings.IsEditable).Apply(p => p.Ignore()));
    public static ObjectEditMeta<T> IgnoreFields<T>(this ObjectEditMeta<T> meta, params string[] fieldNames)
        => meta?.SetProperties(m => fieldNames.Apply(f => m.Property(f)?.Ignore()));
    public static ObjectEditMeta<T> IgnoreFields<T>(this ObjectEditMeta<T> meta, params Expression<Func<T, object>>[] fields)
        => meta?.SetProperties(m => fields.Apply(f => m.Property(f)?.Ignore()));
    public static ObjectEditMeta<T> WithPropertyResolverFunc<T>(this ObjectEditMeta<T> meta, Func<PropertyInfo, bool> shouldHandle)
        => meta?.SetProperties(m => m.PropertyResolverFunc = shouldHandle);

}