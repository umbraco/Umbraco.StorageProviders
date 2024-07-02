using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Infrastructure.DependencyInjection;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.Cms.Core.DependencyInjection;

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
    /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
    public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder)
        => builder.AddInternal();

    /// <summary>
    /// Registers a <see cref="IAzureBlobFileSystem" /> and it's dependencies configured for media.
    /// </summary>
    /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
    /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
    /// <returns>
    /// The <see cref="IUmbracoBuilder" />.
    /// </returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
    public static IUmbracoBuilder AddAzureBlobMediaFileSystem(this IUmbracoBuilder builder, Action<AzureBlobFileSystemOptions> configure)
        => builder.AddInternal(x => x.Configure(configure));

    /// <summary>
    /// Registers a <see cref="IAzureBlobFileSystem" /> and it's dependencies configured for media.
    /// </summary>
    /// <typeparam name="TDep">A dependency used by the configure action.</typeparam>
    /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
    /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
    /// <returns>
    /// The <see cref="IUmbracoBuilder" />.
    /// </returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
    public static IUmbracoBuilder AddAzureBlobFileSystem<TDep>(this IUmbracoBuilder builder, Action<AzureBlobFileSystemOptions, TDep> configure)
        where TDep : class
        => builder.AddInternal(x => x.Configure(configure));

    /// <summary>
    /// Registers a <see cref="IAzureBlobFileSystem" /> and it's dependencies configured for media.
    /// </summary>
    /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
    /// <param name="configure">An action used to configure the <see cref="AzureBlobFileSystemOptions" />.</param>
    /// <returns>
    /// The <see cref="IUmbracoBuilder" />.
    /// </returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
    internal static IUmbracoBuilder AddInternal(this IUmbracoBuilder builder, Action<OptionsBuilder<AzureBlobFileSystemOptions>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddInternal(AzureBlobFileSystemOptions.MediaFileSystemName, optionsBuilder =>
        {
            optionsBuilder.Configure<IOptions<GlobalSettings>>((options, globalSettings) => options.VirtualPath = globalSettings.Value.UmbracoMediaPath);
            configure?.Invoke(optionsBuilder);
        });

        // Note: this instance will be reused via the closure below.
        AzureBlobFileSystemOptions config = builder.Config.GetRequiredSection(AzureBlobFileSystemExtensions.UmbracoStorageAzureBlobMediaConfigurationKey(AzureBlobFileSystemOptions.MediaFileSystemName))
            .Get<AzureBlobFileSystemOptions>() ?? throw new InvalidOperationException($"Configuration section Umbraco:Storage:AzureBlob:{AzureBlobFileSystemOptions.MediaFileSystemName} must have a value");

        // Register the BlobContainerClient as a scoped service. Uses the `Try` method, so if there is a previously registered service, it will not be replaced.
        builder.Services.TryAddScoped(_ => new BlobContainerClient(config.ConnectionString, config.ContainerName));

        builder.SetMediaFileSystem(provider => provider.GetRequiredService<IAzureBlobFileSystemProvider>().GetFileSystem(AzureBlobFileSystemOptions.MediaFileSystemName));

        return builder;
    }
}
