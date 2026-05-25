using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Azure.Core;
using Azure.Storage.Blobs;

namespace Umbraco.StorageProviders.AzureBlob.IO;

/// <summary>
/// Retry and timeout settings applied to the default <see cref="BlobContainerClient" /> created from <see cref="AzureBlobFileSystemOptions" />.
/// </summary>
/// <remarks>
/// These defaults are intentionally more conservative than the Azure SDK defaults to bound the worst-case time a
/// blob operation can spend waiting on blob storage (the default SDK policy permits 3 retries with a 100-second
/// network timeout each, which can tie up a thread for several minutes per call). As these settings also apply to
/// writes, <see cref="NetworkTimeout" /> must remain high enough to upload the largest media files in a single operation.
/// </remarks>
public sealed class AzureBlobFileSystemRetryOptions : IValidatableObject
{
    private const int DefaultMaxRetries = 2;
    private const string DefaultNetworkTimeout = "00:00:30";
    private const RetryMode DefaultMode = RetryMode.Exponential;
    private const string DefaultDelay = "00:00:00.800";
    private const string DefaultMaxDelay = "00:00:05";

    /// <summary>
    /// Gets or sets the maximum number of retry attempts before giving up.
    /// </summary>
    /// <value>
    /// The maximum retries.
    /// </value>
    [DefaultValue(DefaultMaxRetries)]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be a non-negative integer.")]
    public int MaxRetries { get; set; } = DefaultMaxRetries;

    /// <summary>
    /// Gets or sets the timeout applied to an individual network operation.
    /// </summary>
    /// <value>
    /// The network timeout.
    /// </value>
    [DefaultValue(typeof(TimeSpan), DefaultNetworkTimeout)]
    public TimeSpan NetworkTimeout { get; set; } = TimeSpan.Parse(DefaultNetworkTimeout, CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets or sets the approach to use for calculating retry delays.
    /// </summary>
    /// <value>
    /// The mode.
    /// </value>
    [DefaultValue(DefaultMode)]
    [EnumDataType(typeof(RetryMode), ErrorMessage = "{0} must be a valid retry mode.")]
    public RetryMode Mode { get; set; } = DefaultMode;

    /// <summary>
    /// Gets or sets the delay between retry attempts for a fixed approach or the delay on which to base
    /// calculations for a backoff-based approach.
    /// </summary>
    /// <value>
    /// The delay.
    /// </value>
    [DefaultValue(typeof(TimeSpan), DefaultDelay)]
    public TimeSpan Delay { get; set; } = TimeSpan.Parse(DefaultDelay, CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets or sets the maximum permissible delay between retry attempts when using a backoff approach.
    /// </summary>
    /// <value>
    /// The maximum delay.
    /// </value>
    [DefaultValue(typeof(TimeSpan), DefaultMaxDelay)]
    public TimeSpan MaxDelay { get; set; } = TimeSpan.Parse(DefaultMaxDelay, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NetworkTimeout <= TimeSpan.Zero)
        {
            yield return new ValidationResult($"{nameof(NetworkTimeout)} must be a positive duration; got {NetworkTimeout}.", [nameof(NetworkTimeout)]);
        }

        if (Delay < TimeSpan.Zero)
        {
            yield return new ValidationResult($"{nameof(Delay)} must be a non-negative duration; got {Delay}.", [nameof(Delay)]);
        }

        if (MaxDelay < TimeSpan.Zero)
        {
            yield return new ValidationResult($"{nameof(MaxDelay)} must be a non-negative duration; got {MaxDelay}.", [nameof(MaxDelay)]);
        }
    }
}
