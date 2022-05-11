using Azure.Storage.Blobs;
using Umbraco.Cms.Core.IO;

namespace Umbraco.StorageProviders.AzureBlob.IO
{
    /// <summary>
    /// Represents an Azure Blob Storage file system.
    /// </summary>
    public interface IAzureBlobFileSystem : IFileSystem
    {
        /// <summary>
        /// Get the <see cref="BlobClient" />.
        /// </summary>
        /// <param name="path">The relative path to the blob.</param>
        /// <returns>
        /// A <see cref="BlobClient" />.
        /// </returns>
        BlobClient GetBlobClient(string path);
    }
}
