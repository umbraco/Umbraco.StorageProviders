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
        /// <exception cref="System.ArgumentNullException">builder</exception>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.AddAzureBlobFileSystem(AzureBlobFileSystemOptions.MediaFileSystemName, "~/media",
                (options, provider) =>
                {
                    var globalSettingsOptions = provider.GetRequiredService<IOptions<GlobalSettings>>();
                    options.VirtualPath = globalSettingsOptions.Value.UmbracoMediaPath;
                });

            builder.Services.TryAddSingleton<AzureBlobFileSystemMiddleware>();

            // ImageSharp image providers (remove all to ensure the correct order, as PhysicalFileSystemProvider matches all requests)
            builder.Services.RemoveAll<IImageProvider>();
            builder.Services.AddSingleton<IImageProvider, AzureBlobFileSystemImageProvider>();
            builder.Services.AddSingleton<IImageProvider, PhysicalFileSystemProvider>();

            // ImageSharp image cache
            builder.Services.AddUnique<IImageCache, AzureBlobFileSystemImageCache>();

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
        /// <exception cref="System.ArgumentNullException">builder
        /// or
        /// configure</exception>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder, Action<AzureBlobFileSystemOptions> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            AddAzureBlobMediaFileSystem(builder);

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
        /// <exception cref="System.ArgumentNullException">builder
        /// or
        /// configure</exception>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder, Action<AzureBlobFileSystemOptions, IServiceProvider> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            AddAzureBlobMediaFileSystem(builder);

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
        /// <exception cref="System.ArgumentNullException">builder</exception>
        public static IUmbracoApplicationBuilderContext UseAzureBlobMediaFileSystem(this IUmbracoApplicationBuilderContext builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

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
        /// <exception cref="System.ArgumentNullException">app</exception>
        public static IApplicationBuilder UseAzureBlobMediaFileSystem(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            app.UseMiddleware<AzureBlobFileSystemMiddleware>();

            return app;
        }
    }
}
