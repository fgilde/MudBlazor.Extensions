using System.Globalization;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Spreadsheet.XlsxPackage;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class RulesXml
{
    private static readonly string[] TextTypes =
        ["containsText", "notContainsText", "beginsWith", "endsWith", "containsBlanks", "notContainsBlanks", "containsErrors", "notContainsErrors"];

    private static readonly string[] TextOperators = ["containsText", "notContains", "beginsWith", "endsWith"];

    private static readonly string[] TextFormulas =
    [
        "NOT(ISERROR(SEARCH({1},{0})))", "ISERROR(SEARCH({1},{0}))", "LEFT({0},LEN({1}))={1}", "RIGHT({0},LEN({1}))={1}", "LEN(TRIM({0}))=0",
        "LEN(TRIM({0}))>0", "ISERROR({0})", "NOT(ISERROR({0}))"
    ];

    private static readonly Dictionary<string, string> PeriodFormulas = new()
    {
        ["today"] = "FLOOR({0},1)=TODAY()",
        ["yesterday"] = "FLOOR({0},1)=TODAY()-1",
        ["tomorrow"] = "FLOOR({0},1)=TODAY()+1",
        ["last7Days"] = "AND(TODAY()-FLOOR({0},1)<=6,FLOOR({0},1)<=TODAY())",
        ["thisMonth"] = "AND(MONTH({0})=MONTH(TODAY()),YEAR({0})=YEAR(TODAY()))",
        ["lastMonth"] = "AND(MONTH({0})=MONTH(EDATE(TODAY(),0-1)),YEAR({0})=YEAR(EDATE(TODAY(),0-1)))",
        ["nextMonth"] = "AND(MONTH({0})=MONTH(EDATE(TODAY(),0+1)),YEAR({0})=YEAR(EDATE(TODAY(),0+1)))",
        ["thisWeek"] = "AND(TODAY()-ROUNDDOWN({0},0)<=WEEKDAY(TODAY())-1,ROUNDDOWN({0},0)-TODAY()<=7-WEEKDAY(TODAY()))",
        ["lastWeek"] = "AND(TODAY()-ROUNDDOWN({0},0)>=(WEEKDAY(TODAY())),TODAY()-ROUNDDOWN({0},0)<(WEEKDAY(TODAY())+7))",
        ["nextWeek"] = "AND(ROUNDDOWN({0},0)-TODAY()>(7-WEEKDAY(TODAY())),ROUNDDOWN({0},0)-TODAY()<(15-WEEKDAY(TODAY())))"
    };

    private static readonly string[] ValueTypes = ["min", "max", "num", "percent", "percentile", "formula"];

    private static readonly string[] ValidationTypes = ["none", "whole", "decimal", "list", "date", "time", "textLength", "custom"];

    private const string DefaultIconSet = "3TrafficLights1";

    internal static ConditionalFormatting ReadFormatting(XElement e, IReadOnlyList<DifferentialStyle> dxfs, string[] theme)
    {
        var ranges = Ranges((string)e.Attribute("sqref"));
        if (ranges.Count == 0) return null;
        var rules = e.Elements().Where(r => r.Name.LocalName == "cfRule").Select(r => ReadRule(r, dxfs, theme)).OfType<ConditionalRule>().ToList();
        return new ConditionalFormatting(ranges, rules) { Source = e };
    }

    internal static List<DataValidation> ReadValidations(XElement e) =>
        e.Elements().Where(v => v.Name.LocalName == "dataValidation").Select(ReadValidation).OfType<DataValidation>().ToList();

    internal static IEnumerable<XElement> WriteFormats(Worksheet ws, XNamespace ns, StylesXmlWriter styles, string[] theme)
    {
        var known = ws.Workbook.Styles.DifferentialStyles.Count;
        foreach (var cf in ws.ConditionalFormats)
        {
            var e = cf.Source == null ? new XElement(ns + "conditionalFormatting") : new XElement(cf.Source);
            e.SetAttributeValue("sqref", Sqref(cf.Ranges));
            e.Elements(ns + "cfRule").Remove();
            e.AddFirst(cf.Rules.Select(r => WriteRule(r, cf.Anchor, ns, styles, theme, known)));
            yield return e;
        }
    }

    internal static XElement WriteValidations(Worksheet ws, XElement original, XNamespace ns)
    {
        if (ws.DataValidations.Count == 0) return null;
        var e = original == null ? new XElement(ns + "dataValidations") : new XElement(original);
        e.Elements(ns + "dataValidation").Remove();
        e.SetAttributeValue("count", ws.DataValidations.Count);
        e.AddFirst(ws.DataValidations.Select(d => WriteValidation(d, ns)));
        return e;
    }

    private static List<CellRange> Ranges(string sqref) =>
        (sqref ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(s => CellRange.TryParse(s, out var r) ? r.Normalized : (CellRange?)null).OfType<CellRange>().ToList();

    private static string Sqref(IEnumerable<CellRange> ranges) => string.Join(' ', ranges.Select(r => r.ToA1()));

    private static bool True(XElement e, string name, bool fallback = false) =>
        (string)e.Attribute(name) is { } v ? v is "1" or "true" : fallback;

    private static int? Int(XElement e, string name) =>
        int.TryParse((string)e?.Attribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static string Formula(string text) => text == null ? null : SheetXmlWriter.FromFile(text);

    private static ConditionalRule ReadRule(XElement r, IReadOnlyList<DifferentialStyle> dxfs, string[] theme)
    {
        var priority = Int(r, "priority") ?? 1;
        var stop = True(r, "stopIfTrue");
        var dxfId = Int(r, "dxfId");
        var style = dxfId is { } id && id >= 0 && id < dxfs.Count ? dxfs[id] : null;
        var formulas = r.Elements().Where(f => f.Name.LocalName == "formula").Select(f => Formula(f.Value)).ToList();
        var type = (string)r.Attribute("type");
        ConditionalRule rule = type switch
        {
            "cellIs" => new ConditionalRule.CellIs((string)r.Attribute("operator") ?? "equal", formulas.ElementAtOrDefault(0), formulas.ElementAtOrDefault(1), style,
                priority, stop),
            "expression" => new ConditionalRule.Expression(formulas.ElementAtOrDefault(0), style, priority, stop),
            "colorScale" when Child(r, "colorScale") is { } scale => new ConditionalRule.ColorScale(Values(scale)
                .Zip(scale.Elements().Where(c => c.Name.LocalName == "color"), (v, c) => new ColorScaleStop(v, ThemeColors.Resolve(c, theme, ThemeColors.Indexed)))
                .ToList(), priority, stop),
            "dataBar" when Child(r, "dataBar") is { } bar && Values(bar) is [var min, var max] =>
                new ConditionalRule.DataBar(min, max, ThemeColors.Resolve(Child(bar, "color"), theme, ThemeColors.Indexed), priority, stop),
            "iconSet" when Child(r, "iconSet") is { } set => new ConditionalRule.IconSet((string)set.Attribute("iconSet") ?? DefaultIconSet, Values(set),
                True(set, "reverse"), True(set, "showValue", true), priority, stop),
            "top10" => new ConditionalRule.Top10(Int(r, "rank") ?? 10, True(r, "percent"), True(r, "bottom"), style, priority, stop),
            "aboveAverage" => new ConditionalRule.AboveAverage(!True(r, "aboveAverage", true), True(r, "equalAverage"), Int(r, "stdDev") ?? 0, style, priority, stop),
            "duplicateValues" or "uniqueValues" => new ConditionalRule.Duplicate(type == "uniqueValues", style, priority, stop),
            "timePeriod" => new ConditionalRule.TimePeriod((string)r.Attribute("timePeriod") ?? "today", style, priority, stop),
            _ when Array.IndexOf(TextTypes, type) is >= 0 and var kind =>
                new ConditionalRule.ContainsText((ContainsTextKind)kind, (string)r.Attribute("text"), style, priority, stop),
            _ => null
        };
        return rule == null ? null : rule with { Source = r, DxfId = dxfId };
    }

    private static List<ConditionalValue> Values(XElement parent) => parent.Elements().Where(c => c.Name.LocalName == "cfvo").Select(c =>
        new ConditionalValue((string)c.Attribute("type") switch
        {
            "autoMin" => ConditionalValueType.Min,
            "autoMax" => ConditionalValueType.Max,
            var t => Array.IndexOf(ValueTypes, t) is >= 0 and var i ? (ConditionalValueType)i : ConditionalValueType.Number
        }, Formula((string)c.Attribute("val")), True(c, "gte", true))).ToList();

    private static DataValidation ReadValidation(XElement v)
    {
        var ranges = Ranges((string)v.Attribute("sqref"));
        if (ranges.Count == 0) return null;
        var type = Array.IndexOf(ValidationTypes, (string)v.Attribute("type")) is > 0 and var t ? (ValidationType)t : ValidationType.Any;
        return new DataValidation(ranges, type, (string)v.Attribute("operator") ?? "between", Formula(Child(v, "formula1")?.Value), Formula(Child(v, "formula2")?.Value),
            True(v, "allowBlank"), !True(v, "showDropDown"), (string)v.Attribute("promptTitle"), (string)v.Attribute("prompt"),
            (string)v.Attribute("errorTitle"), (string)v.Attribute("error"), (string)v.Attribute("errorStyle") ?? "stop") { Source = v };
    }

    private static XElement FormulaElement(XNamespace ns, string name, string formula) =>
        formula == null ? null : new XElement(ns + name, SheetXmlWriter.FormulaText(RuleFormulas.Bare(formula)));

    private static XElement WriteRule(ConditionalRule rule, CellRef anchor, XNamespace ns, StylesXmlWriter styles, string[] theme, int knownDxfs)
    {
        var x = rule.Source == null ? new XElement(ns + "cfRule") : new XElement(rule.Source);
        x.Elements(ns + "formula").Remove();
        x.SetAttributeValue("type", rule switch
        {
            ConditionalRule.CellIs => "cellIs",
            ConditionalRule.Expression => "expression",
            ConditionalRule.ColorScale => "colorScale",
            ConditionalRule.DataBar => "dataBar",
            ConditionalRule.IconSet => "iconSet",
            ConditionalRule.Top10 => "top10",
            ConditionalRule.AboveAverage => "aboveAverage",
            ConditionalRule.Duplicate d => d.Unique ? "uniqueValues" : "duplicateValues",
            ConditionalRule.TimePeriod => "timePeriod",
            ConditionalRule.ContainsText t => TextTypes[(int)t.Kind],
            _ => null
        });
        x.SetAttributeValue("dxfId", rule.Format != null ? styles.Dxf(rule.Format, rule.DxfId) : rule.DxfId >= knownDxfs ? rule.DxfId : null);
        x.SetAttributeValue("priority", rule.Priority);
        x.SetAttributeValue("stopIfTrue", rule.StopIfTrue ? 1 : null);
        var at = anchor.ToA1();
        List<string> formulas = [];
        switch (rule)
        {
            case ConditionalRule.CellIs c:
                x.SetAttributeValue("operator", c.Operator);
                formulas = [c.Formula1, c.Formula2];
                break;
            case ConditionalRule.Expression e:
                formulas = [e.Formula];
                break;
            case ConditionalRule.ColorScale s:
                Patch(x, ns, "colorScale", (scale, old) => scale.Add(s.Stops.Select(stop => Cfvo(stop.Value, ns)),
                    s.Stops.Select((stop, i) => Color(old.Where(o => o.Name == ns + "color").ElementAtOrDefault(i), stop.Color, ns, theme))));
                break;
            case ConditionalRule.DataBar d:
                Patch(x, ns, "dataBar", (bar, old) => bar.Add(Cfvo(d.Min, ns), Cfvo(d.Max, ns), Color(old.FirstOrDefault(o => o.Name == ns + "color"), d.Color, ns, theme)));
                break;
            case ConditionalRule.IconSet i:
                Patch(x, ns, "iconSet", (set, _) =>
                {
                    set.SetAttributeValue("iconSet", i.Set == DefaultIconSet ? null : i.Set);
                    set.SetAttributeValue("reverse", i.Reverse ? 1 : null);
                    set.SetAttributeValue("showValue", i.ShowValue ? null : 0);
                    set.Add(i.Thresholds.Select(t => Cfvo(t, ns)));
                });
                break;
            case ConditionalRule.Top10 t:
                x.SetAttributeValue("rank", t.Rank);
                x.SetAttributeValue("percent", t.Percent ? 1 : null);
                x.SetAttributeValue("bottom", t.Bottom ? 1 : null);
                break;
            case ConditionalRule.AboveAverage a:
                x.SetAttributeValue("aboveAverage", a.Below ? 0 : null);
                x.SetAttributeValue("equalAverage", a.Equal ? 1 : null);
                x.SetAttributeValue("stdDev", a.StdDev > 0 ? a.StdDev : null);
                break;
            case ConditionalRule.ContainsText t:
                var kind = (int)t.Kind;
                x.SetAttributeValue("operator", kind < TextOperators.Length ? TextOperators[kind] : null);
                x.SetAttributeValue("text", kind < TextOperators.Length ? t.Text ?? "" : null);
                formulas = [string.Format(CultureInfo.InvariantCulture, TextFormulas[kind], at, "\"" + (t.Text ?? "").Replace("\"", "\"\"") + "\"")];
                break;
            case ConditionalRule.TimePeriod p:
                x.SetAttributeValue("timePeriod", p.Period);
                formulas = PeriodFormulas.TryGetValue(p.Period ?? "", out var period) ? [string.Format(CultureInfo.InvariantCulture, period, at)] : [];
                break;
        }

        x.AddFirst(formulas.Select(f => FormulaElement(ns, "formula", f)));
        return x;
    }

    private static void Patch(XElement rule, XNamespace ns, string name, Action<XElement, List<XElement>> fill)
    {
        var source = rule.Element(ns + name);
        var element = source == null ? new XElement(ns + name) : new XElement(source);
        var old = element.Elements().ToList();
        element.RemoveNodes();
        fill(element, old);
        if (source != null) source.ReplaceWith(element);
        else if (rule.Element(ns + "extLst") is { } ext) ext.AddBeforeSelf(element);
        else rule.Add(element);
    }

    private static XElement Cfvo(ConditionalValue v, XNamespace ns) => new(ns + "cfvo", new XAttribute("type", ValueTypes[(int)v.Type]),
        v.Value == null ? null : new XAttribute("val", SheetXmlWriter.FormulaText(RuleFormulas.Bare(v.Value))), v.GreaterOrEqual ? null : new XAttribute("gte", 0));

    private static XElement Color(XElement source, string color, XNamespace ns, string[] theme) =>
        source != null && string.Equals(ThemeColors.Resolve(source, theme, ThemeColors.Indexed), color, StringComparison.OrdinalIgnoreCase)
            ? new XElement(source)
            : new XElement(ns + "color", new XAttribute("rgb", "FF" + (color ?? "#000000").TrimStart('#').ToUpperInvariant()));

    private static XElement WriteValidation(DataValidation d, XNamespace ns)
    {
        var x = d.Source == null
            ? new XElement(ns + "dataValidation", new XAttribute("showInputMessage", 1), new XAttribute("showErrorMessage", 1))
            : new XElement(d.Source);
        x.Elements().Where(e => e.Name.LocalName is "formula1" or "formula2").Remove();
        var comparing = d.Type is not (ValidationType.Any or ValidationType.List or ValidationType.Custom);
        x.SetAttributeValue("type", d.Type == ValidationType.Any ? null : ValidationTypes[(int)d.Type]);
        x.SetAttributeValue("errorStyle", d.ErrorStyle is null or "stop" ? null : d.ErrorStyle);
        x.SetAttributeValue("operator", !comparing || d.Operator is null or "between" ? null : d.Operator);
        x.SetAttributeValue("allowBlank", d.AllowBlank ? 1 : null);
        x.SetAttributeValue("showDropDown", d.ShowDropDown ? null : 1);
        x.SetAttributeValue("errorTitle", d.ErrorTitle);
        x.SetAttributeValue("error", d.Error);
        x.SetAttributeValue("promptTitle", d.PromptTitle);
        x.SetAttributeValue("prompt", d.Prompt);
        x.SetAttributeValue("sqref", Sqref(d.Ranges));
        x.AddFirst(FormulaElement(ns, "formula1", d.Formula1), comparing ? FormulaElement(ns, "formula2", d.Formula2) : null);
        return x;
    }
}
