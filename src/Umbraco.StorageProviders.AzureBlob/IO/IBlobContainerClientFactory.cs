using Azure.Storage.Blobs;

namespace Umbraco.StorageProviders.AzureBlob.IO
{
    /// <summary>
    /// The <see cref="BlobContainerClient"/> factory.
    /// </summary>
    public interface IBlobContainerClientFactory
    {
        /// <summary>
        /// Builds a configured <see cref="BlobContainerClient" />.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>
        /// The configured <see cref="BlobContainerClient" />.
        /// </returns>
        BlobContainerClient Build(AzureBlobFileSystemOptions options);
    }
}
