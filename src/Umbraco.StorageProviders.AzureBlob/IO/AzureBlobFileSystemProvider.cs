using System.Collections.Concurrent;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;

namespace Umbraco.StorageProviders.AzureBlob.IO;

/// <inheritdoc />
public sealed class AzureBlobFileSystemProvider : IAzureBlobFileSystemProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, IAzureBlobFileSystem> _fileSystems = new();
    private readonly IOptionsMonitor<AzureBlobFileSystemOptions> _optionsMonitor;
    private readonly IHostingEnvironment _hostingEnvironment;
    private readonly IIOHelper _ioHelper;
    private readonly FileExtensionContentTypeProvider _fileExtensionContentTypeProvider;
    private readonly IDisposable? _onChangeRegistration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobFileSystemProvider"/> class.
    /// </summary>
    /// <param name="optionsMonitor">The options monitor.</param>
    /// <param name="hostingEnvironment">The hosting environment.</param>
    /// <param name="ioHelper">The IO helper.</param>
    /// <exception cref="ArgumentNullException"><paramref name="optionsMonitor"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="hostingEnvironment"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="ioHelper"/> is <c>null</c>.</exception>
    public AzureBlobFileSystemProvider(IOptionsMonitor<AzureBlobFileSystemOptions> optionsMonitor, IHostingEnvironment hostingEnvironment, IIOHelper ioHelper)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
        _ioHelper = ioHelper ?? throw new ArgumentNullException(nameof(ioHelper));
        _fileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();

        _onChangeRegistration = _optionsMonitor.OnChange((options, name) =>
        {
            if (_fileSystems.TryRemove(name ?? Options.DefaultName, out IAzureBlobFileSystem? removed) && removed is IDisposable disposable)
            {
                disposable.Dispose();
            }
        });
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
    public IAzureBlobFileSystem GetFileSystem(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _fileSystems.GetOrAdd(name, name =>
        {
            AzureBlobFileSystemOptions options = _optionsMonitor.Get(name);

            return new AzureBlobFileSystem(options, _hostingEnvironment, _ioHelper, _fileExtensionContentTypeProvider);
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _onChangeRegistration?.Dispose();

        foreach (IAzureBlobFileSystem fileSystem in _fileSystems.Values)
        {
            if (fileSystem is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _fileSystems.Clear();
    }
}
