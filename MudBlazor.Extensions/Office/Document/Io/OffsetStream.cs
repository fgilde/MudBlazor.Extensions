namespace MudBlazor.Extensions.Office.Document;

internal sealed class OffsetStream(Stream inner, long start) : Stream
{
    private readonly long _length = Math.Max(0, inner.Length - start);
    private long _position;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => _position = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        if (_position >= _length) return 0;
        inner.Position = start + _position;
        var read = inner.Read(buffer[..(int)Math.Min(buffer.Length, _length - _position)]);
        _position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        Position = origin switch { SeekOrigin.Begin => offset, SeekOrigin.Current => _position + offset, _ => _length + offset };

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
