using Umbraco.Cms.Core.IO;

namespace Umbraco.StorageProviders;

/// <summary>
/// Exposes an <see cref="IFileSystem" /> as an <see cref="IStorageProvider" />.
/// </summary>
/// <seealso cref="Umbraco.StorageProviders.FileSystemFileProvider" />
/// <seealso cref="Umbraco.StorageProviders.IStorageProvider" />
public class FileSystemStorageProvider : FileSystemFileProvider, IStorageProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemStorageProvider" /> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="pathPrefix">The path prefix.</param>
    public FileSystemStorageProvider(IFileSystem fileSystem, string? pathPrefix = null)
        : base(fileSystem, pathPrefix)
    { }

    ///// <inheritdoc />
    //public void CopyFile(string sourceSubpath, string destinationSubpath, bool overwrite = false)
    //{
    //    var sourcePath = PathPrefix + sourceSubpath;
    //    var destinationPath = PathPrefix + destinationSubpath;
    //    if (sourcePath != null && destinationPath != null)
    //    {
    //        using var stream = FileSystem.OpenFile(sourcePath);
    //        FileSystem.AddFile(destinationPath, stream, overwrite);
    //    }
    //}

    ///// <inheritdoc />
    //public void DeleteDirectory(string subpath, bool recursive = false)
    //{
    //    var path = PathPrefix + subpath;
    //    if (path != null)
    //    {
    //        FileSystem.DeleteDirectory(path, recursive);
    //    }
    //}

    ///// <inheritdoc />
    //public void DeleteFile(string subpath)
    //{
    //    var path = PathPrefix + subpath;
    //    if (path != null)
    //    {
    //        FileSystem.DeleteFile(path);
    //    }
    //}

    ///// <inheritdoc />
    //public void MoveFile(string sourceSubpath, string destinationSubpath, bool overwrite = false)
    //{
    //    CopyFile(sourceSubpath, destinationSubpath, overwrite);
    //    DeleteFile(sourceSubpath);
    //}

    ///// <inheritdoc />
    //public void StoreFile(string subpath, Stream stream, bool overwrite = false)
    //{
    //    var path = PathPrefix + subpath;
    //    if (path != null)
    //    {
    //        FileSystem.AddFile(path, stream, overwrite);
    //    }
    //}

    /// <inheritdoc />
    public Stream? CreateWriteStream(string subpath, bool overwrite = false) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<bool> MoveAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<bool> MoveDirectoryAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, bool recursive = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<bool> CopyAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<bool> CopyDirectoryAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, bool recursive = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string subpath, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<bool> DeleteDirectoryAsync(string subpath, bool recursive = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
