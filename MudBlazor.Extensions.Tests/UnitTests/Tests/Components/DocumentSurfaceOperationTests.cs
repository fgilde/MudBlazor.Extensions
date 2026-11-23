using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentSurfaceOperationTests : DocumentSurfaceTestBase
{
    private sealed class Recorder : IJSObjectReference
    {
        public List<(string Identifier, object?[]? Args)> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add((identifier, args));
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class Runtime(IJSRuntime inner, Task<IJSObjectReference> created) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            identifier == "import" && args?.OfType<string>().Any(a => a.Contains("MudExDocumentSurface", StringComparison.Ordinal)) == true
                ? ValueTask.FromResult((TValue)(object)new Module(created))
                : inner.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }

    private sealed class Module(Task<IJSObjectReference> created) : IJSObjectReference
    {
        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            typeof(TValue) == typeof(IJSObjectReference) ? (TValue)await created : default!;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private SurfaceResult Resumed(int index = 0) => (SurfaceResult)_context.JSInterop.Invocations["resume"][index].Arguments[0]!;

    private static DocumentRange Range(TextDocument document, string id, int start, int end) => new(document.PositionOf(id, start), document.PositionOf(id, end));

    private static void Insert(TextDocument document, string id, int offset, string text) =>
        document.Execute(new ReplaceTextCommand(Range(document, id, offset, offset), text));

    [Fact]
    public async Task SelectionChanged_ThrowingHandler_LosesNoPatches()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var errors = new List<Exception>();
        Action<DocumentRange> fail = _ => throw new InvalidOperationException("handler");
        var cut = Surface(document, p => p.Add(c => c.SelectionChanged, fail).Add(c => c.OnError, e => errors.Add(e)));

        var result = await Sync(cut, new SurfaceSync(1, [new SurfaceTyped(id, "Hello!", 6, null)], Sel(id, 6), false));
        await cut.InvokeAsync(() => Insert(document, id, 0, ">"));

        Assert.Equal(2, Assert.Single(result.Patches).Seq);
        Assert.Equal("handler", Assert.Single(errors).Message);
        Assert.Equal(3, Assert.Single(Patches().Last()).Seq);
    }

    [Fact]
    public async Task SelectionChanged_ThrowingHandlerAfterRunAsync_ResumesWithThePatches()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var errors = new List<Exception>();
        Action<DocumentRange> fail = _ => throw new InvalidOperationException("handler");
        var cut = Surface(document, p => p.Add(c => c.SelectionChanged, fail).Add(c => c.OnError, e => errors.Add(e)));
        _context.JSInterop.Setup<SurfaceSync>("suspend", _ => true).SetResult(Idle(id, 3));

        await cut.InvokeAsync(() => cut.Instance.RunAsync(r => document.Execute(new ReplaceTextCommand(Range(document, id, 5, 5), "!"), r)));
        await cut.InvokeAsync(() => Insert(document, id, 0, ">"));

        Assert.Equal(2, Assert.Single(Resumed().Patches).Seq);
        Assert.Equal("handler", Assert.Single(errors).Message);
        Assert.Equal(3, Assert.Single(Patches().Last()).Seq);
    }

    [Fact]
    public async Task SelectionChanged_HandlerCallingRunAsync_DoesNotDeadlock()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        MudExDocumentSurface surface = null!;
        var cut = Surface(document, p => p.Add(c => c.SelectionChanged, (Func<DocumentRange, Task>)(_ => surface.RunAsync(_ => { }))));
        surface = cut.Instance;
        _context.JSInterop.Setup<SurfaceSync>("suspend", _ => true).SetResult(Idle(id, 4));

        await Sync(cut, Idle(id, 3));
        cut.WaitForAssertion(() => Assert.Equal(2, _context.JSInterop.Invocations["resume"].Count));
        var run = cut.InvokeAsync(() => cut.Instance.RunAsync(_ => { }));

        Assert.Same(run, await Task.WhenAny(run, Task.Delay(5000)));
        cut.WaitForAssertion(() => Assert.Equal(3, _context.JSInterop.Invocations["resume"].Count));
    }

    [Fact]
    public async Task RunAsync_Overlapping_SuspendAndResumeAlternate()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);
        var order = new List<string>();
        var suspend = _context.JSInterop.Setup<SurfaceSync>("suspend", _ => Logged(order, "suspend"));
        _context.JSInterop.SetupVoid("resume", _ => Logged(order, "resume")).SetVoidResult();
        Task first = null!, second = null!;

        await cut.InvokeAsync(() =>
        {
            first = cut.Instance.RunAsync(_ => order.Add("first"));
            second = cut.Instance.RunAsync(_ => order.Add("second"));
        });
        Assert.Equal(["suspend"], order);
        await cut.InvokeAsync(() => suspend.SetResult(Idle(id, 0)));
        await Task.WhenAll(first, second);

        Assert.Equal(["suspend", "first", "resume", "suspend", "second", "resume"], order);
    }

    private static bool Logged(List<string> order, string name)
    {
        order.Add(name);
        return true;
    }

    [Fact]
    public async Task RunAsync_FromInsideAnAction_Throws()
    {
        var document = Doc("Hello");
        var cut = Surface(document);
        var errors = new List<Exception?>();

        await cut.InvokeAsync(() => cut.Instance.RunAsync(r =>
        {
            errors.Add(Record.Exception(() => { _ = cut.Instance.RunAsync(_ => { }); }));
            errors.Add(Record.Exception(() => { _ = cut.Instance.FlushAsync(); }));
            errors.Add(Record.Exception(() => { _ = cut.Instance.SelectAsync(r); }));
        }));

        Assert.All(errors, e => Assert.IsType<InvalidOperationException>(e));
        Assert.Single(_context.JSInterop.Invocations["resume"]);
    }

    [Fact]
    public async Task RunAsync_DocumentReplacedWhileSuspending_DoesNotRunTheAction()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);
        var other = Doc("Other");
        _context.JSInterop.Setup<SurfaceSync>("suspend", invocation =>
        {
            if (!ReferenceEquals(cut.Instance.Document, other))
                _ = cut.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(MudExDocumentSurface.Document)] = other }));
            return true;
        }).SetResult(new SurfaceSync(1, [new SurfaceTyped(id, "Hallo", 2, null)], Sel(id, 2), false));
        var ran = false;
        Task run = null!;

        await cut.InvokeAsync(() => run = cut.Instance.RunAsync(_ => ran = true));
        await run;

        Assert.Same(other, cut.Instance.Document);
        Assert.False(ran);
        Assert.Equal("Hello", Text(document));
        Assert.Empty(Resumed().Patches);
    }

    [Fact]
    public async Task Sync_AcceptedTypingWithoutChange_IsEchoed()
    {
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("a"), new TextRun("b") { Format = new RunFormat { Bold = true } }] }]);
        var id = document.Blocks[0].Id;
        var cut = Surface(document);

        var result = await Sync(cut, new SurfaceSync(1, [new SurfaceTyped(id, "ab", 2, null)], Sel(id, 2), false));

        var upsert = Assert.Single(Assert.Single(result.Patches).Upsert);
        Assert.Equal((id, true), (upsert.Block.Id, upsert.InPlace));
        Assert.Contains("font-weight:bold", upsert.Block.Runs[1].Css);
        Assert.False(document.Undo.CanUndo);
    }

    [Fact]
    public async Task Sync_TypingInATableCell_UpsertsThatParagraphOnly()
    {
        var table = new Table { Grid = [1000], Rows = [new TableRow([new TableCell([new Paragraph { Inlines = [new TextRun("cell")] }])])] };
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("before")] }, table, new Paragraph()]);
        var cell = ((Table)document.Blocks[1]).Rows[0].Cells[0].Blocks[0].Id;
        var cut = Surface(document);

        var result = await Sync(cut, new SurfaceSync(1, [new SurfaceTyped(cell, "cells", 5, null)], Sel(cell, 5), false));

        Assert.Equal("cells", ((Paragraph)document.FindBlock(cell)).Text);
        var upsert = Assert.Single(Assert.Single(result.Patches).Upsert);
        Assert.Equal((cell, "cells", true), (upsert.Block.Id, upsert.Block.Text, upsert.InPlace));
        Assert.Equal(document.PositionOf(cell, 5), cut.Instance.Selection.End);
    }

    [Fact]
    public async Task Sync_TooManyEntriesInTheStream_RejectedWithBoundedMaterialisation()
    {
        var document = Doc("Hello");
        var cut = Surface(document);
        var json = "{\"seen\":1,\"typed\":[" + string.Join(",", Enumerable.Repeat("{}", 20_000)) + "],\"selection\":null,\"endTyping\":false}";
        var before = SurfaceTypedListConverter.Materialized;

        SurfaceResult result = null!;
        await cut.InvokeAsync(async () => result = await cut.Instance.OnSyncAsync(null!, new TestStreamReference(json)));
        var materialised = SurfaceTypedListConverter.Materialized - before;

        Assert.Empty(result.Patches);
        Assert.InRange(materialised, 1, 10_001);
        Assert.Equal(10_001, JsonSerializer.Deserialize<SurfaceSync>(json, JsonSerializerOptions.Web)!.Typed.Count);
    }

    [Fact]
    public async Task Command_SelectionInAParagraphChangedSinceSeen_IsNotExecuted()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);
        await cut.InvokeAsync(() => Insert(document, id, 0, "X"));

        var result = await Command(cut, new SurfaceSync(1, [], Sel(id, 3), false), "split");

        Assert.Equal(["XHello"], document.Blocks.OfType<Paragraph>().Select(p => p.Text));
        Assert.Equal("XHello", Upsert(result, id).Block.Text);
        Assert.Null(result.Selection);
    }

    [Fact]
    public async Task SelectAsync_GoesThroughSuspendAndResume()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);
        _context.JSInterop.Setup<SurfaceSync>("suspend", _ => true).SetResult(Idle(id, 1));

        await cut.InvokeAsync(() => cut.Instance.SelectAsync(Range(document, id, 2, 4), scroll: false));

        var resume = Assert.Single(_context.JSInterop.Invocations["resume"]);
        Assert.Equal(new SurfaceSelection(new SurfacePoint(id, 2), new SurfacePoint(id, 4)), ((SurfaceResult)resume.Arguments[0]!).Selection);
        Assert.Equal(false, resume.Arguments[1]);
        Assert.Equal(Range(document, id, 2, 4), cut.Instance.Selection);
        Assert.DoesNotContain(_context.JSInterop.Invocations, i => i.Identifier == "select");
    }

    [Fact]
    public async Task Parameters_ChangedWhileStarting_AreSentAfterStart()
    {
        var created = new TaskCompletionSource<IJSObjectReference>();
        _context.Services.AddSingleton<IJSRuntime>(new Runtime(_context.JSInterop.JSRuntime, created.Task));
        var cut = _context.Render<MudExDocumentSurface>(p => p.Add(c => c.Document, Doc("Hello")));
        cut.Render(p => p.Add(c => c.ReadOnly, true).Add(c => c.Zoom, 2.0).Add(c => c.Shortcuts, ["Ctrl+B"]));
        var js = new Recorder();

        await cut.InvokeAsync(() => created.SetResult(js));

        cut.WaitForAssertion(() => Assert.Contains(js.Calls, c => c.Identifier == "patch"));
        Assert.Equal(true, js.Calls.Single(c => c.Identifier == "setReadOnly").Args![0]);
        Assert.Equal(2.0, js.Calls.Single(c => c.Identifier == "setZoom").Args![0]);
        Assert.Equal(["Ctrl+B"], (IEnumerable<string>)js.Calls.Single(c => c.Identifier == "setShortcuts").Args![0]!);
    }

    [Fact]
    public async Task Document_SetToNull_ResetsTheSurface()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);

        cut.Render(p => p.Add(c => c.Document, (TextDocument?)null));
        var sync = await Sync(cut, new SurfaceSync(int.MaxValue, [new SurfaceTyped(id, "x", 1, null)], Sel(id, 1), false));
        var command = await Command(cut, Idle(id, 0), "split");

        var reset = Assert.Single(Patches().Last());
        Assert.Equal((2, true), (reset.Seq, reset.Reset));
        Assert.Empty(reset.Upsert);
        Assert.Empty(sync.Patches);
        Assert.Empty(command.Patches);
        Assert.Equal("Hello", Text(document));
    }
}
