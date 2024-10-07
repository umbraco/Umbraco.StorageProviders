using Azure.Storage.Blobs;

namespace Umbraco.StorageProviders.AzureBlob.IO;

/// <summary>
/// Extension methods for <see cref="AzureBlobFileSystemOptions" />.
/// </summary>
public static class AzureBlobFileSystemOptionsExtensions
{
    /// <summary>
    /// Creates a <see cref="BlobContainerClient" /> using the default constructor accepting a connection string and container name.
    /// </summary>
    /// <param name="options">The Azure Blob File System options.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    public static void CreateBlobContainerClientUsingDefault(this AzureBlobFileSystemOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.BlobContainerClientFactory = AzureBlobFileSystemOptions.DefaultBlobContainerClientFactory;
    }

    /// <summary>
    /// Creates a <see cref="BlobContainerClient" /> using the default constructor accepting a connection string, container name and Blob client options.
    /// </summary>
    /// <param name="options">The Azure Blob File System options.</param>
    /// <param name="blobClientOptions">The Azure Blob client options.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    public static void CreateBlobContainerClientUsingOptions(this AzureBlobFileSystemOptions options, BlobClientOptions blobClientOptions)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.BlobContainerClientFactory = options => new BlobContainerClient(options.ConnectionString, options.ContainerName, blobClientOptions);
    }

    /// <summary>
    /// Parses the connection string to a URI and invokes the <paramref name="create" /> delegate to create a <see cref="BlobContainerClient" />.
    /// </summary>
    /// <param name="options">The Azure Blob File System options.</param>
    /// <param name="create">The delegate to create a <see cref="BlobContainerClient" /> from the parsed <see cref="Uri" />.</param>
    /// <remarks>
    /// If the connection string can't be parsed to a URI, the existing delegate will be used instead.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    public static void TryCreateBlobContainerClientUsingUri(this AzureBlobFileSystemOptions options, Func<Uri, BlobContainerClient> create)
    {
        ArgumentNullException.ThrowIfNull(options);

        Func<AzureBlobFileSystemOptions, BlobContainerClient> defaultCreate = options.BlobContainerClientFactory;
        options.BlobContainerClientFactory = options =>
        {
            if (Uri.TryCreate(options.ConnectionString, UriKind.Absolute, out Uri? blobContainerUri))
            {
                var blobUriBuilder = new BlobUriBuilder(blobContainerUri)
                {
                    BlobContainerName = options.ContainerName
                };

                return create(blobUriBuilder.ToUri());
            }

            return defaultCreate(options);
        };
    }

    /// <summary>
    /// Creates the Azure Blob Container client using the configured factory.
    /// </summary>
    /// <param name="options">The Azure Blob File System options.</param>
    /// <returns>
    /// The Azure Blob Container client.
    /// </returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    public static BlobContainerClient CreateBlobContainerClient(this AzureBlobFileSystemOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.BlobContainerClientFactory(options);
    }
}
