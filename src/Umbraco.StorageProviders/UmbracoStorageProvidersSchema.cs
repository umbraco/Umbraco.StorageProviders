using Umbraco.StorageProviders;

/// <summary>
/// Defines the appsettings JSON schema for Umbraco Storage Providers configuration.
/// </summary>
internal sealed class UmbracoStorageProvidersSchema
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
        /// The CDN media URL provider options.
        /// </summary>
        public required CdnMediaUrlProviderOptions Cdn { get; set; }
    }
}
