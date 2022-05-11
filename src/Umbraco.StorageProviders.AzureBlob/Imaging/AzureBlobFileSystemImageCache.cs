using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Resolvers;
using SixLabors.ImageSharp.Web.Resolvers.Azure;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.StorageProviders.AzureBlob.Imaging
{
    /// <summary>
    /// Implements an Azure Blob Storage based cache storing files in a <c>cache</c> subfolder.
    /// </summary>
    public sealed class AzureBlobFileSystemImageCache : IImageCache
    {
        private const string _cachePath = "cache/";
        private BlobContainerClient _container;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFileSystemImageCache" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
        public AzureBlobFileSystemImageCache(IOptionsMonitor<AzureBlobFileSystemOptions> options)
            : this(AzureBlobFileSystemOptions.MediaFileSystemName, options)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFileSystemImageCache"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="name" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
        public AzureBlobFileSystemImageCache(string name, IOptionsMonitor<AzureBlobFileSystemOptions> options)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(options);

            var fileSystemOptions = options.Get(name);
            _container = new BlobContainerClient(fileSystemOptions.ConnectionString, fileSystemOptions.ContainerName);

            options.OnChange((options, changedName) =>
            {
                if (changedName == name)
                {
                    _container = new BlobContainerClient(options.ConnectionString, options.ContainerName);
                }
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFileSystemImageCache" /> class.
        /// </summary>
        /// <param name="blobContainerClient">The blob container client.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="blobContainerClient" /> is <c>null</c>.</exception>
        public AzureBlobFileSystemImageCache(BlobContainerClient blobContainerClient)
            => _container = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));

        /// <inheritdoc />
        public async Task<IImageCacheResolver?> GetAsync(string key)
        {
            var blob = _container.GetBlobClient(_cachePath + key);

            return !await blob.ExistsAsync().ConfigureAwait(false)
                ? null
                : new AzureBlobStorageCacheResolver(blob);
        }

        /// <inheritdoc />
        public async Task SetAsync(string key, Stream stream, ImageCacheMetadata metadata)
        {
            var blob = _container.GetBlobClient(_cachePath + key);

            await blob.UploadAsync(stream, metadata: metadata.ToDictionary()).ConfigureAwait(false);
        }
    }
}
