using System.ComponentModel.DataAnnotations;
using Azure.Storage.Blobs;

namespace Umbraco.StorageProviders.AzureBlob.IO;

/// <summary>
/// The Azure Blob File System options.
/// </summary>
public sealed class AzureBlobFileSystemOptions : IValidatableObject
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
    /// Gets or sets the in-memory cache settings applied to blob metadata lookups by the read-only file provider.
    /// </summary>
    /// <value>
    /// The in-memory cache settings.
    /// </value>
    public AzureBlobFileSystemCacheOptions Cache { get; set; } = new();

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

    /// <inheritdoc />
    /// <remarks>
    /// Recurses into the nested <see cref="Cache" /> options so its <see cref="IValidatableObject" /> implementation is honored by <c>ValidateDataAnnotations()</c>.
    /// </remarks>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Cache is null)
        {
            yield break;
        }

        var nestedResults = new List<ValidationResult>();
        Validator.TryValidateObject(Cache, new ValidationContext(Cache), nestedResults, validateAllProperties: true);

        foreach (ValidationResult result in nestedResults)
        {
            yield return new ValidationResult(
                result.ErrorMessage,
                result.MemberNames.Select(member => $"{nameof(Cache)}.{member}"));
        }
    }
}
