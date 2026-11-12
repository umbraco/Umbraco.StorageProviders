using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Umbraco.StorageProviders.AzureBlob;

/// <summary>
/// A read-only, seekable stream over a blob whose length and ETag are already known.
/// </summary>
/// <remarks>
/// Creating the stream makes no request. The first read starts a single download from the current
/// position, conditioned on the ETag so the body can't come from a different version of the blob than
/// the metadata (and Content-Length) it was served with. Seeking closes the download, and the next read
/// starts a new one from the new position, so the static file middleware's "seek, then copy" range
/// handling costs one request.
/// </remarks>
internal sealed class AzureBlobReadStream : Stream
{
    private readonly BlobClient _blobClient;
    private readonly long _length;
    private readonly BlobRequestConditions? _conditions;
    private Stream? _content;
    private long _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobReadStream" /> class.
    /// </summary>
    /// <param name="blobClient">The blob client.</param>
    /// <param name="length">The blob length, or -1 if unknown.</param>
    /// <param name="etag">The ETag to pin reads to, or <c>null</c> to read whatever version is current.</param>
    public AzureBlobReadStream(BlobClient blobClient, long length, ETag? etag)
    {
        _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
        _length = length;
        _conditions = etag is { } value ? new BlobRequestConditions { IfMatch = value } : null;
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => _length >= 0;

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

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        if (buffer.IsEmpty || IsAtEnd)
        {
            return 0;
        }

        _content ??= _blobClient.DownloadStreaming(CreateDownloadOptions()).Value.Content;

        int read = _content.Read(buffer);
        _position += read;

        return read;
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.IsEmpty || IsAtEnd)
        {
            return 0;
        }

        if (_content is null)
        {
            Response<BlobDownloadStreamingResult> response = await _blobClient
                .DownloadStreamingAsync(CreateDownloadOptions(), cancellationToken)
                .ConfigureAwait(false);

            _content = response.Value.Content;
        }

        int read = await _content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        _position += read;

        return read;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
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

        ArgumentOutOfRangeException.ThrowIfNegative(position, nameof(offset));

        if (position != _position)
        {
            // The open download is positioned at _position; the next read starts a new one.
            _content?.Dispose();
            _content = null;
            _position = position;
        }

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
        if (_content is not null)
        {
            await _content.DisposeAsync().ConfigureAwait(false);
            _content = null;
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _content?.Dispose();
            _content = null;
        }

        base.Dispose(disposing);
    }

    private bool IsAtEnd => _length >= 0 && _position >= _length;

    private BlobDownloadOptions CreateDownloadOptions() => new()
    {
        Range = new HttpRange(_position),
        Conditions = _conditions,
    };
}
