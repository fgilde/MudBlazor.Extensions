using System.Globalization;
using System.Text;
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
        var sectionIds = new HashSet<string>(StringComparer.Ordinal);
        var pageIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dto in manifest.Sections ?? [])
        {
            if (!NoteValues.IsId(dto?.Id) || !sectionIds.Add(dto.Id)) throw new InvalidDataException($"The notebook has an invalid or duplicate section id {dto?.Id}.");
            var section = new NotebookSection { Id = dto.Id, Name = dto.Name ?? "", Color = NoteValues.IsColor(dto.Color) ? dto.Color : NotebookSection.DefaultColor, Extra = dto.Extra };
            foreach (var id in dto.Pages ?? [])
            {
                if (!NoteValues.IsId(id) || !pageIds.Add(id)) throw new InvalidDataException($"The notebook has an invalid or duplicate page id {id}.");
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
        if (package.Entry(name) == null) return new NotebookPage { Id = id, LoadError = "The page file is missing." };
        var bytes = package.Read(name);
        try
        {
            var page = NotebookJson.ToPage(JsonSerializer.Deserialize(bytes, NotebookJsonContext.Default.PageDto) ?? throw new JsonException("The page is empty."));
            page.Id = id;
            page.SourceEntry = name;
            return page;
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            return new NotebookPage { Id = id, SourceEntry = name, SourceText = Encoding.UTF8.GetString(bytes), LoadError = e.Message };
        }
    }

    private static int? Major(string version)
    {
        var major = version?.Split('.')[0];
        return int.TryParse(major, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= 1 ? value : null;
    }
}
