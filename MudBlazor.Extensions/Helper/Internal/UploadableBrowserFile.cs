using Microsoft.AspNetCore.Components.Forms;
using Nextended.Core.Contracts;

namespace MudBlazor.Extensions.Helper.Internal;

/// <summary>
/// Presents an <see cref="IUploadableFile"/> as an <see cref="IBrowserFile"/>.
/// </summary>
/// <remarks>
/// <see cref="Components.MudExUploadEdit{T}"/> collects files from everywhere - the disk, a url, Google Drive,
/// a recording - and hands them over as bytes. The file structure writers take an
/// <see cref="IBrowserFile"/>, so this adapter is what lets all of those sources go through one upload path
/// without widening the writer contract.
/// </remarks>
internal sealed class UploadableBrowserFile : IBrowserFile
{
    private readonly IUploadableFile _file;

    public UploadableBrowserFile(IUploadableFile file) => _file = file;

    public string Name => _file.FileName;

    public DateTimeOffset LastModified { get; } = DateTimeOffset.Now;

    public long Size => _file.Data?.Length ?? _file.Size;

    public string ContentType => _file.ContentType;

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        var data = _file.Data ?? Array.Empty<byte>();
        if (data.Length > maxAllowedSize)
            throw new IOException($"'{Name}' is {data.Length} bytes and exceeds the allowed {maxAllowedSize}.");

        return new MemoryStream(data, writable: false);
    }
}
