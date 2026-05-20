using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Values for the CSS <c>word-break</c> property.
/// </summary>
public enum WordBreak
{
    /// <summary>Default behavior.</summary>
    [Description("normal")] Normal,
    /// <summary>Word breaks may occur between any two characters.</summary>
    [Description("break-all")] BreakAll,
    /// <summary>Word breaks should not occur for Chinese/Japanese/Korean text.</summary>
    [Description("keep-all")] KeepAll,
    /// <summary>Same as overflow-wrap: anywhere; preserves intra-word soft wrap opportunities.</summary>
    [Description("break-word")] BreakWord
}
