using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>How the counter of a list level is shown.</summary>
public enum NumberFormat
{
    /// <summary>1, 2, 3.</summary>
    Decimal,

    /// <summary>01, 02, 03.</summary>
    DecimalZero,

    /// <summary>a, b, c, then aa, bb.</summary>
    LowerLetter,

    /// <summary>A, B, C, then AA, BB.</summary>
    UpperLetter,

    /// <summary>i, ii, iii.</summary>
    LowerRoman,

    /// <summary>I, II, III.</summary>
    UpperRoman,

    /// <summary>A bullet: the level text without a counter.</summary>
    Bullet,

    /// <summary>No label.</summary>
    None,

    /// <summary>Another format of the file; it is kept on write and shown as decimal.</summary>
    Other
}

/// <summary>A level of a list definition (<c>w:lvl</c>).</summary>
/// <param name="Level">The level, 0 to 8.</param>
/// <param name="Start">The first counter value.</param>
/// <param name="Format">How the counter is shown.</param>
/// <param name="Text">The label pattern; <c>%1</c> to <c>%9</c> stand for the counters of levels 0 to 8.</param>
/// <param name="Restart">
/// The one-based level whose occurrence restarts this level, 0 for never; null restarts it after any higher level.
/// </param>
/// <param name="Indent">The paragraph formatting the level applies, mainly indents and tab stops.</param>
/// <param name="LabelFormat">The character formatting of the label.</param>
/// <param name="Suffix">What follows the label as the <c>w:suff</c> value: <c>tab</c>, <c>space</c> or <c>nothing</c>.</param>
public sealed record NumberingLevel(int Level, int Start, NumberFormat Format, string Text, int? Restart, ParagraphFormat Indent, RunFormat LabelFormat, string Suffix)
{
    internal XElement Source { get; init; }
}

/// <summary>A list definition (<c>w:abstractNum</c>).</summary>
/// <param name="Id">The abstract numbering id.</param>
/// <param name="Levels">The levels; the list is treated as immutable after assignment.</param>
public sealed record AbstractNumbering(int Id, IReadOnlyList<NumberingLevel> Levels)
{
    internal bool Multilevel { get; init; }

    internal XElement Source { get; init; }
}

/// <summary>A level override of a <see cref="NumberingInstance"/> (<c>w:lvlOverride</c>).</summary>
/// <param name="Level">The overridden level.</param>
/// <param name="StartOverride">The value the counter restarts at, or null.</param>
/// <param name="Definition">A replacement definition of the level, or null to keep the abstract one.</param>
public sealed record LevelOverride(int Level, int? StartOverride, NumberingLevel Definition);

/// <summary>A list instance paragraphs refer to (<c>w:num</c>).</summary>
/// <param name="Id">The numbering id used in <see cref="NumberingRef.NumId"/>.</param>
/// <param name="AbstractId">The id of the <see cref="AbstractNumbering"/> it uses.</param>
/// <param name="Overrides">The level overrides; the list is treated as immutable after assignment.</param>
public sealed record NumberingInstance(int Id, int AbstractId, IReadOnlyList<LevelOverride> Overrides)
{
    internal XElement Source { get; init; }
}

/// <summary>The list definitions of a document.</summary>
public sealed class Numbering
{
    internal Numbering(IEnumerable<AbstractNumbering> abstracts, IEnumerable<NumberingInstance> instances)
    {
        AbstractList = abstracts.ToList();
        InstanceList = instances.ToList();
    }

    /// <summary>The list definitions.</summary>
    public IReadOnlyList<AbstractNumbering> Abstracts => AbstractList;

    /// <summary>The list instances.</summary>
    public IReadOnlyList<NumberingInstance> Instances => InstanceList;

    internal List<AbstractNumbering> AbstractList { get; }

    internal List<NumberingInstance> InstanceList { get; }

    /// <summary>Gets the definition of a list level with the overrides of its instance applied.</summary>
    /// <param name="reference">The list and level.</param>
    /// <returns>The level, or null if the instance or the level does not exist.</returns>
    public NumberingLevel Level(NumberingRef reference)
    {
        var instance = InstanceList.FirstOrDefault(i => i.Id == reference.NumId);
        if (instance == null || reference.NumId == 0) return null;
        var levelOverride = instance.Overrides.FirstOrDefault(o => o.Level == reference.Level);
        var level = levelOverride?.Definition ?? AbstractList.FirstOrDefault(a => a.Id == instance.AbstractId)?.Levels.FirstOrDefault(l => l.Level == reference.Level);
        return level != null && levelOverride?.StartOverride is { } start ? level with { Start = start } : level;
    }
}
