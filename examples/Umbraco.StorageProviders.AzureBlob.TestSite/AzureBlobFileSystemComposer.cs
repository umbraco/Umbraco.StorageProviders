using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.StorageProviders.AzureBlob.TestSite;

internal sealed class AzureBlobFileSystemComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder
        .AddAzureBlobMediaFileSystem()
        .AddAzureBlobImageSharpCache()
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
