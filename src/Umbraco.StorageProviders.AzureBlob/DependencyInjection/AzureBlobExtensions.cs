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
    /// Extension methods to help registering Azure Blob File Systems.
    /// </summary>
    public static class AzureBlobExtensions
    {
        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem"/> in the <see cref="IServiceCollection"/>, with it's configuration
        /// loaded from <c>Umbraco:Storage:AzureBlob:{name}</c> where {name} is the value of the <paramref name="name"/> parameter.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder"/>.</param>
        /// <param name="name">The name of the file system.</param>
        /// <param name="path">The path to map the filesystem to.</param>
        /// <returns>The <see cref="IUmbracoBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="name"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is null or whitespace.</exception>
        public static IUmbracoBuilder AddAzureBlobFileSystem(this IUmbracoBuilder builder, string name, string path)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));

            builder.Services.TryAddSingleton<IAzureBlobFileSystemProvider, AzureBlobFileSystemProvider>();

            builder.Services
                .AddOptions<AzureBlobFileSystemOptions>(name)
                .BindConfiguration($"Umbraco:Storage:AzureBlob:{name}")
                .Configure(options => options.VirtualPath = path)
                .ValidateDataAnnotations();

            return builder;
        }

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem"/> in the <see cref="IServiceCollection"/>, with it's configuration
        /// loaded from <c>Umbraco:Storage:AzureBlob:{name}</c> where {name} is the value of the <paramref name="name"/> parameter.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder"/>.</param>
        /// <param name="name">The name of the file system.</param>
        /// <param name="path">The path to map the filesystem to.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions"/>.</param>
        /// <returns>The <see cref="IUmbracoBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="name"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is null or whitespace.</exception>
        public static IUmbracoBuilder AddAzureBlobFileSystem(this IUmbracoBuilder builder,
            string name, string path, Action<AzureBlobFileSystemOptions> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));

            AddAzureBlobFileSystem(builder, name, path);

            builder.Services
                .AddOptions<AzureBlobFileSystemOptions>(name)
                .Configure(configure);

            return builder;
        }

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem"/> in the <see cref="IServiceCollection"/>, with it's configuration
        /// loaded from <c>Umbraco:Storage:AzureBlob:{name}</c> where {name} is the value of the <paramref name="name"/> parameter.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder"/>.</param>
        /// <param name="name">The name of the file system.</param>
        /// <param name="path">The path to map the filesystem to.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions"/>.</param>
        /// <returns>The <see cref="IUmbracoBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="name"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is null or whitespace.</exception>
        public static IUmbracoBuilder AddAzureBlobFileSystem(this IUmbracoBuilder builder,
            string name, string path, Action<AzureBlobFileSystemOptions, IServiceProvider> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));

            AddAzureBlobFileSystem(builder, name, path);

            builder.Services
                .AddOptions<AzureBlobFileSystemOptions>(name)
                .Configure(configure);

            return builder;
        }

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem"/> and it's dependencies configured for media.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder"/>.</param>
        /// <returns>The <see cref="IUmbracoBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            AddAzureBlobFileSystem(builder, AzureBlobFileSystemOptions.MediaFileSystemName, "~/media",
                (options, provider) =>
                {
                    var globalSettingsOptions = provider.GetRequiredService<IOptions<GlobalSettings>>();
                    options.VirtualPath = globalSettingsOptions.Value.UmbracoMediaPath;
                });


            builder.Services.TryAddSingleton<AzureBlobMediaMiddleware>();

            builder.Services.AddUnique<IImageProvider, AzureBlobImageProvider>();
            builder.Services.AddUnique<IImageCache>(provider => new AzureBlobImageCache(provider
                    .GetRequiredService<IOptionsFactory<AzureBlobFileSystemOptions>>()
                    .Create(AzureBlobFileSystemOptions.MediaFileSystemName)
            ));

            builder.SetMediaFileSystem(provider => provider.GetRequiredService<IAzureBlobFileSystemProvider>()
                .GetFileSystem(AzureBlobFileSystemOptions.MediaFileSystemName));

            return builder;
        }

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem"/> and it's dependencies configured for media.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder"/>.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions"/>.</param>
        /// <returns>The <see cref="IUmbracoBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder,
            Action<AzureBlobFileSystemOptions> configure)
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
        /// Registers a <see cref="IAzureBlobFileSystem"/> and it's dependencies configured for media.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder"/>.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions"/>.</param>
        /// <returns>The <see cref="IUmbracoBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
        public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder,
            Action<AzureBlobFileSystemOptions, IServiceProvider> configure)
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
        /// Registers and configures the <see cref="CdnMediaUrlProvider"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder"/>.</param>
        /// <returns>The <see cref="IUmbracoBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
        public static IUmbracoBuilder AddCdnMediaUrlProvider(this IUmbracoBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

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

            builder.MediaUrlProviders()
                .Insert<CdnMediaUrlProvider>();

            return builder;
        }

        /// <summary>
        /// Registers and configures the <see cref="CdnMediaUrlProvider"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder"/>.</param>
        /// <param name="configure">An action used to configure the <see cref="CdnMediaUrlProviderOptions"/>.</param>
        /// <returns>The <see cref="IUmbracoBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
        public static IUmbracoBuilder AddCdnMediaUrlProvider(this IUmbracoBuilder builder,
            Action<CdnMediaUrlProviderOptions> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            AddCdnMediaUrlProvider(builder);

            builder.Services
                .AddOptions<CdnMediaUrlProviderOptions>()
                .Configure(configure);

            return builder;
        }

        /// <summary>
        /// Registers and configures the <see cref="CdnMediaUrlProvider"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder"/>.</param>
        /// <param name="configure">An action used to configure the <see cref="CdnMediaUrlProviderOptions"/>.</param>
        /// <returns>The <see cref="IUmbracoBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
        public static IUmbracoBuilder AddCdnMediaUrlProvider(this IUmbracoBuilder builder,
            Action<CdnMediaUrlProviderOptions, IServiceProvider> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            AddCdnMediaUrlProvider(builder);

            builder.Services
                .AddOptions<CdnMediaUrlProviderOptions>()
                .Configure(configure);

            return builder;
        }

        /// <summary>
        /// Adds the <see cref="AzureBlobMediaMiddleware" />.
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
        /// Adds the <see cref="AzureBlobMediaMiddleware"/>.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> or is null.</exception>
        public static IApplicationBuilder UseAzureBlobMediaFileSystem(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            app.UseMiddleware<AzureBlobMediaMiddleware>();

            return app;
        }
    }
}
