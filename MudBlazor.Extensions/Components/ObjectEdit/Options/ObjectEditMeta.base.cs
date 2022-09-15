using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

public abstract class ObjectEditMeta
{
    public ObjectEditMeta Parent { get; internal set; }
    public abstract IList<ObjectEditPropertyMeta> AllProperties { get; }
    public BindingFlags BindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance;
    public ObjectEditPropertyMeta Property(string name) => AllProperties.FirstOrDefault(m => m.PropertyName == name);
    public IEnumerable<ObjectEditPropertyMeta> Properties(params string[] names) => names.Select(Property);
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
        var res = new ObjectEditMeta<T>(value);
        if (value is IComponent)
            res = res.WithPropertyResolverFunc(IsEditableComponentParameter);
        configures.EmptyIfNull().Apply(c => c?.Invoke(res));
        return res;
    }

    static bool IsEventCallback(Type type) => (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>) || type == typeof(EventCallback));
    static bool IsExpression(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Expression<>);

    private static bool IsEditableComponentParameter(PropertyInfo p)
    {
        return (p.GetCustomAttribute<ParameterAttribute>() != null || p.GetCustomAttribute<CascadingParameterAttribute>() != null) && !IsEventCallback(p.PropertyType) && !IsExpression(p.PropertyType);
    }
}