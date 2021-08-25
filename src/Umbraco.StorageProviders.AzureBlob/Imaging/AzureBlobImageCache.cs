using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Resolvers;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.StorageProviders.AzureBlob.Imaging
{
    /// <inheritdoc />
    public class AzureBlobImageCache : IImageCache
    {
        private readonly BlobContainerClient _container;

        /// <summary>
        /// Creates a new instance of <see cref="AzureBlobImageCache"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public AzureBlobImageCache(AzureBlobFileSystemOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var client = new BlobServiceClient(options.ConnectionString);
            _container = client.GetBlobContainerClient(options.ContainerName);
        }

        /// <inheritdoc />
        public async Task<IImageCacheResolver?> GetAsync(string key)
        {
            var blob = _container.GetBlobClient($"cache/{key}");
            if (await blob.ExistsAsync().ConfigureAwait(false))
                return new AzureBlobImageCacheResolver(blob);

            return null;
        }

        /// <inheritdoc />
        public async Task SetAsync(string key, Stream stream, ImageCacheMetadata metadata)
        {
            var blob = _container.GetBlobClient($"cache/{key}");
            await blob.UploadAsync(stream, metadata: metadata.ToDictionary()).ConfigureAwait(false);
        }
    }
}
