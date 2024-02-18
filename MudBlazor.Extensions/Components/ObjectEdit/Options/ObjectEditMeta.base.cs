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


    internal static bool IsAllowedAsPropertyToEdit(PropertyInfo p)
    {
        var forbiddenAttributes = new[] { typeof(InjectAttribute), typeof(IgnoreDataMemberAttribute), typeof(IgnoreOnObjectEditAttribute) };
        return forbiddenAttributes.All(a => p.GetCustomAttribute(a) == null);
    }

    internal static bool IsAllowedAsPropertyToEditOnAComponent<T>(PropertyInfo p)
    {
        if (p.PropertyType.IsNullableOf<DateTime>() && p.Name == nameof(MudBaseDatePicker.PickerMonth))
            return false; // TODO: find out why its so hard crashing without this
        if (typeof(T) == typeof(MudChip) && p.Name == nameof(MudChip.Value))
            return false; // TODO: find out why its so hard crashing without this

        var forbiddenTypes = new[] { typeof(EventCallback), typeof(EventCallback<>), typeof(Expression<>), typeof(Func<>), typeof(Converter<>), typeof(Converter<,>), typeof(CultureInfo), typeof(RenderFragment), typeof(RenderFragment<>), typeof(IStringLocalizer<>), typeof(IStringLocalizer) };
        var isIComponent = typeof(ComponentBase).IsAssignableFrom(p.DeclaringType);
        return (!isIComponent || p.GetCustomAttribute<ParameterAttribute>() != null || p.GetCustomAttribute<CascadingParameterAttribute>() != null)
               && !p.PropertyType.IsFunc() && !p.PropertyType.IsExpression() //&& !p.PropertyType.IsAction()
           //    && p.PropertyType is {IsInterface: false, IsAbstract: false} // Bad idea
               && forbiddenTypes.All(t => t != p.PropertyType && (!p.PropertyType.IsGenericType || t != p.PropertyType.GetGenericTypeDefinition()));
    }


    /// <summary>
    /// Returns whether the given property is editable.
    /// </summary>
    public static bool IsEditableComponentParameter(PropertyInfo p)
    {
        return (p.GetCustomAttribute<ParameterAttribute>() != null || p.GetCustomAttribute<CascadingParameterAttribute>() != null)
               && !p.PropertyType.IsEventCallback()
               && !p.PropertyType.IsExpression();
    }

    
}