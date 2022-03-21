using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;

namespace Umbraco.StorageProviders.AzureBlob.IO
{
    /// <inheritdoc />
    public class AzureBlobFileSystemProvider : IAzureBlobFileSystemProvider
    {
        private readonly ConcurrentDictionary<string, IAzureBlobFileSystem> _fileSystems = new();
        private readonly IOptionsMonitor<AzureBlobFileSystemOptions> _optionsMonitor;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IIOHelper _ioHelper;
        private readonly FileExtensionContentTypeProvider _fileExtensionContentTypeProvider;

        /// <summary>
        /// Creates a new instance of <see cref="AzureBlobFileSystemProvider" />.
        /// </summary>
        /// <param name="optionsMonitor">The options monitor.</param>
        /// <param name="hostingEnvironment">The hosting environment.</param>
        /// <param name="ioHelper">The IO helper.</param>
        /// <exception cref="System.ArgumentNullException">optionsMonitor
        /// or
        /// hostingEnvironment
        /// or
        /// ioHelper</exception>
        public AzureBlobFileSystemProvider(IOptionsMonitor<AzureBlobFileSystemOptions> optionsMonitor, IHostingEnvironment hostingEnvironment, IIOHelper ioHelper)
        {
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            _ioHelper = ioHelper ?? throw new ArgumentNullException(nameof(ioHelper));

            _fileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();
            _optionsMonitor.OnChange(OptionsOnChange);
        }

        /// <inheritdoc />
        public IAzureBlobFileSystem GetFileSystem(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            return _fileSystems.GetOrAdd(name, name =>
            {
                var options = _optionsMonitor.Get(name);

                return new AzureBlobFileSystem(options, _hostingEnvironment, _ioHelper, _fileExtensionContentTypeProvider);
            });
        }

        private void OptionsOnChange(AzureBlobFileSystemOptions options, string name)
        {
            _fileSystems.TryRemove(name, out _);
        }
    }
}
