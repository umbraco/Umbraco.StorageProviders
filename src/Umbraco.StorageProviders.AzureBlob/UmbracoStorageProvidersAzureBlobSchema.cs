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
        /// Configuration of Umbraco Storage Providers - Azure Blob Storage, keyed by file system name (e.g. "Media").
        /// </summary>
        public required Dictionary<string, AzureBlobFileSystemOptions> AzureBlob { get; set; }
    }
}
