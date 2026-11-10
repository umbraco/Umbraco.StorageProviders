namespace Umbraco.StorageProviders.AzureBlob.IO;

/// <summary>
/// The <see cref="AzureBlobFileSystemOptions" /> settings a Blob Container client factory needs.
/// </summary>
[Flags]
internal enum BlobContainerClientRequirements
{
    /// <summary>
    /// No settings are needed (e.g. the client is supplied, or the factory is custom).
    /// </summary>
    None = 0,

    /// <summary>
    /// The <see cref="AzureBlobFileSystemOptions.ConnectionString" /> is needed.
    /// </summary>
    ConnectionString = 1,

    /// <summary>
    /// The <see cref="AzureBlobFileSystemOptions.ContainerName" /> is needed.
    /// </summary>
    ContainerName = 2,
}
