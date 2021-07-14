using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Umbraco.Extensions;

namespace Umbraco.StorageProviders.AzureBlob
{
    /// <inheritdoc />
    public class DefaultAzureBlobFileSystemProvider : IAzureBlobFileSystemProvider
    {
        private readonly ConcurrentDictionary<string, IAzureBlobFileSystem> _fileSystems = new();
        private readonly IOptionsMonitor<AzureBlobFileSystemOptions> _optionsMonitor;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IIOHelper _ioHelper;
        private readonly FileExtensionContentTypeProvider _fileExtensionContentTypeProvider;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultAzureBlobFileSystemProvider"/>.
        /// </summary>
        /// <param name="optionsMonitor"></param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="ioHelper"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public DefaultAzureBlobFileSystemProvider(IOptionsMonitor<AzureBlobFileSystemOptions> optionsMonitor,
            IHostingEnvironment hostingEnvironment, IIOHelper ioHelper)
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

            return _fileSystems.GetOrAdd(name, CreateInstance);
        }

        private IAzureBlobFileSystem CreateInstance(string name)
        {
            var options = _optionsMonitor.Get(name);

            return CreateInstance(options);
        }

        private IAzureBlobFileSystem CreateInstance(AzureBlobFileSystemOptions options)
        {
            return new AzureBlobFileSystem(options, _hostingEnvironment, _ioHelper, _fileExtensionContentTypeProvider);
        }

        private void OptionsOnChange(AzureBlobFileSystemOptions options, string name)
        {
            _fileSystems.TryUpdate(name, _ => CreateInstance(options));
        }
    }
}
