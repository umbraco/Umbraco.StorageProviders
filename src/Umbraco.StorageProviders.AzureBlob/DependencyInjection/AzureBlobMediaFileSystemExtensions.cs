using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Web.Caching;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Infrastructure.DependencyInjection;
using Umbraco.Extensions;
using Umbraco.StorageProviders.AzureBlob.Imaging;
using Umbraco.StorageProviders.AzureBlob.IO;

// ReSharper disable once CheckNamespace
// uses same namespace as Umbraco Core for easier discoverability
namespace Umbraco.Cms.Core.DependencyInjection
{
    /// <summary>
    /// Extension methods to help registering Azure Blob Storage file systems for Umbraco media.
    /// </summary>
    public static class AzureBlobMediaFileSystemExtensions
    {
        internal static IUmbracoBuilder AddInternal(this IUmbracoBuilder builder, bool useAzureBlobImageCache, Action<OptionsBuilder<AzureBlobFileSystemOptions>>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.AddInternal(AzureBlobFileSystemOptions.MediaFileSystemName, optionsBuilder =>
            {
                optionsBuilder.Configure<IOptions<GlobalSettings>>((options, globalSettings) => options.VirtualPath = globalSettings.Value.UmbracoMediaPath);
                configure?.Invoke(optionsBuilder);
            });

            // ImageSharp image cache
            if (useAzureBlobImageCache)
            {
                builder.Services.AddUnique<IImageCache, AzureBlobFileSystemImageCache>();
            }

            builder.SetMediaFileSystem(provider => provider.GetRequiredService<IAzureBlobFileSystemProvider>().GetFileSystem(AzureBlobFileSystemOptions.MediaFileSystemName));

            return builder;
        }

        /// <summary>
        /// Registers an <see cref="IAzureBlobFileSystem" /> and it's dependencies configured for media.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="useAzureBlobImageCache">If set to <c>true</c> also configures Azure Blob Storage for the image cache.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder, bool useAzureBlobImageCache = true)
            => builder.AddInternal(useAzureBlobImageCache);

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> and it's dependencies configured for media.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
        /// <param name="useAzureBlobImageCache">If set to <c>true</c> also configures Azure Blob Storage for the image cache.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder, Action<AzureBlobFileSystemOptions> configure, bool useAzureBlobImageCache = true)
            => builder.AddInternal(useAzureBlobImageCache, x => x.Configure(configure));

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> and it's dependencies configured for media.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
        /// <param name="useAzureBlobImageCache">If set to <c>true</c> also configures Azure Blob Storage for the image cache.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        public static IUmbracoBuilder AddAzureBlobFileSystem(this IUmbracoBuilder builder, Action<AzureBlobFileSystemOptions, IServiceProvider> configure, bool useAzureBlobImageCache = true)
            => builder.AddInternal(useAzureBlobImageCache, x => x.Configure(configure));

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> and it's dependencies configured for media.
        /// </summary>
        /// <typeparam name="TDep">A dependency used by the configure action.</typeparam>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
        /// <param name="useAzureBlobImageCache">If set to <c>true</c> also configures Azure Blob Storage for the image cache.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        public static IUmbracoBuilder AddAzureBlobFileSystem<TDep>(this IUmbracoBuilder builder, Action<AzureBlobFileSystemOptions, TDep> configure, bool useAzureBlobImageCache = true)
            where TDep : class
            => builder.AddInternal(useAzureBlobImageCache, x => x.Configure(configure));
    }
}
