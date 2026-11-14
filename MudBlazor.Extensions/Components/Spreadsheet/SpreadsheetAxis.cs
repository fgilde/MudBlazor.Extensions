namespace MudBlazor.Extensions.Components;

/// <summary>
/// Offsets of rows or columns from sparse size deviations: prefix sums over the deviating indexes give
/// <see cref="Offset"/> in O(log k) and <see cref="IndexAt"/> in O(log n · log k).
/// </summary>
internal sealed class SpreadsheetAxis
{
    private readonly double _default;
    private readonly int[] _indexes;
    private readonly double[] _sizes;
    private readonly double[] _deltaSums;

    public SpreadsheetAxis(double defaultSize, IReadOnlyDictionary<int, double> sizes, IReadOnlySet<int> hidden, int count)
    {
        _default = defaultSize;
        Count = count;
        var all = new SortedDictionary<int, double>();
        foreach (var (i, s) in sizes)
            if (i < count) all[i] = s;
        foreach (var i in hidden)
            if (i < count) all[i] = 0;
        var deviations = all.Where(p => p.Value != defaultSize).ToList();
        _indexes = deviations.Select(p => p.Key).ToArray();
        _sizes = deviations.Select(p => p.Value).ToArray();
        _deltaSums = new double[_indexes.Length];
        var sum = 0d;
        for (var k = 0; k < _indexes.Length; k++) _deltaSums[k] = sum += _sizes[k] - defaultSize;
    }

    public int Count { get; }

    public double Total => Offset(Count);

    public double Offset(int index)
    {
        var k = LowerBound(index);
        return index * _default + (k > 0 ? _deltaSums[k - 1] : 0);
    }

    public double Size(int index)
    {
        var k = Array.BinarySearch(_indexes, index);
        return k >= 0 ? _sizes[k] : _default;
    }

    public int IndexAt(double offset)
    {
        int lo = 0, hi = Count - 1;
        while (lo < hi)
        {
            var mid = lo + (hi - lo + 1) / 2;
            if (Offset(mid) <= offset) lo = mid;
            else hi = mid - 1;
        }

        return lo;
    }

    private int LowerBound(int index)
    {
        int lo = 0, hi = _indexes.Length;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (_indexes[mid] < index) lo = mid + 1;
            else hi = mid;
        }

        return lo;
    }
}
