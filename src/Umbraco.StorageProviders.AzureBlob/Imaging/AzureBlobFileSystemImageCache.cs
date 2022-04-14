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
    public class AzureBlobFileSystemImageCache : IImageCache
    {
        private const string _cachePath = "cache/";
        private readonly string _name;
        private BlobContainerClient _container;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFileSystemImageCache" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        public AzureBlobFileSystemImageCache(IOptionsMonitor<AzureBlobFileSystemOptions> options)
            : this(AzureBlobFileSystemOptions.MediaFileSystemName, options)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFileSystemImageCache"/> class.
        /// Creates a new instance of <see cref="AzureBlobFileSystemImageCache" />.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="System.ArgumentNullException">options
        /// or
        /// name.</exception>
        protected AzureBlobFileSystemImageCache(string name, IOptionsMonitor<AzureBlobFileSystemOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _name = name ?? throw new ArgumentNullException(nameof(name));

            var fileSystemOptions = options.Get(name);
            _container = new BlobContainerClient(fileSystemOptions.ConnectionString, fileSystemOptions.ContainerName);

            options.OnChange(OptionsOnChange);
        }

        /// <inheritdoc />
        public async Task<IImageCacheResolver?> GetAsync(string key)
        {
            var blob = _container.GetBlobClient(_cachePath + key);

            if (await blob.ExistsAsync().ConfigureAwait(false))
            {
                return new AzureBlobStorageCacheResolver(blob);
            }

            return null;
        }

        /// <inheritdoc />
        public async Task SetAsync(string key, Stream stream, ImageCacheMetadata metadata)
        {
            var blob = _container.GetBlobClient(_cachePath + key);

            await blob.UploadAsync(stream, metadata: metadata.ToDictionary()).ConfigureAwait(false);
        }

        private void OptionsOnChange(AzureBlobFileSystemOptions options, string name)
        {
            if (name != _name)
            {
                return;
            }

            _container = new BlobContainerClient(options.ConnectionString, options.ContainerName);
        }
    }
}
