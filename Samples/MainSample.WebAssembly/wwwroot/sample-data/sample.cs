using MudBlazor.Extensions;

// A class the code viewer can colour.
public sealed class InvoiceService(IFileStore store)
{
    public async Task<Invoice> LoadAsync(string path, CancellationToken ct = default)
    {
        await using var stream = await store.OpenAsync(path, ct);
        return Invoice.Parse(stream) ?? throw new InvalidOperationException($"no invoice in {path}");
    }

    public IEnumerable<Invoice> Overdue(IEnumerable<Invoice> all, DateOnly today)
        => all.Where(i => i.DueDate < today && i.Open > 0).OrderBy(i => i.DueDate);
}
