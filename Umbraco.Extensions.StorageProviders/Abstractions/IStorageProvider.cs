using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Umbraco.Extensions.StorageProviders
{
    /// <summary>
    /// A read/write storage provider abstraction.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.FileProviders.IFileProvider" />
    public interface IStorageProvider : IFileProvider
    {
        /// <summary>
        /// Stores the file.
        /// </summary>
        /// <param name="subpath">The subpath.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="overwrite">if set to <c>true</c> [overwrite].</param>
        void StoreFile(string subpath, Stream stream, bool overwrite = false);

        /// <summary>
        /// Moves a specified file to a new location, providing the option to specify a new file name.
        /// </summary>
        /// <param name="sourceSubpath">The name of the file to move..</param>
        /// <param name="destinationSubpath">The name of the destination file. This cannot be a directory.</param>
        /// <param name="overwrite"><c>true</c> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
        void MoveFile(string sourceSubpath, string destinationSubpath, bool overwrite = false);

        /// <summary>
        /// Copies an existing file to a new file.
        /// </summary>
        /// <param name="sourceSubpath">The file to copy.</param>
        /// <param name="destinationSubpath">The name of the destination file. This cannot be a directory.</param>
        /// <param name="overwrite"><c>true</c> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
        void CopyFile(string sourceSubpath, string destinationSubpath, bool overwrite = false);

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="subpath">The name of the file to be deleted. Wildcard characters are not supported.</param>
        void DeleteFile(string subpath);

        /// <summary>
        /// Deletes the specified directory and, if indicated, any subdirectories and files in the directory.
        /// </summary>
        /// <param name="subpath">The name of the directory to remove.</param>
        /// <param name="recursive"><c>true</c> to remove directories, subdirectories, and files in path; otherwise, <c>false</c>.</param>
        void DeleteDirectory(string subpath, bool recursive = false);
    }
}
