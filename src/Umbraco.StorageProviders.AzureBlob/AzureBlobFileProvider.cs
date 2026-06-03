using System.ComponentModel;
using System.IO.Hashing;
using System.Net;
using System.Runtime.InteropServices;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Umbraco.Cms.Core;
using Umbraco.Extensions;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.StorageProviders.AzureBlob;

/// <summary>
/// Represents a read-only Azure Blob Storage file provider.
/// </summary>
/// <seealso cref="Microsoft.Extensions.FileProviders.IFileProvider" />
public sealed class AzureBlobFileProvider : IFileProvider
{
    private readonly BlobContainerClient _containerClient;
    private readonly string? _containerRootPath;
    private readonly (HybridCache Cache, HybridCacheEntryOptions HitEntryOptions, HybridCacheEntryOptions MissEntryOptions)? _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobFileProvider" /> class.
    /// </summary>
    /// <param name="containerClient">The container client.</param>
    /// <param name="containerRootPath">The container root path.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="containerClient" /> is <c>null</c>.</exception>
    public AzureBlobFileProvider(BlobContainerClient containerClient, string? containerRootPath = null)
    {
        _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
        _containerRootPath = containerRootPath?.Trim(Constants.CharArrays.ForwardSlash);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobFileProvider" /> class with blob metadata caching backed by a caller-supplied <see cref="HybridCache" />.
    /// </summary>
    /// <param name="containerClient">The container client.</param>
    /// <param name="containerRootPath">The container root path.</param>
    /// <param name="cache">The shared <see cref="HybridCache" /> used to store blob metadata.</param>
    /// <param name="cacheOptions">The cache options supplying the absolute expirations for found and not-found entries.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="containerClient" /> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="cache" /> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="cacheOptions" /> is <c>null</c>.</exception>
    public AzureBlobFileProvider(BlobContainerClient containerClient, string? containerRootPath, HybridCache cache, AzureBlobFileSystemCacheOptions cacheOptions)
        : this(containerClient, containerRootPath)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(cacheOptions);

        if (cacheOptions.Enabled is true)
        {
            _cache = (
                cache,
                new HybridCacheEntryOptions
                {
                    Expiration = cacheOptions.HitDuration,
                    LocalCacheExpiration = cacheOptions.HitDuration,
                    Flags = HybridCacheEntryFlags.DisableDistributedCache,
                },
                new HybridCacheEntryOptions
                {
                    Expiration = cacheOptions.MissDuration,
                    LocalCacheExpiration = cacheOptions.MissDuration,
                    Flags = HybridCacheEntryFlags.DisableDistributedCache,
                });
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobFileProvider" /> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    public AzureBlobFileProvider(AzureBlobFileSystemOptions options)
        : this(GetContainerClient(options), options.ContainerRootPath)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobFileProvider" /> class with blob metadata caching backed by a caller-supplied <see cref="HybridCache" />.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="cache">The shared <see cref="HybridCache" /> used to store blob metadata.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="cache" /> is <c>null</c>.</exception>
    public AzureBlobFileProvider(AzureBlobFileSystemOptions options, HybridCache cache)
        : this(GetContainerClient(options), options.ContainerRootPath, cache, options.Cache)
    { }

    /// <inheritdoc />
    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        var path = GetFullPath(subpath);

        // Get all blobs and iterate to fetch all pages
        var blobs = _containerClient.GetBlobsByHierarchy(BlobTraits.None, BlobStates.None, delimiter: "/", prefix: path).ToList();

        return blobs.Count == 0
            ? NotFoundDirectoryContents.Singleton
            : new AzureBlobDirectoryContents(_containerClient, blobs);
    }

    /// <inheritdoc />
    public IFileInfo GetFileInfo(string subpath)
    {
        var path = GetFullPath(subpath);

        if (_cache is not (var cache, var hitEntryOptions, var missEntryOptions))
        {
            return Fetch(path);
        }

        string cacheKey = CreateCacheKey(_containerClient, path);

        // IFileProvider.GetFileInfo is sync; HybridCache is async. Block once at the contract boundary.
        // Stampede protection inside HybridCache ensures concurrent callers for the same key share a single fetch.
        // Stateful overload + static lambda avoids a per-call closure allocation.
        // CachedFileInfo wrapper is [ImmutableObject(true)] so HybridCache stores by reference rather than
        // serializing — IFileInfo is an interface which System.Text.Json cannot deserialize.
        ValueTask<CachedFileInfo> getTask = cache.GetOrCreateAsync(
            cacheKey,
            (Provider: this, Path: path),
            static (state, ct) => state.Provider.FetchAsync(state.Path, ct),
            hitEntryOptions);

        CachedFileInfo cached = getTask.IsCompletedSuccessfully
            ? getTask.Result
            : getTask.AsTask().GetAwaiter().GetResult();

        // Optimistic HitDuration was applied above; shorten to MissDuration for not-found results
        // so newly-uploaded blobs become visible quickly.
        if (!cached.Value.Exists)
        {
            ValueTask setTask = cache.SetAsync(cacheKey, cached, missEntryOptions);
            if (!setTask.IsCompletedSuccessfully)
            {
                setTask.AsTask().GetAwaiter().GetResult();
            }
        }

        return cached.Value;
    }

    /// <inheritdoc />
    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    /// <summary>
    /// Builds the <see cref="HybridCache" /> key for a blob. This is the single place a cache key is constructed,
    /// so the read path (<see cref="GetFileInfo" />) and the write-invalidation path (<see cref="IO.AzureBlobFileSystem" />) can never diverge.
    /// </summary>
    /// <param name="containerClient">The container client the blob belongs to.</param>
    /// <param name="blobPath">The container-relative blob path.</param>
    /// <returns>
    /// A cache key namespaced to this package and the storage account/container.
    /// </returns>
    internal static string CreateCacheKey(BlobContainerClient containerClient, string blobPath)
    {
        // Hash the (potentially long, partly untrusted) blob path so the key stays well under HybridCache's
        // MaximumKeyLength (1024 by default) no matter how deep the request path is; an over-long key silently
        // bypasses the cache. XxHash128 is fast and non-cryptographic — a collision merely serves another blob's
        // metadata until the entry expires or is invalidated, an acceptable trade for a metadata cache. The
        // account/container are kept readable to keep keys diagnosable, and the literal prefix avoids collisions
        // with other consumers of the shared HybridCache.
        UInt128 hash = XxHash128.HashToUInt128(MemoryMarshal.AsBytes(blobPath.AsSpan()));

        return $"Umbraco.StorageProviders.AzureBlob:{containerClient.AccountName}:{containerClient.Name}:{hash:x32}";
    }

    private string GetFullPath(string subpath) => _containerRootPath + subpath.EnsureStartsWith('/');

    private IFileInfo Fetch(string path)
    {
        BlobClient blobClient = _containerClient.GetBlobClient(path);
        try
        {
            return new AzureBlobItemInfo(blobClient, blobClient.GetProperties().Value);
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return new NotFoundFileInfo(AzureBlobItemInfo.ParseName(path));
        }
    }

    private async ValueTask<CachedFileInfo> FetchAsync(string path, CancellationToken cancellationToken)
    {
        BlobClient blobClient = _containerClient.GetBlobClient(path);
        try
        {
            Response<BlobProperties> response = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            return new CachedFileInfo(new AzureBlobItemInfo(blobClient, response.Value));
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return new CachedFileInfo(new NotFoundFileInfo(AzureBlobItemInfo.ParseName(path)));
        }
    }

    private static BlobContainerClient GetContainerClient(AzureBlobFileSystemOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.CreateBlobContainerClient();
    }

    // Immutable wrapper around IFileInfo. HybridCache's ImmutableTypeCache<T> sees [ImmutableObject(true)]
    // on this type and uses ImmutableCacheItem<T> (stored by reference, no serialization) instead of the
    // default MutableCacheItem<T> which serializes via System.Text.Json — that path fails for IFileInfo
    // because the interface cannot be deserialized polymorphically.
    [ImmutableObject(true)]
    private sealed record CachedFileInfo(IFileInfo Value);
}
