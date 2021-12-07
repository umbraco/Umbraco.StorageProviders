using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Providers;
using SixLabors.ImageSharp.Web.Resolvers;
using SixLabors.ImageSharp.Web.Resolvers.Azure;
using Umbraco.Cms.Core.Hosting;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.StorageProviders.AzureBlob.Imaging
{
    /// <inheritdoc />
    public class AzureBlobFileSystemImageProvider : IImageProvider
    {
        private readonly string _name;
        private readonly IAzureBlobFileSystemProvider _fileSystemProvider;
        private string _rootPath;
        private readonly FormatUtilities _formatUtilities;

        /// <summary>
        /// A match function used by the resolver to identify itself as the correct resolver to use.
        /// </summary>
        private Func<HttpContext, bool>? _match;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFileSystemImageProvider" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="fileSystemProvider">The file system provider.</param>
        /// <param name="hostingEnvironment">The hosting environment.</param>
        /// <param name="formatUtilities">The format utilities.</param>
        public AzureBlobFileSystemImageProvider(IOptionsMonitor<AzureBlobFileSystemOptions> options, IAzureBlobFileSystemProvider fileSystemProvider, IHostingEnvironment hostingEnvironment, FormatUtilities formatUtilities)
            : this(AzureBlobFileSystemOptions.MediaFileSystemName, options, fileSystemProvider, hostingEnvironment, formatUtilities)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="AzureBlobFileSystemImageProvider" />.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <param name="fileSystemProvider">The file system provider.</param>
        /// <param name="hostingEnvironment">The hosting environment.</param>
        /// <param name="formatUtilities">The format utilities.</param>
        /// <exception cref="System.ArgumentNullException">optionsFactory
        /// or
        /// hostingEnvironment
        /// or
        /// name
        /// or
        /// fileSystemProvider
        /// or
        /// formatUtilities</exception>
        protected AzureBlobFileSystemImageProvider(string name, IOptionsMonitor<AzureBlobFileSystemOptions> options, IAzureBlobFileSystemProvider fileSystemProvider, IHostingEnvironment hostingEnvironment, FormatUtilities formatUtilities)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (hostingEnvironment == null) throw new ArgumentNullException(nameof(hostingEnvironment));

            _name = name ?? throw new ArgumentNullException(nameof(name));
            _fileSystemProvider = fileSystemProvider ?? throw new ArgumentNullException(nameof(fileSystemProvider));
            _formatUtilities = formatUtilities ?? throw new ArgumentNullException(nameof(formatUtilities));

            var fileSystemOptions = options.Get(name);
            _rootPath = hostingEnvironment.ToAbsolute(fileSystemOptions.VirtualPath);

            options.OnChange((o, n) => OptionsOnChange(o, n, hostingEnvironment));
        }

        /// <inheritdoc />
        public bool IsValidRequest(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            return _formatUtilities.GetExtensionFromUri(context.Request.GetDisplayUrl()) != null;
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
                .GetFileSystem(_name)
                .GetBlobClient(context.Request.Path);

            if (await blob.ExistsAsync().ConfigureAwait(false))
                return new AzureBlobStorageImageResolver(blob);

            return null;
        }

        /// <inheritdoc />
        public ProcessingBehavior ProcessingBehavior => ProcessingBehavior.CommandOnly;

        /// <inheritdoc/>
        public Func<HttpContext, bool> Match
        {
            get => this._match ?? IsMatch;
            set => this._match = value;
        }

        private bool IsMatch(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            return context.Request.Path.StartsWithSegments(_rootPath, StringComparison.InvariantCultureIgnoreCase);
        }

        private void OptionsOnChange(AzureBlobFileSystemOptions options, string name, IHostingEnvironment hostingEnvironment)
        {
            if (name != _name) return;

            _rootPath = hostingEnvironment.ToAbsolute(options.VirtualPath);
        }
    }
}
