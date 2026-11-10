using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Presentation.PptxReader;

namespace MudBlazor.Extensions.Office.Presentation;

internal static class SlideTiming
{
    private static readonly string[] Builds = ["bldP", "bldDgm", "bldOleChart", "bldGraphic"];
    private static readonly string[] Lists = ["tnLst", "childTnLst", "subTnLst", "bldLst"];
    private static readonly string[] TimeNodes = ["par", "seq", "excl"];

    internal static uint MaxTarget(XElement slide) =>
        slide.Descendants(P + "timing").Descendants().Attributes("spid").Select(a => (uint?)Long(a.Value) ?? 0).DefaultIfEmpty().Max();

    internal static void RemoveDeadTargets(XElement slide, XElement tree)
    {
        var ids = tree.Descendants(P + "cNvPr").Select(c => (string)c.Attribute("id")).ToHashSet();
        foreach (var timing in slide.Descendants(P + "timing").ToList())
        {
            foreach (var target in timing.Descendants(P + "spTgt").Where(t => !ids.Contains((string)t.Attribute("spid"))).ToList())
                if (target.Ancestors().Contains(timing))
                    target.Ancestors().FirstOrDefault(a => a.Parent?.Name.LocalName is "tnLst" or "childTnLst" or "subTnLst")?.Remove();
            timing.Descendants().Where(b => b.Name.Namespace == P && Builds.Contains(b.Name.LocalName) && !ids.Contains((string)b.Attribute("spid"))).Remove();
            Prune(timing);
        }
    }

    private static void Prune(XElement timing)
    {
        while (timing.Descendants().FirstOrDefault(e => e.Name.Namespace == P && Lists.Contains(e.Name.LocalName) && !e.HasElements) is { } empty)
        {
            if (empty.Name.LocalName == "tnLst")
            {
                timing.Remove();
                return;
            }

            if (empty.Name.LocalName == "childTnLst" && empty.Parent?.Parent is { } node && TimeNodes.Contains(node.Name.LocalName)) node.Remove();
            else empty.Remove();
        }
    }
}
