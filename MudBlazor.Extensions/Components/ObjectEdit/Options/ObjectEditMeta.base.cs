using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Attribute;
using Nextended.Blazor.Extensions;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Base class for ObjectEditMeta instances, providing a set of properties and methods to configure the meta instance.
/// </summary>
public abstract class ObjectEditMeta
{
    /// <summary>
    /// Parent ObjectEditMeta instance, if any.
    /// </summary>
    public ObjectEditMeta Parent { get; internal set; }
    
    /// <summary>
    /// All properties of the object to be edited.
    /// </summary>
    public abstract IList<ObjectEditPropertyMeta> AllProperties { get; }
    
    /// <summary>
    /// All ignored properties of the object to be edited.
    /// </summary>
    public virtual IList<ObjectEditPropertyMeta> AllIgnored => AllProperties.Where(m => !m.ShouldRender()).Distinct().ToList();
    
    /// <summary>
    /// Returns all properties that are rendered with a component of type <typeparamref name="TComponent"/>.
    /// </summary>
    public virtual IList<ObjectEditPropertyMeta> AllRenderedWith<TComponent>() => AllRenderedWith(typeof(TComponent));
    
    /// <summary>
    /// Returns all properties that are rendered with a component of the given type.
    /// </summary>
    public virtual IList<ObjectEditPropertyMeta> AllRenderedWith(Type componentType) => AllProperties.Where(m => m?.RenderData?.ComponentType == componentType).Distinct().ToList();
    
    /// <summary>
    /// Binding flags to be used when reflecting over the object to be edited.
    /// </summary>
    public BindingFlags BindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance;
    
    /// <summary>
    /// Returns the ObjectEditPropertyMeta instance for the property with the given name.
    /// </summary>
    public ObjectEditPropertyMeta Property(string name) => AllProperties.FirstOrDefault(m => m.PropertyName == name);
    
    /// <summary>
    /// Returns the ObjectEditPropertyMeta instances for the properties with the given names.
    /// </summary>
    public IEnumerable<ObjectEditPropertyMeta> Properties(params string[] names) => names?.Select(Property) ?? Enumerable.Empty<ObjectEditPropertyMeta>();
    
    /// <summary>
    /// Returns the ObjectEditPropertyMeta instance for the property with the given path.
    /// </summary>
    public ObjectEditPropertyMeta Property(params MemberInfo[] path) => Property(AllProperties, path);
    
    private ObjectEditPropertyMeta Property(IEnumerable<ObjectEditPropertyMeta> properties, IList<MemberInfo> member)
    {
        var res = properties.FirstOrDefault(m => m.PropertyInfo == member.First());
        return member.Count == 1 ? res : Property(res?.Children.EmptyIfNull(), member.Skip(1).ToArray());
    }

    /// <summary>
    /// Creates a new ObjectEditMeta instance for the given object, with the given configuration actions applied.
    /// </summary>
    public static ObjectEditMeta<T> Create<T>(T value, params Action<ObjectEditMeta<T>>[] configures)
    {
        if (value == null)
            return null;
        var res = new ObjectEditMeta<T>(value).WithPropertyResolverFunc(IsAllowedAsPropertyToEdit);
        if(value is IComponent)
            res = res.WithPropertyResolverFunc(IsAllowedAsPropertyToEditOnAComponent<T>);
        configures.EmptyIfNull().Apply(c => c?.Invoke(res));
        return res;
    }


    private static readonly ConcurrentDictionary<PropertyInfo, bool> _allowedPropertyCache = new();
    private static readonly Type[] ForbiddenAttributes = { typeof(InjectAttribute), typeof(IgnoreDataMemberAttribute), typeof(IgnoreOnObjectEditAttribute) };

    internal static bool IsAllowedAsPropertyToEdit(PropertyInfo p)
    {
        return _allowedPropertyCache.GetOrAdd(p, prop => ForbiddenAttributes.All(a => prop.GetCustomAttribute(a) == null));
    }

    private static readonly ConcurrentDictionary<(Type, PropertyInfo), bool> _componentPropertyAllowedCache = new();
    private static readonly Type[] ForbiddenComponentTypes = { typeof(EventCallback), typeof(EventCallback<>), typeof(Expression<>), typeof(Func<>), typeof(Converter<,>), typeof(Converter<,>), typeof(CultureInfo), typeof(RenderFragment), typeof(RenderFragment<>), typeof(IStringLocalizer<>), typeof(IStringLocalizer) };

    internal static bool IsAllowedAsPropertyToEditOnAComponent<T>(PropertyInfo p)
    {
        return _componentPropertyAllowedCache.GetOrAdd((typeof(T), p), key =>
        {
            var (ownerType, prop) = key;
            if (prop.PropertyType.IsNullableOf<DateTime>() && prop.Name == nameof(MudBaseDatePicker.PickerMonth))
                return false;
            if (ownerType == typeof(MudChip<>) && prop.Name == nameof(MudChip<T>.Value))
                return false;

            var isIComponent = typeof(ComponentBase).IsAssignableFrom(prop.DeclaringType);
            return (!isIComponent || prop.GetCustomAttribute<ParameterAttribute>() != null || prop.GetCustomAttribute<CascadingParameterAttribute>() != null)
                   && !prop.PropertyType.IsFunc() && !prop.PropertyType.IsExpression()
                   && ForbiddenComponentTypes.All(t => t != prop.PropertyType && (!prop.PropertyType.IsGenericType || t != prop.PropertyType.GetGenericTypeDefinition()));
        });
    }


    private static readonly ConcurrentDictionary<PropertyInfo, bool> _editableComponentParamCache = new();

    /// <summary>
    /// Returns whether the given property is editable.
    /// </summary>
    public static bool IsEditableComponentParameter(PropertyInfo p)
    {
        return _editableComponentParamCache.GetOrAdd(p, prop =>
            (prop.GetCustomAttribute<ParameterAttribute>() != null || prop.GetCustomAttribute<CascadingParameterAttribute>() != null)
            && !prop.PropertyType.IsEventCallback()
            && !prop.PropertyType.IsExpression());
    }

    
}