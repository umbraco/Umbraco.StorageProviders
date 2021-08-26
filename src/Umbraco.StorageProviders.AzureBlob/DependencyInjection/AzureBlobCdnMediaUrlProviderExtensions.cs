using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.StorageProviders;
using Umbraco.StorageProviders.AzureBlob.IO;

// ReSharper disable once CheckNamespace
// uses same namespace as Umbraco Core for easier discoverability
namespace Umbraco.Cms.Core.DependencyInjection
{
    /// <summary>
    /// Extension methods to help registering a CDN media URL provider.
    /// </summary>
    public static class AzureBlobCdnMediaUrlProviderExtensions
    {
        /// <summary>
        /// Registers and configures the <see cref="CdnMediaUrlProvider" />.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">builder</exception>
        public static IUmbracoBuilder AddAzureBlobCdnMediaUrlProvider(this IUmbracoBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.AddCdnMediaUrlProvider();

            builder.Services.AddOptions<CdnMediaUrlProviderOptions>()
                .BindConfiguration("Umbraco:Storage:AzureBlob:Media:Cdn")
                .Configure<IOptionsFactory<AzureBlobFileSystemOptions>>(
                    (options, factory) =>
                    {
                        var mediaOptions = factory.Create(AzureBlobFileSystemOptions.MediaFileSystemName);
                        if (!string.IsNullOrEmpty(mediaOptions.ContainerName))
                        {
                            options.Url = new Uri(options.Url, mediaOptions.ContainerName);
                        }
                    }
                )
                .ValidateDataAnnotations();

            return builder;
        }
    }
}
