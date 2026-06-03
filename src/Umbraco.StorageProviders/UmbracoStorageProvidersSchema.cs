using System.Diagnostics.CodeAnalysis;
using Umbraco.StorageProviders;

/// <summary>
/// Defines the appsettings JSON schema for Umbraco Storage Providers configuration.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by the JSON schema generator via reflection.")]
internal sealed class UmbracoStorageProvidersSchema
{
    /// <summary>
    /// Configuration container for all Umbraco products.
    /// </summary>
    public required UmbracoDefinition Umbraco { get; set; }

    /// <summary>
    /// Represents the configuration container for all Umbraco products.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by the JSON schema generator via reflection.")]
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
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by the JSON schema generator via reflection.")]
    public sealed class StorageDefinition
    {
        /// <summary>
        /// The CDN media URL provider options.
        /// </summary>
        public required CdnMediaUrlProviderOptions Cdn { get; set; }
    }
}
