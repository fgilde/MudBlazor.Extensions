using System.Globalization;
using System.Text.Json;

namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>Reads the notebook format. A page whose JSON cannot be read becomes an empty, read-only error page whose file is kept unchanged.</summary>
internal static class NotebookReader
{
    public static Notebook Read(Stream stream, NotebookLoadOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var package = NotebookPackage.Open(stream, options);
        var manifest = ReadManifest(package);
        var major = Major(manifest.FormatVersion) ?? throw new InvalidDataException($"The notebook format version {manifest.FormatVersion} is not valid.");
        var notebook = new Notebook
        {
            Name = manifest.Name ?? "", Package = package, FormatVersion = manifest.FormatVersion, ReadOnlyFormat = major > 1, Extra = manifest.Extra
        };
        var sectionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dto in manifest.Sections ?? [])
        {
            if (!NoteValues.IsId(dto?.Id) || !sectionIds.Add(dto.Id)) throw new InvalidDataException($"The notebook has an invalid or duplicate section id {dto?.Id} (ids are compared ignoring case).");
            var section = new NotebookSection { Id = dto.Id, Name = dto.Name ?? "", Color = NoteValues.IsColor(dto.Color) ? dto.Color : NotebookSection.DefaultColor, Extra = dto.Extra };
            foreach (var id in dto.Pages ?? [])
            {
                if (!NoteValues.IsId(id) || !pageIds.Add(id)) throw new InvalidDataException($"The notebook has an invalid or duplicate page id {id} (ids are compared ignoring case).");
                section.PageList.Add(ReadPage(package, id));
            }

            if (section.PageList.Count == 0) section.PageList.Add(new NotebookPage());
            notebook.SectionList.Add(section);
        }

        if (notebook.SectionList.Count == 0) notebook.SectionList.AddRange(Notebook.Create().SectionList);
        return notebook;
    }

    private static ManifestDto ReadManifest(NotebookPackage package)
    {
        if (package.Entry(NotebookPackage.ManifestEntry) == null) throw new InvalidDataException("The file is not a notebook: notebook.json is missing.");
        try
        {
            return JsonSerializer.Deserialize(package.Read(NotebookPackage.ManifestEntry), NotebookJsonContext.Default.ManifestDto)
                   ?? throw new JsonException("The manifest is empty.");
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            throw new InvalidDataException("The notebook manifest cannot be read: " + e.Message, e);
        }
    }

    private static NotebookPage ReadPage(NotebookPackage package, string id)
    {
        var name = NotebookPackage.PageEntry(id);
        if (package.Entry(name)?.FullName is not { } entry) return new NotebookPage { Id = id, LoadError = package.Problem(name) ?? "The page file is missing." };
        try
        {
            var page = NotebookJson.ToPage(JsonSerializer.Deserialize(package.Read(entry), NotebookJsonContext.Default.PageDto) ?? throw new JsonException("The page is empty."));
            page.Id = id;
            page.SourceEntry = entry;
            return page;
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            return new NotebookPage { Id = id, SourceEntry = entry, LoadError = e.Message };
        }
    }

    private static int? Major(string version)
    {
        var major = version?.Split('.')[0];
        return int.TryParse(major, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= 1 ? value : null;
    }
}
