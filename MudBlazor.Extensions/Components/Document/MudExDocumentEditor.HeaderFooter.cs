using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Document;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

public partial class MudExDocumentEditor
{
    private bool _marks;

    private Task OnHeaderFooterRequestedAsync(string kind) => OpenHeaderFooterAsync(kind == "footer" ? HeaderFooterKind.Footer : HeaderFooterKind.Header);

    internal async Task OpenHeaderFooterAsync(HeaderFooterKind kind)
    {
        if (IsReadOnly)
            return;
        (DocumentRange Range, string Text, ParagraphAlignment Alignment, bool Exists) info = default;
        await RunAsync(range => info = Live(range) && _document.Sections.Count > 0 ? HeaderFooterInfo(range, kind) : default);
        if (info.Range.Start == null)
            return;
        var title = TryLocalize(kind == HeaderFooterKind.Header ? "Edit Header" : "Edit Footer");
        var reference = await DialogService.ShowExAsync<MudExDocumentHeaderFooterDialog>(title, new DialogParameters
        {
            { nameof(MudExDocumentHeaderFooterDialog.Kind), kind },
            { nameof(MudExDocumentHeaderFooterDialog.Text), info.Text },
            { nameof(MudExDocumentHeaderFooterDialog.Alignment), info.Alignment },
            { nameof(MudExDocumentHeaderFooterDialog.Exists), info.Exists },
            { nameof(MudExDocumentHeaderFooterDialog.Localizer), Localizer }
        }, DialogOptionsEx.EditorDialogOptions(DialogOptions));
        var result = await reference.Result;
        if (await ReselectAsync(info.Range) && result is { Canceled: false, Data: ValueTuple<string, ParagraphAlignment, bool> data })
            await ApplyHeaderFooterAsync(kind, data.Item1, data.Item2, data.Item3);
        await FocusSurfaceAsync();
    }

    private (DocumentRange, string, ParagraphAlignment, bool) HeaderFooterInfo(DocumentRange range, HeaderFooterKind kind)
    {
        var section = _document.Sections[0];
        var part = kind == HeaderFooterKind.Header ? _document.HeaderFor(section, HeaderFooterType.Default) : _document.FooterFor(section, HeaderFooterType.Default);
        var paragraphs = part?.Blocks.OfType<Paragraph>().ToList() ?? [];
        var text = string.Join('\n', paragraphs.Select(p => string.Concat(p.Inlines.Select(i => i switch
        {
            OpaqueInline token when DocumentSurfaceRender.FieldOf(token) is { } field => "{" + field + "}",
            Marker => "",
            _ => Paragraph.TextOf(i)
        }))));
        var alignment = paragraphs.Count > 0 ? _document.EffectiveParagraphFormat(paragraphs[0]).Alignment ?? ParagraphAlignment.Left : ParagraphAlignment.Left;
        return (range, text, alignment is ParagraphAlignment.Center or ParagraphAlignment.Right ? alignment : ParagraphAlignment.Left, part != null);
    }

    private Task ApplyHeaderFooterAsync(HeaderFooterKind kind, string text, ParagraphAlignment alignment, bool remove) => RunAsync(range =>
    {
        if (!Live(range) || IsReadOnly || _document.Sections.Count == 0)
            return;
        var section = _document.Sections[0];
        if (remove)
            _document.Execute(new RemoveHeaderFooterCommand(section.Id, kind, HeaderFooterType.Default), range);
        else
            _document.Execute(new SetHeaderFooterTextCommand(section.Id, kind, HeaderFooterType.Default, (text ?? "").Replace("\r", "").Split('\n'), alignment), range);
    });
}
