using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Umbraco.StorageProviders.AzureBlob.IO;

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
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
        public static IUmbracoBuilder AddAzureBlobFileSystem(this IUmbracoBuilder builder, string name)
            => builder.AddInternal(name);

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> in the <see cref="IServiceCollection" />, with it's configuration
        /// loaded from <c>Umbraco:Storage:AzureBlob:{name}</c> where {name} is the value of the <paramref name="name" /> parameter.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="name">The name of the file system.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="name" /> is <c>null</c>.</exception>
        public static IUmbracoBuilder AddAzureBlobFileSystem(this IUmbracoBuilder builder, string name, Action<AzureBlobFileSystemOptions> configure)
            => builder.AddInternal(name, optionsBuilder => optionsBuilder.Configure(configure));

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> in the <see cref="IServiceCollection" />, with it's configuration
        /// loaded from <c>Umbraco:Storage:AzureBlob:{name}</c> where {name} is the value of the <paramref name="name" /> parameter.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="name">The name of the file system.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="name" /> is <c>null</c>.</exception>
        public static IUmbracoBuilder AddAzureBlobFileSystem(this IUmbracoBuilder builder, string name, Action<AzureBlobFileSystemOptions, IServiceProvider> configure)
            => builder.AddInternal(name, optionsBuilder => optionsBuilder.Configure(configure));

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> in the <see cref="IServiceCollection" />, with it's configuration
        /// loaded from <c>Umbraco:Storage:AzureBlob:{name}</c> where {name} is the value of the <paramref name="name" /> parameter.
        /// </summary>
        /// <typeparam name="TDep">A dependency used by the configure action.</typeparam>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="name">The name of the file system.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="name" /> is <c>null</c>.</exception>
        public static IUmbracoBuilder AddAzureBlobFileSystem<TDep>(this IUmbracoBuilder builder, string name, Action<AzureBlobFileSystemOptions, TDep> configure)
            where TDep : class
            => builder.AddInternal(name, optionsBuilder => optionsBuilder.Configure(configure));

        /// <summary>
        /// Registers a <see cref="IAzureBlobFileSystem" /> in the <see cref="IServiceCollection" />, with it's configuration
        /// loaded from <c>Umbraco:Storage:AzureBlob:{name}</c> where {name} is the value of the <paramref name="name" /> parameter.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="name">The name of the file system.</param>
        /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="name" /> is <c>null</c>.</exception>
        internal static IUmbracoBuilder AddInternal(this IUmbracoBuilder builder, string name, Action<OptionsBuilder<AzureBlobFileSystemOptions>>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(name);

            builder.Services.TryAddSingleton<IAzureBlobFileSystemProvider, AzureBlobFileSystemProvider>();

            var optionsBuilder = builder.Services.AddOptions<AzureBlobFileSystemOptions>(name)
                .BindConfiguration($"Umbraco:Storage:AzureBlob:{name}")
                .ValidateDataAnnotations();

            configure?.Invoke(optionsBuilder);

            return builder;
        }
    }
}
