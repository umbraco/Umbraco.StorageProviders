using System.Buffers;
using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Umbraco.StorageProviders.AzureBlob;

/// <summary>
/// A read-only, seekable stream over a blob whose length and ETag are already known.
/// </summary>
/// <remarks>
/// Creating the stream makes no request. Reads download the blob in ranges of up to 4 MiB, the same size
/// <c>BlobClient.OpenRead()</c> uses, starting at the current position and conditioned on the ETag, so the
/// body can't come from a different version of the blob than the metadata (and Content-Length) it was
/// served with. Seeking makes no request either: the next read continues the open download when it's at
/// the same position, and otherwise starts a new range there. The first bytes read are kept, so a caller
/// that reads a header and seeks back to it (like ImageSharp detecting the image format) doesn't download
/// them again.
/// </remarks>
internal sealed class AzureBlobReadStream : Stream
{
    // The range size BlobClient.OpenRead() uses by default. It's also the largest range Blob Storage returns
    // a transactional MD5 for, so downloads keep working when MD5 transfer validation is enabled.
    private const int RangeSize = 4 * 1024 * 1024;

    // How many of the first bytes read are kept for a seek back.
    private const int HeadSize = 4 * 1024;

    private readonly BlobClient _blobClient;
    private readonly long _length;
    private readonly BlobRequestConditions? _conditions;
    private readonly Action? _onStale;
    private Stream? _content;
    private long _contentPosition;
    private long _contentEnd;
    private byte[]? _head;
    private long _headStart;
    private int _headLength;
    private long _position;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobReadStream" /> class.
    /// </summary>
    /// <param name="blobClient">The blob client.</param>
    /// <param name="length">The blob length, or -1 if unknown.</param>
    /// <param name="etag">The ETag to pin reads to, or <c>null</c> to read whatever version is current.</param>
    /// <param name="onStale">Called when a download finds the blob was replaced or deleted since <paramref name="etag" /> and <paramref name="length" /> were read.</param>
    public AzureBlobReadStream(BlobClient blobClient, long length, ETag? etag, Action? onStale = null)
    {
        _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
        _length = length;
        _conditions = etag is { } value ? new BlobRequestConditions { IfMatch = value } : null;
        _onStale = onStale;
    }

    /// <inheritdoc />
    public override bool CanRead => !_disposed;

    /// <inheritdoc />
    public override bool CanSeek => !_disposed && _length >= 0;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => _length >= 0 ? _length : throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    private bool IsAtEnd => _length >= 0 && _position >= _length;

    private bool IsContentAtPosition => _content is not null && _contentPosition == _position && _position < _contentEnd;

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);

        return Read(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (buffer.IsEmpty || IsAtEnd)
        {
            return 0;
        }

        int read = ReadFromHead(buffer);
        if (read > 0)
        {
            return read;
        }

        try
        {
            if (!IsContentAtPosition)
            {
                _content = _blobClient.DownloadStreaming(StartDownload()).Value.Content;
            }

            read = _content!.Read(buffer);
        }
        catch (Exception ex)
        {
            OnReadFailed(ex);
            throw;
        }

        OnContentRead(buffer[..read]);

        return read;
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);

        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (buffer.IsEmpty || IsAtEnd)
        {
            return 0;
        }

        int read = ReadFromHead(buffer.Span);
        if (read > 0)
        {
            return read;
        }

        try
        {
            if (!IsContentAtPosition)
            {
                Response<BlobDownloadStreamingResult> response = await _blobClient
                    .DownloadStreamingAsync(StartDownload(), cancellationToken)
                    .ConfigureAwait(false);

                _content = response.Value.Content;
            }

            read = await _content!.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnReadFailed(ex);
            throw;
        }

        OnContentRead(buffer.Span[..read]);

        return read;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!CanSeek)
        {
            throw new NotSupportedException();
        }

        long position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };

        if (position < 0)
        {
            throw new IOException("An attempt was made to move the position before the beginning of the stream.");
        }

        // No request here: the next read continues the open download if it's still at this position.
        _position = position;

        return _position;
    }

    /// <inheritdoc />
    public override void Flush()
    { }

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        _disposed = true;

        if (_content is not null)
        {
            await _content.DisposeAsync().ConfigureAwait(false);
            _content = null;
        }

        ReturnHead();

        await base.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposed = true;
            _content?.Dispose();
            _content = null;
            ReturnHead();
        }

        base.Dispose(disposing);
    }

    private BlobDownloadOptions StartDownload()
    {
        _content?.Dispose();
        _content = null;

        long? rangeLength = _length >= 0 ? Math.Min(RangeSize, _length - _position) : null;
        _contentPosition = _position;
        _contentEnd = rangeLength is { } value ? _position + value : long.MaxValue;

        return new BlobDownloadOptions
        {
            Range = new HttpRange(_position, rangeLength),
            Conditions = _conditions,
        };
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Evicting cached metadata is best effort; the storage error is rethrown by the caller.")]
    private void OnReadFailed(Exception exception)
    {
        // Drop the download, so a retry starts a new range instead of reading from a stream that already failed.
        _content?.Dispose();
        _content = null;

        if (exception is RequestFailedException { Status: (int)HttpStatusCode.PreconditionFailed or (int)HttpStatusCode.NotFound })
        {
            try
            {
                _onStale?.Invoke();
            }
            catch (Exception)
            {
                // Evicting cached metadata is best effort: keep the storage error as the exception the caller sees.
            }
        }
    }

    private void ReturnHead()
    {
        if (_head is not null)
        {
            ArrayPool<byte>.Shared.Return(_head);
            _head = null;
            _headLength = 0;
        }
    }

    private int ReadFromHead(Span<byte> buffer)
    {
        if (_head is null || _position < _headStart || _position >= _headStart + _headLength)
        {
            return 0;
        }

        int offset = (int)(_position - _headStart);
        int count = Math.Min(buffer.Length, _headLength - offset);
        _head.AsSpan(offset, count).CopyTo(buffer);
        _position += count;

        return count;
    }

    private void OnContentRead(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        if (_head is null)
        {
            _head = ArrayPool<byte>.Shared.Rent(HeadSize);
            _headStart = _position;
        }

        // Keep the bytes while they extend the head; the content is pinned to one ETag, so they stay valid.
        if (_headLength < HeadSize && _position == _headStart + _headLength)
        {
            int count = Math.Min(data.Length, HeadSize - _headLength);
            data[..count].CopyTo(_head.AsSpan(_headLength));
            _headLength += count;
        }

        _position += data.Length;
        _contentPosition += data.Length;
    }
}