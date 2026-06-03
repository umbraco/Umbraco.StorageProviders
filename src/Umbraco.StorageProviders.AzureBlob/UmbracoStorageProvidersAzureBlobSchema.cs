using Umbraco.StorageProviders.AzureBlob.IO;

/// <summary>
/// Defines the appsettings JSON schema for Umbraco Storage Providers - Azure Blob Storage configuration.
/// </summary>
internal sealed class UmbracoStorageProvidersAzureBlobSchema
{
    /// <summary>
    /// Configuration container for all Umbraco products.
    /// </summary>
    public required UmbracoDefinition Umbraco { get; set; }

    /// <summary>
    /// Represents the configuration container for all Umbraco products.
    /// </summary>
    public sealed class UmbracoDefinition
    {
        /// <summary>
        /// Configuration of Umbraco Storage Providers.
        /// </summary>
        public required StorageDefinition Storage { get; set; }
    }

    /// <summary>
    /// Represents the configuration of Umbraco Storage Providers.
    /// </summary>
    public sealed class StorageDefinition
    {
        /// <summary>
        /// Configuration of Umbraco Storage Providers - Azure Blob Storage.
        /// </summary>
        public required AzureBlobDefinition AzureBlob { get; set; }
    }

    /// <summary>
    /// Represents the configuration of Umbraco Storage Providers - Azure Blob Storage.
    /// </summary>
    public sealed class AzureBlobDefinition
    {
        /// <summary>
        /// The Azure Blob File System options for the media file system.
        /// </summary>
        public required AzureBlobFileSystemOptions Media { get; set; }
    }
}
