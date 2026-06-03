using System.Diagnostics.CodeAnalysis;
using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache? _cache;
    private readonly AzureBlobFileSystemCacheOptions? _cacheOptions;

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
    /// Initializes a new instance of the <see cref="AzureBlobFileProvider" /> class with blob metadata caching backed by a caller-supplied <see cref="IMemoryCache" />.
    /// </summary>
    /// <param name="containerClient">The container client.</param>
    /// <param name="containerRootPath">The container root path.</param>
    /// <param name="cache">The cache used to store blob metadata. The lifetime of this cache is owned by the caller.</param>
    /// <param name="cacheOptions">The cache options supplying the absolute expirations for found and not-found entries.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="containerClient" /> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="cache" /> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="cacheOptions" /> is <c>null</c>.</exception>
    public AzureBlobFileProvider(BlobContainerClient containerClient, string? containerRootPath, IMemoryCache cache, AzureBlobFileSystemCacheOptions cacheOptions)
        : this(containerClient, containerRootPath)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _cacheOptions = cacheOptions ?? throw new ArgumentNullException(nameof(cacheOptions));
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
    /// Initializes a new instance of the <see cref="AzureBlobFileProvider" /> class with blob metadata caching backed by a caller-supplied <see cref="IMemoryCache" />.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="cache">The cache used to store blob metadata. The lifetime of this cache is owned by the caller.</param>
    /// <param name="cacheOptions">The cache options supplying the absolute expirations for found and not-found entries.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="cache" /> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="cacheOptions" /> is <c>null</c>.</exception>
    public AzureBlobFileProvider(AzureBlobFileSystemOptions options, IMemoryCache cache, AzureBlobFileSystemCacheOptions cacheOptions)
        : this(GetContainerClient(options), options.ContainerRootPath, cache, cacheOptions)
    { }

    /// <inheritdoc />
    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        var path = GetFullPath(subpath);

        // Get all blobs and iterate to fetch all pages
        var blobs = _containerClient.GetBlobsByHierarchy(delimiter: "/", prefix: path).ToList();

        return blobs.Count == 0
            ? NotFoundDirectoryContents.Singleton
            : new AzureBlobDirectoryContents(_containerClient, blobs);
    }

    /// <inheritdoc />
    public IFileInfo GetFileInfo(string subpath)
    {
        var path = GetFullPath(subpath);

        if (TryGetFromCache(path, out IFileInfo? cached))
        {
            return cached;
        }

        BlobClient blobClient = _containerClient.GetBlobClient(path);

        IFileInfo fileInfo;
        bool found;
        try
        {
            BlobProperties properties = blobClient.GetProperties().Value;
            fileInfo = new AzureBlobItemInfo(blobClient, properties);
            found = true;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            fileInfo = new NotFoundFileInfo(AzureBlobItemInfo.ParseName(path));
            found = false;
        }

        TrySetCache(path, fileInfo, found);

        return fileInfo;
    }

    /// <inheritdoc />
    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    private string GetFullPath(string subpath) => _containerRootPath + subpath.EnsureStartsWith('/');

    private bool TryGetFromCache(string path, [NotNullWhen(true)] out IFileInfo? value)
    {
        if (_cache is null)
        {
            value = null;
            return false;
        }

        try
        {
            return _cache.TryGetValue(path, out value) && value is not null;
        }
        catch (ObjectDisposedException)
        {
            // Cache was disposed mid-request (options change or app shutdown); fall through to fetch from blob.
            value = null;
            return false;
        }
    }

    private void TrySetCache(string path, IFileInfo fileInfo, bool found)
    {
        if (_cache is null || _cacheOptions is null)
        {
            return;
        }

        try
        {
            _cache.Set(path, fileInfo, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = found ? _cacheOptions.HitDuration : _cacheOptions.MissDuration,
                Size = 1,
            });
        }
        catch (ObjectDisposedException)
        {
            // Cache was disposed mid-request (options change or app shutdown); skip caching this entry.
        }
    }

    private static BlobContainerClient GetContainerClient(AzureBlobFileSystemOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.CreateBlobContainerClient();
    }
}
