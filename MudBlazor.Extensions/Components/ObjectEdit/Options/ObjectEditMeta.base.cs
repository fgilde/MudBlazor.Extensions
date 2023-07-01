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

public abstract class ObjectEditMeta
{
    public ObjectEditMeta Parent { get; internal set; }
    public abstract IList<ObjectEditPropertyMeta> AllProperties { get; }
    public virtual IList<ObjectEditPropertyMeta> AllIgnored => AllProperties.Where(m => !m.ShouldRender()).Distinct().ToList();
    public BindingFlags BindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance;
    public ObjectEditPropertyMeta Property(string name) => AllProperties.FirstOrDefault(m => m.PropertyName == name);
    public IEnumerable<ObjectEditPropertyMeta> Properties(params string[] names) => names?.Select(Property) ?? Enumerable.Empty<ObjectEditPropertyMeta>();
    public ObjectEditPropertyMeta Property(params MemberInfo[] path) => Property(AllProperties, path);
    private ObjectEditPropertyMeta Property(IEnumerable<ObjectEditPropertyMeta> properties, IList<MemberInfo> member)
    {
        var res = properties.FirstOrDefault(m => m.PropertyInfo == member.First());
        return member.Count == 1 ? res : Property(res?.Children.EmptyIfNull(), member.Skip(1).ToArray());
    }

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


    private static bool IsAllowedAsPropertyToEdit(PropertyInfo p)
    {
        var forbiddenAttributes = new[] { typeof(InjectAttribute), typeof(IgnoreDataMemberAttribute), typeof(IgnoreOnObjectEditAttribute) };
        return forbiddenAttributes.All(a => p.GetCustomAttribute(a) == null);
    }

    private static bool IsAllowedAsPropertyToEditOnAComponent<T>(PropertyInfo p)
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

    
    private static bool IsEditableComponentParameter(PropertyInfo p)
    {
        return (p.GetCustomAttribute<ParameterAttribute>() != null || p.GetCustomAttribute<CascadingParameterAttribute>() != null)
               && !p.PropertyType.IsEventCallback()
               && !p.PropertyType.IsExpression();
    }

    
}