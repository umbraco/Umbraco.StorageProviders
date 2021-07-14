using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Resolvers;

namespace Umbraco.StorageProviders.AzureBlob.Imaging
{
    /// <inheritdoc />
    public class AzureBlobImageResolver : IImageResolver
    {
        private readonly BlobClient _blobClient;

        /// <summary>
        /// Creates a new instance of <see cref="AzureBlobImageResolver"/>.
        /// </summary>
        /// <param name="blobClient"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public AzureBlobImageResolver(BlobClient blobClient)
        {
            _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
        }

        /// <inheritdoc />
        public async Task<ImageMetadata> GetMetaDataAsync()
        {
            var properties = await _blobClient.GetPropertiesAsync().ConfigureAwait(false);
            return new ImageMetadata(properties.Value.LastModified.UtcDateTime, properties.Value.ContentLength);
        }

        /// <inheritdoc />
        public async Task<Stream> OpenReadAsync() => await _blobClient.OpenReadAsync().ConfigureAwait(false);
    }
}
