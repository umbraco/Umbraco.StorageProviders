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
    private const string CacheKeyPrefix = "Umbraco.StorageProviders.AzureBlob:FileProviderMetadata:";
    private readonly BlobContainerClient _containerClient;
    private readonly string? _containerRootPath;
    private readonly IMemoryCache? _memoryCache;
    private readonly bool _enableMetadataCache;
    private readonly TimeSpan _metadataCacheDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobFileProvider" /> class.
    /// </summary>
    /// <param name="containerClient">The container client.</param>
    /// <param name="containerRootPath">The container root path.</param>
    /// <param name="options">The options.</param>
    /// <param name="memoryCache">The memory cache for blob metadata.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="containerClient" /> is <c>null</c>.</exception>
    public AzureBlobFileProvider(BlobContainerClient containerClient, string? containerRootPath = null, AzureBlobFileSystemOptions? options = null, IMemoryCache? memoryCache = null)
    {
        _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
        _containerRootPath = containerRootPath?.Trim(Constants.CharArrays.ForwardSlash);
        _memoryCache = memoryCache;
        _enableMetadataCache = options?.EnableFileProviderMetadataCache == true && memoryCache is not null;
        _metadataCacheDuration = options?.FileProviderMetadataCacheDuration ?? TimeSpan.FromSeconds(60);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobFileProvider" /> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="memoryCache">The memory cache for blob metadata.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    public AzureBlobFileProvider(AzureBlobFileSystemOptions options, IMemoryCache? memoryCache = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _containerClient = options.CreateBlobContainerClient();
        _containerRootPath = options.ContainerRootPath?.Trim(Constants.CharArrays.ForwardSlash);
        _memoryCache = memoryCache;
        _enableMetadataCache = options.EnableFileProviderMetadataCache && memoryCache is not null;
        _metadataCacheDuration = options.FileProviderMetadataCacheDuration;
    }

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
        BlobClient blobClient = _containerClient.GetBlobClient(path);

        if (TryGetCachedProperties(path, out BlobProperties cachedProperties))
        {
            return new AzureBlobItemInfo(blobClient, cachedProperties);
        }

        BlobProperties properties;
        try
        {
            properties = blobClient.GetProperties().Value;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return new NotFoundFileInfo(AzureBlobItemInfo.ParseName(path));
        }

        TrySetCachedProperties(path, properties);
        return new AzureBlobItemInfo(blobClient, properties);
    }

    /// <inheritdoc />
    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    private string GetFullPath(string subpath) => _containerRootPath + subpath.EnsureStartsWith('/');

    private bool TryGetCachedProperties(string path, out BlobProperties properties)
    {
        properties = default!;
        if (!_enableMetadataCache || _metadataCacheDuration <= TimeSpan.Zero || _memoryCache is null)
        {
            return false;
        }

        if (_memoryCache.TryGetValue(CacheKeyPrefix + path, out BlobProperties? cachedProperties) && cachedProperties is not null)
        {
            properties = cachedProperties;
            return true;
        }

        return false;
    }

    private void TrySetCachedProperties(string path, BlobProperties properties)
    {
        if (!_enableMetadataCache || _metadataCacheDuration <= TimeSpan.Zero || _memoryCache is null)
        {
            return;
        }

        _memoryCache.Set(CacheKeyPrefix + path, properties, _metadataCacheDuration);
    }
}
