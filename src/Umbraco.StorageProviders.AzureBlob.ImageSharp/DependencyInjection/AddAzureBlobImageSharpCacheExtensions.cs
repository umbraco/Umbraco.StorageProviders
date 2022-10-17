using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Web.Caching;
using Umbraco.Extensions;
using Umbraco.StorageProviders.AzureBlob.ImageSharp;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.Cms.Core.DependencyInjection;

/// <summary>
/// Extension methods to help registering Azure Blob Storage image caches for ImageSharp.
/// </summary>
public static class AddAzureBlobImageSharpCacheExtensions
{
    /// <summary>
    /// Registers an <see cref="IImageCache" /> configured using the specified <see cref="IAzureBlobFileSystem" />.
    /// </summary>
    /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
    /// <param name="name">The name of the file system.</param>
    /// <returns>
    /// The <see cref="IUmbracoBuilder" />.
    /// </returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
    public static IUmbracoBuilder AddAzureBlobImageSharpCache(this IUmbracoBuilder builder, string name = AzureBlobFileSystemOptions.MediaFileSystemName)
        => builder.AddInternal(name);

    /// <summary>
    /// Registers an <see cref="IImageCache" /> configured using the specified <see cref="IAzureBlobFileSystem" />.
    /// </summary>
    /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
    /// <param name="name">The name of the file system.</param>
    /// <returns>
    /// The <see cref="IUmbracoBuilder" />.
    /// </returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
    internal static IUmbracoBuilder AddInternal(this IUmbracoBuilder builder, string name = AzureBlobFileSystemOptions.MediaFileSystemName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddUnique<IImageCache>(provider => new AzureBlobFileSystemImageCache(name, provider.GetRequiredService<IOptionsMonitor<AzureBlobFileSystemOptions>>()));

        return builder;
    }
}
