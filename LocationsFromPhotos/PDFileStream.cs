using PortableDeviceApiLib;

namespace LocationsFromPhotos;

internal class PDFileStream : Stream
{
    private readonly IStream _stream;
    private readonly MemoryStream _memoryStream;

    public PDFileStream(IStream stream)
    {
        _stream = stream;
        _memoryStream = new MemoryStream();
        ReadDataToMemoryStream();
    }

    private void ReadDataToMemoryStream()
    {
        const uint bufferSize = 8192;
        var buffer = new byte[bufferSize];

        while (true)
        {
            _stream.RemoteRead(out buffer[0], bufferSize, out uint bytesRead);

            if (bytesRead == 0)
                break;

            _memoryStream.Write(buffer, 0, (int)bytesRead);
        }

        _memoryStream.Seek(0, SeekOrigin.Begin);
    }

    public override bool CanRead => _memoryStream.CanRead;
    public override bool CanSeek => _memoryStream.CanSeek;
    public override bool CanWrite => false;

    public override long Length => _memoryStream.Length;

    public override long Position
    {
        get => _memoryStream.Position;
        set => _memoryStream.Position = value;
    }

    public override void Flush() => _memoryStream.Flush();

    public override int Read(byte[] buffer, int offset, int count) => _memoryStream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => _memoryStream.Seek(offset, origin);

    public override void SetLength(long value) => _memoryStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException("This stream is read-only.");
    }
}