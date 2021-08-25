using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Providers;
using SixLabors.ImageSharp.Web.Resolvers;
using Umbraco.Cms.Core.Hosting;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.StorageProviders.AzureBlob.Imaging
{
    /// <inheritdoc />
    public class AzureBlobImageProvider : IImageProvider
    {
        private readonly IAzureBlobFileSystemProvider _fileSystemProvider;
        private readonly FormatUtilities _formatUtilities;
        private readonly string _rootPath;

        /// <summary>
        /// Creates a new instance of <see cref="AzureBlobImageProvider"/>.
        /// </summary>
        /// <param name="optionsFactory"></param>
        /// <param name="fileSystemProvider"></param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="formatUtilities"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public AzureBlobImageProvider(IOptionsFactory<AzureBlobFileSystemOptions> optionsFactory,
            IAzureBlobFileSystemProvider fileSystemProvider, IHostingEnvironment hostingEnvironment,
            FormatUtilities formatUtilities)
        {
            if (optionsFactory == null) throw new ArgumentNullException(nameof(optionsFactory));
            if (hostingEnvironment == null) throw new ArgumentNullException(nameof(hostingEnvironment));

            _fileSystemProvider = fileSystemProvider ?? throw new ArgumentNullException(nameof(fileSystemProvider));
            _formatUtilities = formatUtilities ?? throw new ArgumentNullException(nameof(formatUtilities));

            var options = optionsFactory.Create(AzureBlobFileSystemOptions.MediaFileSystemName);
            _rootPath = hostingEnvironment.ToAbsolute(options.VirtualPath);
        }

        /// <inheritdoc />
        public bool IsValidRequest(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            return context.Request.Path.StartsWithSegments(_rootPath, StringComparison.InvariantCultureIgnoreCase)
                   && _formatUtilities.GetExtensionFromUri(context.Request.GetDisplayUrl()) != null;
        }

        /// <inheritdoc />
        public Task<IImageResolver?> GetAsync(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            return GetResolverAsync(context);
        }

        private async Task<IImageResolver?> GetResolverAsync(HttpContext context)
        {
            var blob = _fileSystemProvider
                .GetFileSystem(AzureBlobFileSystemOptions.MediaFileSystemName)
                .GetBlobClient(context.Request.Path);

            if (await blob.ExistsAsync().ConfigureAwait(false))
                return new AzureBlobImageResolver(blob);

            return null;
        }

        /// <inheritdoc />
        public ProcessingBehavior ProcessingBehavior => ProcessingBehavior.CommandOnly;

        /// <inheritdoc />
        public Func<HttpContext, bool> Match { get; set; } = _ => true;
    }
}
