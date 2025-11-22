using Nextended.Core.Types;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Represents a IRange implementation
/// </summary>
/// <typeparam name="T"></typeparam>
public class MudExRange<T> : SimpleRange<T> where T : IComparable<T>
{
    public MudExRange(T startAndEnd) : base(startAndEnd)
    { }

    public MudExRange(T start, T end) : base(start, end)
    {}

}