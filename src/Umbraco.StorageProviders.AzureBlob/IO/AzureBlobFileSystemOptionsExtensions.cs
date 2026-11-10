using Azure.Storage.Blobs;

namespace Umbraco.StorageProviders.AzureBlob.IO;

/// <summary>
/// Extension methods for <see cref="AzureBlobFileSystemOptions" />.
/// </summary>
public static class AzureBlobFileSystemOptionsExtensions
{
    private const BlobContainerClientRequirements ConnectionStringAndContainerName = BlobContainerClientRequirements.ConnectionString | BlobContainerClientRequirements.ContainerName;

    /// <summary>
    /// Creates a <see cref="BlobContainerClient" /> using the default constructor accepting a connection string and container name.
    /// </summary>
    /// <param name="options">The Azure Blob File System options.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    public static void CreateBlobContainerClientUsingDefault(this AzureBlobFileSystemOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.SetBlobContainerClientFactory(AzureBlobFileSystemOptions.DefaultBlobContainerClientFactory, ConnectionStringAndContainerName);
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

        options.SetBlobContainerClientFactory(options => new BlobContainerClient(options.ConnectionString, options.ContainerName, blobClientOptions), ConnectionStringAndContainerName);
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
        options.SetBlobContainerClientFactory(
            options =>
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
            },
            ConnectionStringAndContainerName);
    }

    /// <summary>
    /// Uses the supplied <see cref="BlobContainerClient" /> instead of creating one from the connection string (e.g. a client registered in dependency injection by .NET Aspire).
    /// </summary>
    /// <param name="options">The Azure Blob File System options.</param>
    /// <param name="blobContainerClient">The Blob Container client to use.</param>
    /// <remarks>
    /// The client is used as-is: <see cref="AzureBlobFileSystemOptions.ConnectionString" /> and <see cref="AzureBlobFileSystemOptions.Retry" /> are ignored.
    /// <see cref="AzureBlobFileSystemOptions.ContainerName" /> is optional, but must match the name of the supplied container when set.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="blobContainerClient" /> is <c>null</c>.</exception>
    public static void UseBlobContainerClient(this AzureBlobFileSystemOptions options, BlobContainerClient blobContainerClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(blobContainerClient);

        options.SetBlobContainerClientFactory(_ => blobContainerClient, BlobContainerClientRequirements.None, blobContainerClient);
    }

    /// <summary>
    /// Uses the supplied <see cref="BlobServiceClient" /> to get the <see cref="BlobContainerClient" /> for <see cref="AzureBlobFileSystemOptions.ContainerName" />,
    /// instead of creating one from the connection string (e.g. a client registered in dependency injection by .NET Aspire).
    /// </summary>
    /// <param name="options">The Azure Blob File System options.</param>
    /// <param name="blobServiceClient">The Blob Service client to use.</param>
    /// <remarks>
    /// The client is used as-is: <see cref="AzureBlobFileSystemOptions.ConnectionString" /> and <see cref="AzureBlobFileSystemOptions.Retry" /> are ignored.
    /// <see cref="AzureBlobFileSystemOptions.ContainerName" /> is still required.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="blobServiceClient" /> is <c>null</c>.</exception>
    public static void UseBlobServiceClient(this AzureBlobFileSystemOptions options, BlobServiceClient blobServiceClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(blobServiceClient);

        options.SetBlobContainerClientFactory(options => blobServiceClient.GetBlobContainerClient(options.ContainerName), BlobContainerClientRequirements.ContainerName);
    }

    /// <summary>
    /// Creates a <see cref="BlobContainerClient" /> using the specified <paramref name="factory" />.
    /// </summary>
    /// <param name="options">The Azure Blob File System options.</param>
    /// <param name="factory">The delegate to create a <see cref="BlobContainerClient" /> from the options.</param>
    /// <remarks>
    /// <see cref="AzureBlobFileSystemOptions.ConnectionString" /> and <see cref="AzureBlobFileSystemOptions.ContainerName" /> aren't validated,
    /// as only the <paramref name="factory" /> knows whether it needs them.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="factory" /> is <c>null</c>.</exception>
    public static void CreateBlobContainerClientUsing(this AzureBlobFileSystemOptions options, Func<AzureBlobFileSystemOptions, BlobContainerClient> factory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(factory);

        options.SetBlobContainerClientFactory(factory, BlobContainerClientRequirements.None);
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
