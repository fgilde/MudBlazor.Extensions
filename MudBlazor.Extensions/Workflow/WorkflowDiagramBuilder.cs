using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class WorkflowDiagramBuilder
{
    private readonly List<WorkflowPage> _pages = new();

    public WorkflowDiagramBuilder AddPage(Action<WorkflowPageBuilder> configure)
    {
        var page = new WorkflowPage();
        var pageBuilder = new WorkflowPageBuilder(page);
        configure(pageBuilder);
        _pages.Add(page);
        return this;
    }

    public string BuildSvg()
    {
        var svgBuilder = new StringBuilder();
        svgBuilder.AppendLine("<svg xmlns='http://www.w3.org/2000/svg' width='1200' height='1000'>");
        int tabYOffset = 10;

        foreach (var page in _pages)
        {
            svgBuilder.AppendLine($"<text x='10' y='{tabYOffset}' font-size='20' font-weight='bold'>{page.Title}</text>");
            tabYOffset += 40;

            int yOffset = tabYOffset;

            foreach (var group in page.Groups)
            {
                svgBuilder.AppendLine($"<rect x='20' y='{yOffset}' width='500' height='40' fill='{group.BgColor}' rx='5' ry='5'/>");
                svgBuilder.AppendLine($"<text x='30' y='{yOffset + 25}' fill='{group.FgColor}'>{group.Name}</text>");
                yOffset += 60;

                foreach (var command in group.Commands)
                {
                    svgBuilder.AppendLine($"<rect x='50' y='{yOffset}' width='400' height='30' fill='{command.BgColor}' rx='5' ry='5'/>");
                    svgBuilder.AppendLine($"<text x='60' y='{yOffset + 20}' fill='{command.FgColor}'>{command.Name}</text>");
                    yOffset += 50;
                }

                // Verbindungen zwischen Gruppen
                if (group.ConnectionTo != null)
                {
                    var targetGroup = page.Groups.FirstOrDefault(g => g.Id == group.ConnectionTo);
                    if (targetGroup != null)
                    {
                        svgBuilder.AppendLine(DrawArrow(260, yOffset - 30, 260, yOffset + 50));
                    }
                }
            }
        }

        svgBuilder.AppendLine("</svg>");
        return svgBuilder.ToString();
    }

    private string DrawArrow(int x1, int y1, int x2, int y2)
    {
        return $"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='black' marker-end='url(#arrowhead)' />";
    }
}
