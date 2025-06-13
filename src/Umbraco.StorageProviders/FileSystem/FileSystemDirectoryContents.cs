using System.Collections;
using Microsoft.Extensions.FileProviders;
using Umbraco.Cms.Core.IO;

namespace Umbraco.StorageProviders.FileSystem;

/// <summary>
/// Represents the directory contents in an <see cref="IFileSystem" />.
/// </summary>
/// <seealso cref="Microsoft.Extensions.FileProviders.IDirectoryContents" />
public class FileSystemDirectoryContents : IDirectoryContents
{
    private readonly IFileSystem _fileSystem;
    private readonly string _subpath;
    private IEnumerable<IFileInfo> _entries = null!;

    /// <inheritdoc />
    public bool Exists => true;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemDirectoryContents"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="subpath">The subpath.</param>
    /// <exception cref="System.ArgumentNullException">
    /// fileSystem
    /// or
    /// subpath
    /// </exception>
    public FileSystemDirectoryContents(IFileSystem fileSystem, string subpath)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _subpath = subpath ?? throw new ArgumentNullException(nameof(subpath));
    }

    /// <inheritdoc />
    public IEnumerator<IFileInfo> GetEnumerator()
    {
        EnsureInitialized();
        return _entries.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        EnsureInitialized();
        return _entries.GetEnumerator();
    }

    private void EnsureInitialized()
    {
        _entries = _fileSystem.GetDirectories(_subpath).Select<string, IFileInfo>(d => new FileSystemDirectoryInfo(_fileSystem, d))
            .Union(_fileSystem.GetFiles(_subpath).Select<string, IFileInfo>(f => new FileSystemFileInfo(_fileSystem, f)))
            .ToList();
    }
}
