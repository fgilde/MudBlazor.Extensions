using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Services;

namespace MainSample.WebAssembly.Examples.FileManager;

/// <summary>
/// The in-memory structure the file manager examples browse. Real content, so the preview panel and the
/// content previews have something to render.
/// </summary>
public static class DemoFileStructure
{
    public static MudExInMemoryFileStructureManager Build()
    {
        var manager = new MudExInMemoryFileStructureManager
        {
            Capabilities = MudExFileManagerCapabilities.All
        };

        var files = new (string Path, string Content)[]
        {
            ("readme.md", "# Demo structure\n\nEverything here lives in memory. Rename it, move it, delete it - a reload brings it back.\n"),
            ("documents/notes.txt", "The tree and the file area share the same node instances, so a folder loaded on one side is already loaded on the other.\n"),
            ("documents/report.md", "## Report\n\n| Panel | Comes from |\n|---|---|\n| Preview | MudExFileDisplay |\n| Details | MudExFileMetaView |\n"),
            ("documents/invoices/2026-01.csv", "date;customer;amount\n2026-01-04;Contoso;1240.00\n2026-01-17;Fabrikam;880.50\n"),
            ("documents/invoices/2026-02.csv", "date;customer;amount\n2026-02-02;Contoso;1310.00\n"),
            ("source/Program.cs", "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddMudServicesWithExtensions();\n\nvar app = builder.Build();\napp.Run();\n"),
            ("source/appsettings.json", "{\n  \"Files\": {\n    \"Root\": \"/srv/files\",\n    \"AllowWrite\": true\n  }\n}\n"),
            ("source/styles/site.css", ".mud-ex-fm {\n    width: 100%;\n}\n"),
            ("data/points.geojson", "{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\",\"geometry\":{\"type\":\"Point\",\"coordinates\":[13.405,52.52]},\"properties\":{\"name\":\"Berlin\"}}]}"),
            ("data/empty.log", "")
        };

        foreach (var (path, content) in files)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            manager.AddFile(path, bytes.Length, DateTimeOffset.Now.AddDays(-Random.Shared.Next(1, 90)));
            manager.SetContent(path, bytes);
        }

        // A folder with no files of its own, so "this folder is empty" has somewhere to show up.
        manager.AddFile("archive/.keep", 0, DateTimeOffset.Now);

        return manager;
    }
}
