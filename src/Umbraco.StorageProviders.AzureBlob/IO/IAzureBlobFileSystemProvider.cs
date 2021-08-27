namespace Umbraco.StorageProviders.AzureBlob.IO
{
    /// <summary>
    /// The Azure Blob file system provider.
    /// </summary>
    public interface IAzureBlobFileSystemProvider
    {
        /// <summary>
        /// Get the file system by its <paramref name="name" />.
        /// </summary>
        /// <param name="name">The name of the <see cref="IAzureBlobFileSystem" />.</param>
        /// <returns>
        /// The <see cref="IAzureBlobFileSystem" />.
        /// </returns>
        IAzureBlobFileSystem GetFileSystem(string name);
    }
}
