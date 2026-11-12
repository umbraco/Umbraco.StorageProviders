using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.FileProviders;

namespace Umbraco.StorageProviders.AzureBlob;

/// <summary>
/// Represents an Azure Blob Storage blob item.
/// </summary>
/// <seealso cref="Microsoft.Extensions.FileProviders.IFileInfo" />
public sealed class AzureBlobItemInfo : IFileInfo
{
    private readonly BlobClient _blobClient;
    private readonly ETag? _etag;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobItemInfo" /> class.
    /// </summary>
    /// <param name="blobClient">The blob client.</param>
    /// <param name="properties">The properties.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="blobClient" /> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="properties" /> is <c>null</c>.</exception>
    public AzureBlobItemInfo(BlobClient blobClient, BlobProperties properties)
        : this(blobClient)
    {
        ArgumentNullException.ThrowIfNull(properties);

        LastModified = properties.LastModified;
        Length = properties.ContentLength;
        _etag = properties.ETag;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobItemInfo" /> class.
    /// </summary>
    /// <param name="blobClient">The blob client.</param>
    /// <param name="properties">The properties.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="blobClient" /> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="properties" /> is <c>null</c>.</exception>
    public AzureBlobItemInfo(BlobClient blobClient, BlobItemProperties properties)
        : this(blobClient)
    {
        ArgumentNullException.ThrowIfNull(properties);

        LastModified = properties.LastModified.GetValueOrDefault();
        Length = properties.ContentLength.GetValueOrDefault(-1);
        _etag = properties.ETag;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobItemInfo" /> class.
    /// </summary>
    /// <param name="blobClient">The blob client.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="blobClient" /> is <c>null</c>.</exception>
    private AzureBlobItemInfo(BlobClient blobClient)
    {
        _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));

        Name = ParseName(blobClient.Name);
    }

    /// <inheritdoc />
    public bool Exists => true;

    /// <inheritdoc />
    public bool IsDirectory => false;

    /// <inheritdoc />
    public DateTimeOffset LastModified { get; }

    /// <inheritdoc />
    public long Length { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? PhysicalPath => null;

    /// <inheritdoc />
    /// <remarks>
    /// Creating the stream doesn't make a request: the download starts on the first read, pinned to the
    /// ETag this item was created with. <c>BlobClient.OpenRead()</c> would first make a synchronous
    /// GetProperties request, fetching the properties this item already holds.
    /// </remarks>
    public Stream CreateReadStream() => new AzureBlobReadStream(_blobClient, Length, _etag);

    /// <summary>
    /// Parses the name from the file path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>
    /// The name.
    /// </returns>
    internal static string ParseName(string path) => path[(path.LastIndexOf('/') + 1)..];
}
