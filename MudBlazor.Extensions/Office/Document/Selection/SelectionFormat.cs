using System.Reflection;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>The merged formatting of a selection; every property that differs across the selection is null.</summary>
/// <param name="Run">The effective character formatting.</param>
/// <param name="Paragraph">The effective paragraph formatting.</param>
/// <param name="StyleId">The paragraph style id, with the default style resolved to its id; null if the paragraphs use different styles.</param>
/// <param name="List">The kind of list if all paragraphs are items of lists of one kind; otherwise null.</param>
public sealed record SelectionFormat(RunFormat Run, ParagraphFormat Paragraph, string StyleId, ListTemplate? List);

internal sealed class FormatMerge<T> where T : class, new()
{
    private static readonly PropertyInfo[] Properties =
        [.. typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)];

    private readonly bool[] _mixed = new bool[Properties.Length];
    private T _first;
    private int _mixedCount;

    public bool AllMixed => _mixedCount == Properties.Length;

    public void Add(T format)
    {
        if (_first == null)
        {
            _first = format;
            return;
        }

        for (var i = 0; i < Properties.Length; i++)
            if (!_mixed[i] && !Equals(Properties[i].GetValue(_first), Properties[i].GetValue(format)))
            {
                _mixed[i] = true;
                _mixedCount++;
            }
    }

    public T Result()
    {
        var result = new T();
        if (_first == null) return result;
        for (var i = 0; i < Properties.Length; i++)
            if (!_mixed[i])
                Properties[i].SetValue(result, Properties[i].GetValue(_first));
        return result;
    }
}
