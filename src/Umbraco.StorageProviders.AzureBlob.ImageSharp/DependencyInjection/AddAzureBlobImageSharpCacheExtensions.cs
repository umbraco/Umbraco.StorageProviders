using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp.Web.Caching;
using Umbraco.Extensions;
using Umbraco.StorageProviders.AzureBlob.ImageSharp;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.Cms.Core.DependencyInjection;

/// <summary>
/// Extension methods to help register an Azure Blob Storage image cache for ImageSharp.
/// </summary>
public static class AddAzureBlobImageSharpCacheExtensions
{
    private const string ContainerRootPath = "cache";

    /// <summary>
    /// Registers an <see cref="IImageCache" /> configured using the <see cref="AzureBlobFileSystemOptions" /> for media and <see cref="ContainerRootPath" /> container root path.
    /// </summary>
    /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
    /// <returns>
    /// The <see cref="IUmbracoBuilder" />.
    /// </returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
    public static IUmbracoBuilder AddAzureBlobImageSharpCache(this IUmbracoBuilder builder)
        => builder.AddInternal(AzureBlobFileSystemOptions.MediaFileSystemName, ContainerRootPath);

    /// <summary>
    /// Registers an <see cref="IImageCache" /> configured using the specified <see cref="AzureBlobFileSystemOptions" />.
    /// </summary>
    /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
    /// <param name="name">The name of the file system.</param>
    /// <param name="containerRootPath">The container root path (will use <see cref="AzureBlobFileSystemOptions.ContainerRootPath" /> if <c>null</c>).</param>
    /// <returns>
    /// The <see cref="IUmbracoBuilder" />.
    /// </returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
    public static IUmbracoBuilder AddAzureBlobImageSharpCache(this IUmbracoBuilder builder, string name, string? containerRootPath = null)
        => builder.AddInternal(name, containerRootPath);

    /// <summary>
    /// Registers an <see cref="IImageCache" /> configured using the specified <see cref="AzureBlobFileSystemOptions" />.
    /// </summary>
    /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
    /// <param name="name">The name of the file system.</param>
    /// <param name="containerRootPath">The container root path (will use <see cref="AzureBlobFileSystemOptions.ContainerRootPath" /> if <c>null</c>).</param>
    /// <returns>
    /// The <see cref="IUmbracoBuilder" />.
    /// </returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
    internal static IUmbracoBuilder AddInternal(this IUmbracoBuilder builder, string name, string? containerRootPath)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddUnique<IImageCache>(provider => new AzureBlobFileSystemImageCache(provider.GetRequiredService<BlobContainerClient>(), containerRootPath));

        return builder;
    }
}
