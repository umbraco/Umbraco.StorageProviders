using Microsoft.Extensions.FileProviders;
using Umbraco.Cms.Core.IO;

namespace Umbraco.StorageProviders.FileSystem;

/// <summary>
/// Represents a file in an <see cref="IFileSystem" />.
/// </summary>
/// <seealso cref="Microsoft.Extensions.FileProviders.IFileInfo" />
public class FileSystemFileInfo : IFileInfo
{
    private readonly IFileSystem _fileSystem;
    private readonly string _subpath;

    /// <inheritdoc />
    public bool Exists => true;

    /// <inheritdoc />
    public bool IsDirectory => false;

    /// <inheritdoc />
    public DateTimeOffset LastModified => _fileSystem.GetLastModified(_subpath);

    /// <inheritdoc />
    public long Length => _fileSystem.GetSize(_subpath);

    /// <inheritdoc />
    public string Name => _fileSystem.GetRelativePath(_subpath);

    /// <inheritdoc />
    public string PhysicalPath => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemFileInfo" /> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="subpath">The subpath.</param>
    public FileSystemFileInfo(IFileSystem fileSystem, string subpath)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _subpath = subpath ?? throw new ArgumentNullException(nameof(subpath));
    }

    /// <inheritdoc />
    public Stream CreateReadStream() => _fileSystem.OpenFile(_subpath);
}
