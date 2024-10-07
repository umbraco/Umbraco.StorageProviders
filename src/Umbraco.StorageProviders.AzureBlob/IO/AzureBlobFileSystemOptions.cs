using System.ComponentModel.DataAnnotations;
using Azure.Storage.Blobs;

namespace Umbraco.StorageProviders.AzureBlob.IO;

/// <summary>
/// The Azure Blob File System options.
/// </summary>
public sealed class AzureBlobFileSystemOptions
{
    /// <summary>
    /// The media filesystem name.
    /// </summary>
    public const string MediaFileSystemName = "Media";

    /// <summary>
    /// Gets or sets the storage account connection string.
    /// </summary>
    [Required]
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the container name.
    /// </summary>
    [Required]
    public required string ContainerName { get; set; }

    /// <summary>
    /// Gets or sets the root path of the container.
    /// </summary>
    public string? ContainerRootPath { get; set; }

    /// <summary>
    /// Gets or sets the virtual path.
    /// </summary>
    [Required]
    public required string VirtualPath { get; set; }

    /// <summary>
    /// Gets or sets the Azure Blob Container client factory.
    /// </summary>
    /// <value>
    /// The Azure Blob Container client factory.
    /// </value>
    internal Func<AzureBlobFileSystemOptions, BlobContainerClient> BlobContainerClientFactory { get; set; } = DefaultBlobContainerClientFactory;

    /// <summary>
    /// Gets the default Azure Blob Container client factory.
    /// </summary>
    /// <value>
    /// The default Azure Blob Container client factory.
    /// </value>
    internal static Func<AzureBlobFileSystemOptions, BlobContainerClient> DefaultBlobContainerClientFactory => options => new BlobContainerClient(options.ConnectionString, options.ContainerName);
}
