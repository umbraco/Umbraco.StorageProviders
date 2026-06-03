using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.FileProviders;

namespace Umbraco.StorageProviders.AzureBlob.IO;

/// <summary>
/// In-memory cache settings applied to blob metadata lookups performed by the read-only file provider.
/// </summary>
/// <remarks>
/// Caching blob metadata (size, last modified) avoids a network round-trip to Azure Blob Storage
/// on every media request. Under load the default Azure SDK retry policy can hold a thread for many seconds
/// per call, so eliminating the round-trip for the steady-state hot path significantly reduces both latency
/// and thread-pool pressure. Metadata is cached per blob path with a short absolute expiration.
/// </remarks>
public sealed class AzureBlobFileSystemCacheOptions : IValidatableObject
{
    private const bool DefaultEnabled = true;
    private const string DefaultHitDuration = "00:00:30";
    private const string DefaultMissDuration = "00:00:05";
    private const long DefaultSizeLimit = 10_000;

    /// <summary>
    /// Gets or sets a value indicating whether blob metadata caching is enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> if caching is enabled; otherwise, <c>false</c>.
    /// </value>
    [DefaultValue(DefaultEnabled)]
    public bool Enabled { get; set; } = DefaultEnabled;

    /// <summary>
    /// Gets or sets how long a successful metadata lookup is cached.
    /// </summary>
    /// <value>
    /// The cache duration for found blobs.
    /// </value>
    /// <remarks>
    /// Writes through this filesystem instance (<see cref="AzureBlobFileSystem.AddFile(string, System.IO.Stream)" />,
    /// <see cref="AzureBlobFileSystem.DeleteFile(string)" />, <see cref="AzureBlobFileSystem.DeleteDirectory(string)" />)
    /// invalidate the affected cache entries immediately. Only writes performed outside this instance (another process,
    /// another instance, or directly via the Azure SDK) can leave metadata stale for up to this duration. Defaults to 30 seconds.
    /// </remarks>
    [DefaultValue(typeof(TimeSpan), DefaultHitDuration)]
    public TimeSpan HitDuration { get; set; } = TimeSpan.Parse(DefaultHitDuration, CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets or sets how long a not-found metadata result is cached.
    /// </summary>
    /// <value>
    /// The cache duration for not-found blobs.
    /// </value>
    /// <remarks>
    /// Kept shorter than <see cref="HitDuration" /> so newly-uploaded blobs become visible quickly.
    /// Defaults to 5 seconds.
    /// </remarks>
    [DefaultValue(typeof(TimeSpan), DefaultMissDuration)]
    public TimeSpan MissDuration { get; set; } = TimeSpan.Parse(DefaultMissDuration, CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets or sets the maximum number of cached <see cref="IFileInfo" /> entries.
    /// </summary>
    /// <value>
    /// The cache size limit.
    /// </value>
    /// <remarks>
    /// Each cache entry counts as a single unit. Defaults to 10,000 entries.
    /// </remarks>
    [DefaultValue(DefaultSizeLimit)]
    [Range(0, long.MaxValue, ErrorMessage = "{0} must be a non-negative integer.")]
    public long SizeLimit { get; set; } = DefaultSizeLimit;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (HitDuration <= TimeSpan.Zero)
        {
            yield return new ValidationResult($"{nameof(HitDuration)} must be a positive duration; got {HitDuration}.", [nameof(HitDuration)]);
        }

        if (MissDuration <= TimeSpan.Zero)
        {
            yield return new ValidationResult($"{nameof(MissDuration)} must be a positive duration; got {MissDuration}.", [nameof(MissDuration)]);
        }
    }
}
