using Microsoft.JSInterop;
using System.Text;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

internal sealed class TestStreamReference(byte[] bytes) : IJSStreamReference
{
    public TestStreamReference(string text) : this(Encoding.UTF8.GetBytes(text)) { }
    public long Length => bytes.Length;
    public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<Stream>(new MemoryStream(bytes));
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
