using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.StorageProviders.AzureBlob.IO;

// ReSharper disable once CheckNamespace
// uses same namespace as Umbraco Core for easier discoverability
namespace Umbraco.Cms.Core.DependencyInjection
{
    /// <summary>
    /// Extension methods to help registering Azure Blob Storage file systems.
    /// </summary>
    public static class AzureBlobFileSystemExtensions
    {
        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> in the <see cref="IServiceCollection" />, with it's configuration
        /// loaded from <c>Umbraco:Storage:AzureBlob:{name}</c> where {name} is the value of the <paramref name="name" /> parameter.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="name">The name of the file system.</param>
        /// <param name="path">The path to map the filesystem to.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">builder
        /// or
        /// name.</exception>
        /// <exception cref="System.ArgumentException">Value cannot be null or whitespace. - path.</exception>
        public static IUmbracoBuilder AddAzureBlobFileSystem(this IUmbracoBuilder builder, string name, string path)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            builder.Services.TryAddSingleton<IAzureBlobFileSystemProvider, AzureBlobFileSystemProvider>();

            builder.Services
                .AddOptions<AzureBlobFileSystemOptions>(name)
                .BindConfiguration($"Umbraco:Storage:AzureBlob:{name}")
                .Configure(options => options.VirtualPath = path)
                .ValidateDataAnnotations();

            return builder;
        }

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> in the <see cref="IServiceCollection" />, with it's configuration
        /// loaded from <c>Umbraco:Storage:AzureBlob:{name}</c> where {name} is the value of the <paramref name="name" /> parameter.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="name">The name of the file system.</param>
        /// <param name="path">The path to map the filesystem to.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">builder
        /// or
        /// name
        /// or
        /// configure.</exception>
        /// <exception cref="System.ArgumentException">Value cannot be null or whitespace. - path.</exception>
        public static IUmbracoBuilder AddAzureBlobFileSystem(this IUmbracoBuilder builder, string name, string path, Action<AzureBlobFileSystemOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            AddAzureBlobFileSystem(builder, name, path);

            builder.Services
                .AddOptions<AzureBlobFileSystemOptions>(name)
                .Configure(configure);

            return builder;
        }

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> in the <see cref="IServiceCollection" />, with it's configuration
        /// loaded from <c>Umbraco:Storage:AzureBlob:{name}</c> where {name} is the value of the <paramref name="name" /> parameter.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="name">The name of the file system.</param>
        /// <param name="path">The path to map the filesystem to.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">builder
        /// or
        /// name
        /// or
        /// configure.</exception>
        /// <exception cref="System.ArgumentException">Value cannot be null or whitespace. - path.</exception>
        public static IUmbracoBuilder AddAzureBlobFileSystem(this IUmbracoBuilder builder, string name, string path, Action<AzureBlobFileSystemOptions, IServiceProvider> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            AddAzureBlobFileSystem(builder, name, path);

            builder.Services
                .AddOptions<AzureBlobFileSystemOptions>(name)
                .Configure(configure);

            return builder;
        }
    }
}
