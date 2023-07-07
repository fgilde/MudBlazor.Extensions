
namespace MudBlazor.Extensions.Attribute;

/// <summary>
/// IgnoreOnObjectEditAttribute if its set to a property this is completely ignored and cant accessed by meta
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IgnoreOnObjectEditAttribute: System.Attribute
{
}