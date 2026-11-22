namespace MudBlazor.Extensions.Office.Document;

internal sealed record DocumentLoadLimits(int Blocks, long Inlines, int TextCharacters, int RtfCharacters, long PackageBytes, long DocxElements)
{
    private const int Mi = 1024 * 1024;
    private static readonly AsyncLocal<DocumentLoadLimits> Injected = new();
    private static readonly AsyncLocal<Budget> Active = new();

    internal static DocumentLoadLimits Desktop { get; } = new(1_000_000, 8_000_000, 32 * Mi, 32 * Mi, 1L << 30, 16 * Mi);

    internal static DocumentLoadLimits Browser { get; } = new(250_000, 2_000_000, 8 * Mi, 4 * Mi, 256L * Mi, 4 * Mi);

    internal static DocumentLoadLimits Current => Injected.Value ?? (OperatingSystem.IsBrowser() ? Browser : Desktop);

    internal static IDisposable Use(DocumentLoadLimits limits)
    {
        var previous = Injected.Value;
        Injected.Value = limits;
        return new Scope(() => Injected.Value = previous);
    }

    internal static IDisposable BeginLoad()
    {
        var previous = Active.Value;
        Active.Value = new Budget(Current);
        return new Scope(() => Active.Value = previous);
    }

    internal static IDisposable Pause()
    {
        var previous = Active.Value;
        Active.Value = null;
        return new Scope(() => Active.Value = previous);
    }

    internal static void Charge(Block block) => Active.Value?.Charge(block);

    internal static void CheckBlocks(long count)
    {
        if (Active.Value is { } budget && count > budget.Limits.Blocks) throw TooManyBlocks(budget.Limits);
    }

    private static InvalidDataException TooManyBlocks(DocumentLoadLimits limits) => new($"The document has more than {limits.Blocks} blocks.");

    private sealed class Budget(DocumentLoadLimits limits)
    {
        private long _blocks;
        private long _inlines;

        public DocumentLoadLimits Limits => limits;

        public void Charge(Block block)
        {
            if (++_blocks > limits.Blocks) throw TooManyBlocks(limits);
            if (block is Paragraph paragraph && (_inlines += paragraph.Inlines.Count) > limits.Inlines)
                throw new InvalidDataException($"The document has more than {limits.Inlines} inlines.");
        }
    }

    private sealed class Scope(Action end) : IDisposable
    {
        public void Dispose() => end();
    }
}
