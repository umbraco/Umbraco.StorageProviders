using Microsoft.Extensions.FileProviders;

namespace Umbraco.StorageProviders;

/// <summary>
/// A read/write storage provider abstraction.
/// </summary>
/// <seealso cref="Microsoft.Extensions.FileProviders.IFileProvider" />
public interface IStorageProvider : IFileProvider
{
    /// <summary>
    /// Create a writable stream to store file contents. Caller should dispose the stream when complete.
    /// </summary>
    /// <param name="subpath">The path/name of the file.</param>
    /// <param name="overwrite"><c>true</c> if the file can be overwritten; otherwise, <c>false</c>.</param>
    /// <returns>
    /// The file stream.
    /// </returns>
    Stream? CreateWriteStream(string subpath, bool overwrite = false);

    /// <summary>
    /// Moves the specified file to a new location, providing the option to specify a new file name.
    /// </summary>
    /// <param name="sourceSubpath">The path/name of the source file.</param>
    /// <param name="destinationSubpath">The path/name of the destination file. This cannot be a directory.</param>
    /// <param name="overwrite"><c>true</c> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns a value indicating whether the operation succeeded.
    /// </returns>
    Task<bool> MoveAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves the specified directory to a new location.
    /// </summary>
    /// <param name="sourceSubpath">The path/name of the source directory.</param>
    /// <param name="destinationSubpath">The path/name of the destination directory.</param>
    /// <param name="overwrite"><c>true</c> if files in the destination directory can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="recursive"><c>true</c> to recursively move subdirectories and files; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns a value indicating whether the operation succeeded.
    /// </returns>
    Task<bool> MoveDirectoryAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, bool recursive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies an existing file to a new file, providing the option to specify a new file name.
    /// </summary>
    /// <param name="sourceSubpath">The path/name of the source directory.</param>
    /// <param name="destinationSubpath">The path/name of the destination file. This cannot be a directory.</param>
    /// <param name="overwrite"><c>true</c> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns a value indicating whether the operation succeeded.
    /// </returns>
    Task<bool> CopyAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies an existing directory to a new directory.
    /// </summary>
    /// <param name="sourceSubpath">The path/name of the source directory.</param>
    /// <param name="destinationSubpath">The path/name of the destination directory.</param>
    /// <param name="overwrite"><c>true</c> if files in the destination directory can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="recursive"><c>true</c> to recursively copy subdirectories and files; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns a value indicating whether the operation succeeded.
    /// </returns>
    Task<bool> CopyDirectoryAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, bool recursive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified file.
    /// </summary>
    /// <param name="subpath">The path/name of the file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns a value indicating whether the operation succeeded.
    /// </returns>
    Task<bool> DeleteAsync(string subpath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified directory.
    /// </summary>
    /// <param name="subpath">The path/name of the directory.</param>
    /// <param name="recursive"><c>true</c> to recursively remove subdirectories and files; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns a value indicating whether the operation succeeded.
    /// </returns>
    Task<bool> DeleteDirectoryAsync(string subpath, bool recursive = false, CancellationToken cancellationToken = default);
}
