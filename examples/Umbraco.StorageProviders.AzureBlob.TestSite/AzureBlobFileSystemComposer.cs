using Azure.Core;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.StorageProviders.AzureBlob.TestSite;

internal sealed class AzureBlobFileSystemComposer : IComposer
{
    private static readonly BlobClientOptions _blobClientOptions = new BlobClientOptions
    {
        Retry = {
            Mode = RetryMode.Exponential,
            MaxRetries = 3
        }
    };

    public void Compose(IUmbracoBuilder builder)
        => builder
        // Add using default options (overly verbose, but shows how to revert back to the default)
        .AddAzureBlobMediaFileSystem(options => options.CreateBlobContainerClientUsingDefault())
        // Add using options
        .AddAzureBlobMediaFileSystem(options => options.CreateBlobContainerClientUsingOptions(_blobClientOptions))
        // If the connection string is parsed to a URI, use the delegate to create a BlobContainerClient
        .AddAzureBlobMediaFileSystem(options => options.TryCreateBlobContainerClientUsingUri(uri => new BlobContainerClient(uri, _blobClientOptions)))
        // Add the ImageSharp IImageCache implementation using the default media file system and "cache" container root path
        .AddAzureBlobImageSharpCache()
        // Add notification handler to create the media file system on install if it doesn't exist
        .AddNotificationHandler<UnattendedInstallNotification, AzureBlobMediaFileSystemCreateIfNotExistsHandler>();

    private sealed class AzureBlobMediaFileSystemCreateIfNotExistsHandler : INotificationHandler<UnattendedInstallNotification>
    {
        private readonly AzureBlobFileSystemOptions _options;

        public AzureBlobMediaFileSystemCreateIfNotExistsHandler(IOptionsMonitor<AzureBlobFileSystemOptions> options)
            => _options = options.Get(AzureBlobFileSystemOptions.MediaFileSystemName);

        public void Handle(UnattendedInstallNotification notification)
            => AzureBlobFileSystem.CreateIfNotExists(_options);
    }
}
