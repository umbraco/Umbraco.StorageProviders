using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Umbraco.StorageProviders;

/// <summary>
/// Provides a generic implementation for most storage provider actions.
/// </summary>
/// <remarks>
/// The default implementation where moving is based on copy/delete and copying requires reading/writing the file contents.
/// </remarks>
/// <seealso cref="Umbraco.StorageProviders.IStorageProvider" />
public abstract class StorageProviderBase : IStorageProvider
{
    /// <inheritdoc />
    public abstract IDirectoryContents GetDirectoryContents(string subpath);

    /// <inheritdoc />
    public abstract IFileInfo GetFileInfo(string subpath);

    /// <inheritdoc />
    public virtual IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    /// <inheritdoc />
    public abstract Stream CreateWriteStream(string subpath, bool overwrite = false);

    /// <inheritdoc />
    public virtual async Task<bool> MoveAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, CancellationToken cancellationToken = default)
        => await CopyAsync(sourceSubpath, destinationSubpath, overwrite, cancellationToken).ConfigureAwait(false) &&
            await DeleteAsync(sourceSubpath, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public virtual async Task<bool> MoveDirectoryAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, bool recursive = false, CancellationToken cancellationToken = default)
    {
        var sourceDirectoryContents = GetDirectoryContents(sourceSubpath);
        if (sourceDirectoryContents is null || sourceDirectoryContents.Exists is false)
        {
            return false;
        }

        foreach (var sourceFileInfo in sourceDirectoryContents)
        {
            var sourcePath = CombinePath(sourceSubpath, sourceFileInfo.Name);
            var destinationPath = CombinePath(destinationSubpath, sourceFileInfo.Name);

            if (sourceFileInfo.IsDirectory is false)
            {
                await MoveAsync(sourcePath, destinationPath, overwrite, cancellationToken).ConfigureAwait(false);
            }
            else if (recursive)
            {
                await MoveDirectoryAsync(sourcePath, destinationPath, overwrite, recursive, cancellationToken).ConfigureAwait(false);
            }
        }

        return true;
    }

    /// <inheritdoc />
    public virtual async Task<bool> CopyAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, CancellationToken cancellationToken = default)
        => await CopyAsync(GetFileInfo(sourceSubpath), destinationSubpath, overwrite, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    protected virtual async Task<bool> CopyAsync(IFileInfo sourceFileInfo, string destinationSubpath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        if (sourceFileInfo is null || sourceFileInfo.Exists is false || sourceFileInfo.IsDirectory)
        {
            return false;
        }

        using (var source = sourceFileInfo.CreateReadStream())
        using (var destination = CreateWriteStream(destinationSubpath, overwrite))
        {
            await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <inheritdoc />
    public virtual async Task<bool> CopyDirectoryAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, bool recursive = false, CancellationToken cancellationToken = default)
    {
        var sourceDirectoryContents = GetDirectoryContents(sourceSubpath);
        if (sourceDirectoryContents is null || sourceDirectoryContents.Exists is false)
        {
            return false;
        }

        foreach (var sourceFileInfo in sourceDirectoryContents)
        {
            var destinationPath = CombinePath(destinationSubpath, sourceFileInfo.Name);

            if (sourceFileInfo.IsDirectory is false)
            {
                await CopyAsync(sourceFileInfo, destinationPath, overwrite, cancellationToken).ConfigureAwait(false);
            }
            else if (recursive)
            {
                var sourcePath = CombinePath(sourceSubpath, sourceFileInfo.Name);

                await CopyDirectoryAsync(sourcePath, destinationPath, overwrite, recursive, cancellationToken).ConfigureAwait(false);
            }
        }

        return true;
    }

    /// <inheritdoc />
    public abstract Task<bool> DeleteAsync(string subpath, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async Task<bool> DeleteDirectoryAsync(string subpath, bool recursive = false, CancellationToken cancellationToken = default)
    {
        var directoryContents = GetDirectoryContents(subpath);
        if (directoryContents is null || directoryContents.Exists is false)
        {
            return false;
        }

        foreach (var fileInfo in directoryContents)
        {
            var path = CombinePath(subpath, fileInfo.Name);

            if (fileInfo.IsDirectory is false)
            {
                await DeleteAsync(path, cancellationToken).ConfigureAwait(false);
            }
            else if (recursive)
            {
                await DeleteDirectoryAsync(path, recursive, cancellationToken).ConfigureAwait(false);
            }
        }

        return true;
    }

    /// <summary>
    /// Combines the path and file name.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="fileName">The file name.</param>
    /// <returns>
    /// The combined path and file name.
    /// </returns>
    protected virtual string CombinePath(string path, string fileName) => Path.Combine(path, fileName);
}
