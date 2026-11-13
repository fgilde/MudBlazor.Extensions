using System.Text.Json;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

internal sealed class JsonStreamReference(object value) : IJSStreamReference
{
    private readonly byte[] _data = JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions.Web);

    public long Length => _data.Length;

    public bool Disposed { get; private set; }

    public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
        _data.Length > maxAllowedSize ? throw new InvalidOperationException("Too large.") : ValueTask.FromResult<Stream>(new MemoryStream(_data));

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
