using System.ComponentModel.DataAnnotations;

namespace Umbraco.StorageProviders.AzureBlob.ImageSharp;

/// <summary>
/// Configuration options for Azure Blob Storage ImageSharp cache behavior.
/// </summary>
public sealed class AzureBlobImageSharpCacheOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string ConfigurationSectionName = "Umbraco:Storage:AzureBlob:ImageSharpCache";

    /// <summary>
    /// Gets or sets a value indicating whether cache metadata should be stored in memory.
    /// </summary>
    public bool EnableMetadataCache { get; set; } = true;

    /// <summary>
    /// Gets or sets the metadata cache duration.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan MetadataCacheDuration { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the maximum number of concurrent cache operations. Set to 0 or less to disable.
    /// </summary>
    public int MaxConcurrentImageRequests { get; set; }
}
