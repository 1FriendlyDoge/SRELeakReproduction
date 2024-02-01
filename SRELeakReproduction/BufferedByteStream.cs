namespace SRELeakReproduction;

class BufferedByteStream : Stream
{
    private AutoResetEvent _writeEvent;
    private readonly List<byte> _buffer;
    private readonly int _size;
    private int _readPos;
    private int _writePos;
    private bool _reset;

    public BufferedByteStream(int bufferSize)
    {
        _size = bufferSize;
        _writeEvent = new AutoResetEvent(false);
        _buffer = new List<byte>(_size);
        for (int i = 0; i < _size; i++)
        {
            _buffer.Add(new byte());
        }
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => -1L;

    public override long Position
    {
        get => 0L;
        set {  }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return 0;
    }

    public override void SetLength(long value)
    {
        
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int i = 0;
        while (i<count)
        {
            if (!_reset && _readPos >= _writePos)
            {
                _writeEvent.WaitOne(100, true);
                continue;
            }
            buffer[i] = _buffer[_readPos+offset];
            _readPos++;
            if (_readPos == _size)
            {
                _readPos = 0;
                _reset = false;
            }
            i++;
        }

        return count;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        for (int i = offset; i < offset+count; i++)
        {
            _buffer[_writePos] = buffer[i];
            _writePos++;
            if (_writePos != _size) continue;
            _writePos = 0;
            _reset = true;
        }
        _writeEvent.Set();
    }

    private bool _disposed;
    
    public override void Close()
    {
        if (_disposed) return;
        _disposed = true;
        _writeEvent.Close();
        _writeEvent = null!;
        base.Close();
    }

    public override void Flush()
    {
        
    }
}