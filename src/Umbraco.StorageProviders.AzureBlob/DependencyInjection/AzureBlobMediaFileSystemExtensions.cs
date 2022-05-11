using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Providers;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Infrastructure.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Extensions;
using Umbraco.StorageProviders.AzureBlob;
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
        /// <summary>
        /// Registers an <see cref="IAzureBlobFileSystem" /> and it's dependencies configured for media.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <remarks>
        /// This will also configure the ImageSharp.Web middleware to use Azure Blob Storage to retrieve the original and cache the processed images.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">builder.</exception>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder)
            => builder.AddAzureBlobMediaFileSystem(true);

        /// <summary>
        /// Registers an <see cref="IAzureBlobFileSystem" /> and it's dependencies configured for media.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="useAzureBlobImageCache">If set to <c>true</c> also configures Azure Blob Storage for the image cache.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">builder.</exception>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder, bool useAzureBlobImageCache)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddAzureBlobFileSystem(AzureBlobFileSystemOptions.MediaFileSystemName, "~/media", (options, provider) =>
            {
                var globalSettingsOptions = provider.GetRequiredService<IOptions<GlobalSettings>>();
                options.VirtualPath = globalSettingsOptions.Value.UmbracoMediaPath;
            });

            builder.Services.TryAddSingleton<AzureBlobFileSystemMiddleware>();

            // ImageSharp image provider/cache
            builder.Services.Insert(0, ServiceDescriptor.Singleton<IImageProvider, AzureBlobFileSystemImageProvider>());
            if (useAzureBlobImageCache)
            {
                builder.Services.AddUnique<IImageCache, AzureBlobFileSystemImageCache>();
            }

            builder.SetMediaFileSystem(provider => provider.GetRequiredService<IAzureBlobFileSystemProvider>()
                .GetFileSystem(AzureBlobFileSystemOptions.MediaFileSystemName));

            return builder;
        }

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> and it's dependencies configured for media.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <remarks>
        /// This will also configure the ImageSharp.Web middleware to use Azure Blob Storage to retrieve the original and cache the processed images.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">builder
        /// or
        /// configure.</exception>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder, Action<AzureBlobFileSystemOptions> configure)
            => builder.AddAzureBlobMediaFileSystem(true, configure);

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> and it's dependencies configured for media.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="useAzureBlobImageCache">If set to <c>true</c> also configures Azure Blob Storage for the image cache.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">builder
        /// or
        /// configure.</exception>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder, bool useAzureBlobImageCache, Action<AzureBlobFileSystemOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            AddAzureBlobMediaFileSystem(builder, useAzureBlobImageCache);

            builder.Services
                .AddOptions<AzureBlobFileSystemOptions>(AzureBlobFileSystemOptions.MediaFileSystemName)
                .Configure(configure);

            return builder;
        }

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> and it's dependencies configured for media.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <remarks>
        /// This will also configure the ImageSharp.Web middleware to use Azure Blob Storage to retrieve the original and cache the processed images.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">builder
        /// or
        /// configure.</exception>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder, Action<AzureBlobFileSystemOptions, IServiceProvider> configure)
            => builder.AddAzureBlobMediaFileSystem(true, configure);

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> and it's dependencies configured for media.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="useAzureBlobImageCache">If set to <c>true</c> also configures Azure Blob Storage for the image cache.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">builder
        /// or
        /// configure.</exception>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder, bool useAzureBlobImageCache, Action<AzureBlobFileSystemOptions, IServiceProvider> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            AddAzureBlobMediaFileSystem(builder, useAzureBlobImageCache);

            builder.Services
                .AddOptions<AzureBlobFileSystemOptions>(AzureBlobFileSystemOptions.MediaFileSystemName)
                .Configure(configure);

            return builder;
        }

        /// <summary>
        /// Adds the <see cref="AzureBlobFileSystemMiddleware" />.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoApplicationBuilderContext" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoApplicationBuilderContext" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">builder.</exception>
        public static IUmbracoApplicationBuilderContext UseAzureBlobMediaFileSystem(this IUmbracoApplicationBuilderContext builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            UseAzureBlobMediaFileSystem(builder.AppBuilder);

            return builder;
        }

        /// <summary>
        /// Adds the <see cref="AzureBlobFileSystemMiddleware" />.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder" />.</param>
        /// <returns>
        /// The <see cref="IApplicationBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">app.</exception>
        public static IApplicationBuilder UseAzureBlobMediaFileSystem(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseMiddleware<AzureBlobFileSystemMiddleware>();

            return app;
        }
    }
}
