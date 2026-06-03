using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
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
    /// Gets or sets the retry and timeout settings applied to the default Blob client.
    /// </summary>
    /// <remarks>
    /// Only honored by the default factory. If a custom <see cref="BlobClientOptions" /> is supplied via
    /// <see cref="AzureBlobFileSystemOptionsExtensions.CreateBlobContainerClientUsingOptions" /> or another override,
    /// these values are ignored and the supplied options are used as-is.
    /// </remarks>
    public AzureBlobFileSystemRetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Gets or sets the Azure Blob Container client factory.
    /// </summary>
    /// <value>
    /// The Azure Blob Container client factory.
    /// </value>
    [JsonIgnore] // Set in code, not bound from configuration; excluded from the generated appsettings schema.
    internal Func<AzureBlobFileSystemOptions, BlobContainerClient> BlobContainerClientFactory { get; set; } = DefaultBlobContainerClientFactory;

    /// <summary>
    /// Gets the default Azure Blob Container client factory.
    /// </summary>
    /// <value>
    /// The default Azure Blob Container client factory.
    /// </value>
    internal static Func<AzureBlobFileSystemOptions, BlobContainerClient> DefaultBlobContainerClientFactory => options => new BlobContainerClient(options.ConnectionString, options.ContainerName, options.ConfigureRetry(new BlobClientOptions()));

    /// <summary>
    /// Applies the <see cref="Retry" /> settings from this options instance to the supplied <see cref="BlobClientOptions" />.
    /// </summary>
    /// <param name="blobClientOptions">The Blob client options to configure.</param>
    /// <returns>
    /// The same <paramref name="blobClientOptions" /> instance, to allow fluent chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="blobClientOptions" /> is <c>null</c>.</exception>
    /// <remarks>
    /// Use this when supplying a custom <see cref="BlobClientOptions" /> (e.g. via <see cref="AzureBlobFileSystemOptionsExtensions.CreateBlobContainerClientUsingOptions" />) to ensure the same retry policy is applied.
    /// </remarks>
    public BlobClientOptions ConfigureRetry(BlobClientOptions blobClientOptions)
        => Retry.Configure(blobClientOptions);

    /// <inheritdoc />
    /// <remarks>
    /// Recurses into the nested <see cref="Cache" /> and <see cref="Retry" /> options so their validation attributes and
    /// <see cref="IValidatableObject" /> implementations are honored by <c>ValidateDataAnnotations()</c>.
    /// </remarks>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return [..ValidateObject(Cache), ..ValidateObject(Retry)];

        static IEnumerable<ValidationResult> ValidateObject(object? instance, [CallerArgumentExpression(nameof(instance))] string? prefix = null)
        {
            if (instance is null)
            {
                yield break;
            }

            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(instance, new ValidationContext(instance), validationResults, validateAllProperties: true);

            foreach (ValidationResult validationResult in validationResults)
            {
                yield return new ValidationResult(validationResult.ErrorMessage, validationResult.MemberNames.Select(member => $"{prefix}.{member}"));
            }
        }
    }
}
