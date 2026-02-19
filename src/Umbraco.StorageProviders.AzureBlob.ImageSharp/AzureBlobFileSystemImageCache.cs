using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Resolvers;
using Umbraco.Extensions;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.StorageProviders.AzureBlob.ImageSharp;

/// <summary>
/// Implements an Azure Blob Storage based cache storing files in a <c>cache</c> subfolder.
/// </summary>
public sealed class AzureBlobFileSystemImageCache : IImageCache, IDisposable
{
    private const string CacheKeyPrefix = "Umbraco.StorageProviders.AzureBlob.ImageSharp:CacheMetadata:";
    private readonly object _concurrencyLock = new();
    private BlobContainerClient _container;
    private readonly IOptionsMonitor<AzureBlobImageSharpCacheOptions> _cacheOptions;
    private readonly IMemoryCache? _memoryCache;
    private string? _containerRootPath;
    private SemaphoreSlim? _concurrencySemaphore;
    private int? _concurrencyLimit;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobFileSystemImageCache" /> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="cacheOptions">The ImageSharp cache options.</param>
    /// <param name="memoryCache">The memory cache for blob metadata.</param>
    /// <param name="name">The name.</param>
    /// <param name="containerRootPath">The container root path (will use <see cref="AzureBlobFileSystemOptions.ContainerRootPath" /> if <c>null</c>).</param>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    public AzureBlobFileSystemImageCache(IOptionsMonitor<AzureBlobFileSystemOptions> options, IOptionsMonitor<AzureBlobImageSharpCacheOptions> cacheOptions, IMemoryCache? memoryCache, string name, string? containerRootPath)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cacheOptions);
        ArgumentNullException.ThrowIfNull(name);

        AzureBlobFileSystemOptions fileSystemOptions = options.Get(name);
        _container = fileSystemOptions.CreateBlobContainerClient();
        _cacheOptions = cacheOptions;
        _memoryCache = memoryCache;
        _containerRootPath = GetContainerRootPath(containerRootPath, fileSystemOptions);

        options.OnChange((options, changedName) =>
        {
            if (changedName == name)
            {
                _container = options.CreateBlobContainerClient();
                _containerRootPath = GetContainerRootPath(containerRootPath, options);
            }
        });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobFileSystemImageCache" /> class.
    /// </summary>
    /// <param name="blobContainerClient">The blob container client.</param>
    /// <param name="containerRootPath">The container root path.</param>
    /// <param name="cacheOptions">The ImageSharp cache options.</param>
    /// <param name="memoryCache">The memory cache for blob metadata.</param>
    /// <exception cref="ArgumentNullException"><paramref name="blobContainerClient" /> is <c>null</c>.</exception>
    public AzureBlobFileSystemImageCache(BlobContainerClient blobContainerClient, string? containerRootPath, IOptionsMonitor<AzureBlobImageSharpCacheOptions> cacheOptions, IMemoryCache? memoryCache = null)
    {
        _container = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));
        _cacheOptions = cacheOptions ?? throw new ArgumentNullException(nameof(cacheOptions));
        _memoryCache = memoryCache;
        _containerRootPath = GetContainerRootPath(containerRootPath);
    }

    /// <inheritdoc />
    public async Task<IImageCacheResolver?> GetAsync(string key)
    {
        string blobName = (_containerRootPath ?? string.Empty) + key;
        BlobClient blob = _container.GetBlobClient(blobName);

        string cacheKey = CacheKeyPrefix + key;
        if (TryGetCachedMetadata(cacheKey, out ImageCacheMetadata cachedMetadata))
        {
            return new AzureBlobImageCacheResolver(blob, cachedMetadata, AcquireConcurrencyLeaseAsync);
        }

        BlobProperties properties;
        using var concurrencyLease = await AcquireConcurrencyLeaseAsync().ConfigureAwait(false);
        try
        {
            properties = (await blob.GetPropertiesAsync().ConfigureAwait(false)).Value;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return null;
        }

        ImageCacheMetadata metadata = GetImageCacheMetadata(properties);
        TrySetCachedMetadata(cacheKey, metadata);

        return new AzureBlobImageCacheResolver(blob, metadata, AcquireConcurrencyLeaseAsync);
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, Stream stream, ImageCacheMetadata metadata)
    {
        using var concurrencyLease = await AcquireConcurrencyLeaseAsync().ConfigureAwait(false);

        string blobName = (_containerRootPath ?? string.Empty) + key;
        BlobClient blob = _container.GetBlobClient(blobName);

        await blob.UploadAsync(stream, new BlobUploadOptions()
        {
            Metadata = metadata.ToDictionary()
        }).ConfigureAwait(false);

        TrySetCachedMetadata(CacheKeyPrefix + key, metadata);
    }

    private static string? GetContainerRootPath(string? containerRootPath, AzureBlobFileSystemOptions? options = null)
    {
        var path = containerRootPath ?? options?.ContainerRootPath;

        return string.IsNullOrEmpty(path) ? null : path.EnsureEndsWith('/');
    }

    private bool TryGetCachedMetadata(string cacheKey, out ImageCacheMetadata metadata)
    {
        metadata = default;

        AzureBlobImageSharpCacheOptions options = _cacheOptions.CurrentValue;
        if (!options.EnableMetadataCache || _memoryCache is null)
        {
            return false;
        }

        return _memoryCache.TryGetValue(cacheKey, out metadata);
    }

    private void TrySetCachedMetadata(string cacheKey, ImageCacheMetadata metadata)
    {
        AzureBlobImageSharpCacheOptions options = _cacheOptions.CurrentValue;
        if (!options.EnableMetadataCache || _memoryCache is null)
        {
            return;
        }

        if (options.MetadataCacheDuration <= TimeSpan.Zero)
        {
            return;
        }

        _memoryCache.Set(cacheKey, metadata, options.MetadataCacheDuration);
    }

    private async Task<IDisposable?> AcquireConcurrencyLeaseAsync()
    {
        int limit = _cacheOptions.CurrentValue.MaxConcurrentImageRequests;
        if (limit <= 0)
        {
            return null;
        }

        SemaphoreSlim semaphore = GetConcurrencySemaphore(limit);
        await semaphore.WaitAsync().ConfigureAwait(false);
        return new SemaphoreRelease(semaphore);
    }

    private SemaphoreSlim GetConcurrencySemaphore(int limit)
    {
        lock (_concurrencyLock)
        {
            if (_concurrencySemaphore is null || _concurrencyLimit != limit)
            {
                _concurrencySemaphore = new SemaphoreSlim(limit, limit);
                _concurrencyLimit = limit;
            }

            return _concurrencySemaphore;
        }
    }

    private sealed class SemaphoreRelease : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public SemaphoreRelease(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public void Dispose() => _semaphore.Release();
    }

    private static ImageCacheMetadata GetImageCacheMetadata(BlobProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        if (properties.Metadata.Count > 0)
        {
            return ImageCacheMetadata.FromDictionary(properties.Metadata);
        }

        DateTime lastModifiedUtc = properties.LastModified.UtcDateTime;
        string contentType = properties.ContentType ?? "application/octet-stream";

        return new ImageCacheMetadata(lastModifiedUtc, lastModifiedUtc, contentType, TimeSpan.Zero, properties.ContentLength);
    }

    /// <inheritdoc />
    public void Dispose() => _concurrencySemaphore?.Dispose();
}
