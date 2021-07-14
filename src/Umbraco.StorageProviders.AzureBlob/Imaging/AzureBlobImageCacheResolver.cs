using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Resolvers;

namespace Umbraco.StorageProviders.AzureBlob.Imaging
{
    /// <inheritdoc />
    public class AzureBlobImageCacheResolver : IImageCacheResolver
    {
        private readonly BlobClient _blob;

        /// <summary>
        /// Creates a new instance of <see cref="AzureBlobImageCacheResolver"/>.
        /// </summary>
        /// <param name="blob"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public AzureBlobImageCacheResolver(BlobClient blob)
        {
            _blob = blob ?? throw new ArgumentNullException(nameof(blob));
        }

        /// <inheritdoc />
        public async Task<ImageCacheMetadata> GetMetaDataAsync()
        {
            var properties = await _blob.GetPropertiesAsync().ConfigureAwait(false);
            return ImageCacheMetadata.FromDictionary(properties.Value.Metadata);
        }

        /// <inheritdoc />
        public async Task<Stream> OpenReadAsync() => await _blob.OpenReadAsync().ConfigureAwait(false);
    }
}
