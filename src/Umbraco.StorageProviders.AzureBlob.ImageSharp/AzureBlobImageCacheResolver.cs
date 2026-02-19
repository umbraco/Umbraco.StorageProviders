using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Resolvers;

namespace Umbraco.StorageProviders.AzureBlob.ImageSharp;

internal sealed class AzureBlobImageCacheResolver : IImageCacheResolver
{
    private readonly BlobClient _blob;
    private readonly ImageCacheMetadata _metadata;
    private readonly Func<Task<IDisposable?>>? _acquireLease;

    public AzureBlobImageCacheResolver(BlobClient blob, ImageCacheMetadata metadata, Func<Task<IDisposable?>>? acquireLease = null)
    {
        _blob = blob ?? throw new ArgumentNullException(nameof(blob));
        _metadata = metadata;
        _acquireLease = acquireLease;
    }

    public Task<ImageCacheMetadata> GetMetaDataAsync() => Task.FromResult(_metadata);

    public async Task<Stream> OpenReadAsync()
    {
        IDisposable? lease = null;
        if (_acquireLease != null)
        {
            lease = await _acquireLease().ConfigureAwait(false);
        }

        Response<BlobDownloadStreamingResult> response = await _blob.DownloadStreamingAsync().ConfigureAwait(false);
        #pragma warning disable CA2000
        return lease is null
            ? response.Value.Content
            : new LeaseReleasingStream(response.Value.Content, lease);
        #pragma warning restore CA2000
    }

    private sealed class LeaseReleasingStream : Stream
    {
        private readonly Stream _inner;
        private readonly IDisposable _lease;
        private bool _disposed;

        public LeaseReleasingStream(Stream inner, IDisposable lease)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _lease = lease ?? throw new ArgumentNullException(nameof(lease));
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer, cancellationToken);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.WriteAsync(buffer, offset, count, cancellationToken);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.WriteAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _inner.Dispose();
                _lease.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await _inner.DisposeAsync().ConfigureAwait(false);
            _lease.Dispose();
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}
