using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Umbraco.StorageProviders.Internal;

namespace Umbraco.StorageProviders
{
    /// <summary>
    /// Storage provider for the on-disk file system.
    /// </summary>
    /// <seealso cref="Umbraco.StorageProviders.IStorageProvider" />
    /// <seealso cref="Microsoft.Extensions.FileProviders.PhysicalFileProvider" />
    public class PhysicalStorageProvider : PhysicalFileProvider, IStorageProvider
    {
        private static readonly char[] _pathSeparators = new[]
        {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="PhysicalStorageProvider" /> class.
        /// </summary>
        /// <param name="root">The root directory. This should be an absolute path.</param>
        public PhysicalStorageProvider(string root)
            : base(root)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhysicalStorageProvider" /> class.
        /// </summary>
        /// <param name="root">The root directory. This should be an absolute path.</param>
        /// <param name="filters">Specifies which files or directories are excluded.</param>
        public PhysicalStorageProvider(string root, ExclusionFilters filters)
            : base(root, filters)
        { }

        /// <inheritdoc />
        public Stream? CreateWriteStream(string subpath, bool overwrite = false)
        {
            if (TryGetFullPath(subpath, out string fullPath))
            {
                EnsureDirectory(fullPath);

                return File.Open(fullPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }

            return null;
        }

        /// <inheritdoc />
        public Task<bool> MoveAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            var fileInfo = GetFileInfo(sourceSubpath);
            if (fileInfo.Exists &&
                fileInfo.PhysicalPath is string sourceFullPath &&
                TryGetFullPath(destinationSubpath, out string destinationFullPath))
            {
                EnsureDirectory(destinationFullPath);

                File.Move(sourceFullPath, destinationFullPath, overwrite);

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> MoveDirectoryAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, bool recursive = false, CancellationToken cancellationToken = default)
        {
            var directoryContents = GetDirectoryContents(sourceSubpath);
            if (directoryContents.Exists &&
                TryGetFullPath(sourceSubpath, out string sourceFullPath) &&
                TryGetFullPath(destinationSubpath, out string destinationFullPath))
            {
                EnsureDirectory(destinationFullPath);

                bool deleteSourceDirectory = true;
                foreach (var fileInfo in directoryContents)
                {
                    if (fileInfo.PhysicalPath is string sourceFilePath)
                    {
                        var relativeSourcePath = Path.GetRelativePath(sourceFullPath, sourceFilePath);
                        var destinationFilePath = Path.Combine(destinationFullPath, );

                        if (!fileInfo.IsDirectory)
                        {
                            File.Move(sourceFilePath, destinationFilePath, overwrite);
                        }
                        else if (recursive)
                        {
                            MoveDirectoryAsync(sourceFilePath, destinationFilePath)
                        }
                        else
                        {
                            // We have subdirectories, but aren't recursively moving them, so can't delete source directory
                            deleteSourceDirectory = false;
                        }
                    }


                }

                var directoryInfo = new DirectoryInfo(sourceFullPath);
                if (directoryInfo.Exists)
                {
                    EnsureDirectory(destinationSubpath);

                    // Move files
                    foreach (var fileInfo in directoryInfo.EnumerateFiles())
                    {
                        var destinationFilePath = Path.Combine(destinationFullPath, Path.GetRelativePath(sourceFullPath, fileInfo.FullName));

                        fileInfo.MoveTo(destinationFilePath, overwrite);
                    }

                    if (recursive)
                    {
                        foreach (var subDirectoryInfo in directoryInfo.EnumerateDirectories())
                        {

                        }
                    }

                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> CopyAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            if (TryGetFullPath(sourceSubpath, out string sourceFullPath) &&
                TryGetFullPath(destinationSubpath, out string destinationFullPath))
            {
                var fileInfo = new FileInfo(sourceFullPath);
                if (fileInfo.Exists)
                {
                    EnsureDirectory(destinationSubpath);

                    fileInfo.CopyTo(destinationFullPath, overwrite);

                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> CopyDirectoryAsync(string sourceSubpath, string destinationSubpath, bool overwrite = false, bool recursive = false, CancellationToken cancellationToken = default)
        {
            if (TryGetFullPath(sourceSubpath, out string sourceFullPath) &&
                TryGetFullPath(destinationSubpath, out string destinationFullPath))
            {
                var directoryInfo = new DirectoryInfo(sourceFullPath);
                if (directoryInfo.Exists)
                {
                    

                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> DeleteAsync(string subpath, CancellationToken cancellationToken = default)
        {
            if (TryGetFullPath(subpath, out string fullPath))
            {
                var fileInfo = new FileInfo(subpath);
                if (fileInfo.Exists)
                {
                    fileInfo.Delete();

                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> DeleteDirectoryAsync(string subpath, bool recursive = false, CancellationToken cancellationToken = default)
        {
            if (TryGetFullPath(subpath, out string fullPath))
            {
                var directoryInfo = new DirectoryInfo(subpath);
                if (directoryInfo.Exists)
                {
                    directoryInfo.Delete(recursive);

                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        private bool TryGetFullPath(string subpath, out string fullPath)
        {
            if (string.IsNullOrEmpty(subpath) || PathUtils.HasInvalidPathChars(subpath))
            {
                fullPath = null!;
                return false;
            }

            // Relative paths starting with leading slashes are okay
            subpath = subpath.TrimStart(_pathSeparators);

            // Absolute paths not permitted
            if (Path.IsPathRooted(subpath))
            {
                fullPath = null!;
                return false;
            }

            if (PathUtils.PathNavigatesAboveRoot(subpath))
            {
                fullPath = null!;
                return false;
            }

            try
            {
                fullPath = Path.GetFullPath(Path.Combine(Root, subpath));
            }
            catch
            {
                fullPath = null!;
                return false;
            }

            if (!fullPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static void EnsureDirectory(string fullPath)
        {
            var directoryName = Path.GetDirectoryName(fullPath);
            if (directoryName is not null && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }
    }

    //private class Temp
    //{
    //    private static readonly char[] _pathSeparators = new[]
    //    {
    //        Path.DirectorySeparatorChar,
    //        Path.AltDirectorySeparatorChar
    //    };

    //    /// <inheritdoc />
    //    public void CopyFile(string sourceSubpath, string destinationSubpath, bool overwrite = false)
    //    {
    //        if (string.IsNullOrEmpty(sourceSubpath) || PathUtils.HasInvalidPathChars(sourceSubpath) ||
    //            string.IsNullOrEmpty(destinationSubpath) || PathUtils.HasInvalidPathChars(destinationSubpath))
    //        {
    //            return;
    //        }

    //        // Relative paths starting with leading slashes are okay
    //        sourceSubpath = sourceSubpath.TrimStart(_pathSeparators);
    //        destinationSubpath = destinationSubpath.TrimStart(_pathSeparators);

    //        // Absolute paths not permitted.
    //        if (Path.IsPathRooted(sourceSubpath) || Path.IsPathRooted(destinationSubpath))
    //        {
    //            return;
    //        }

    //        string? sourceFullPath = GetFullPath(sourceSubpath);
    //        if (sourceFullPath == null)
    //        {
    //            return;
    //        }

    //        string? destinationFullPath = GetFullPath(destinationSubpath);
    //        if (destinationFullPath == null)
    //        {
    //            return;
    //        }

    //        File.Copy(sourceFullPath, destinationFullPath, overwrite);
    //    }

    //    /// <inheritdoc />
    //    public void DeleteDirectory(string subpath, bool recursive = false)
    //    {
    //        if (subpath == null || PathUtils.HasInvalidPathChars(subpath))
    //        {
    //            return;
    //        }

    //        // Relative paths starting with leading slashes are okay
    //        subpath = subpath.TrimStart(_pathSeparators);

    //        // Absolute paths not permitted.
    //        if (Path.IsPathRooted(subpath))
    //        {
    //            return;
    //        }

    //        string? fullPath = GetFullPath(subpath);
    //        if (fullPath == null || !Directory.Exists(fullPath))
    //        {
    //            return;
    //        }

    //        Directory.Delete(fullPath, recursive);
    //    }

    //    /// <inheritdoc />
    //    public void DeleteFile(string subpath)
    //    {
    //        if (string.IsNullOrEmpty(subpath) || PathUtils.HasInvalidPathChars(subpath))
    //        {
    //            return;
    //        }

    //        // Relative paths starting with leading slashes are okay
    //        subpath = subpath.TrimStart(_pathSeparators);

    //        // Absolute paths not permitted.
    //        if (Path.IsPathRooted(subpath))
    //        {
    //            return;
    //        }

    //        string? fullPath = GetFullPath(subpath);
    //        if (fullPath == null || !Directory.Exists(fullPath))
    //        {
    //            return;
    //        }

    //        File.Delete(fullPath);
    //    }

    //    /// <inheritdoc />
    //    public void MoveFile(string sourceSubpath, string destinationSubpath, bool overwrite = false)
    //    {
    //        if (string.IsNullOrEmpty(sourceSubpath) || PathUtils.HasInvalidPathChars(sourceSubpath) ||
    //            string.IsNullOrEmpty(destinationSubpath) || PathUtils.HasInvalidPathChars(destinationSubpath))
    //        {
    //            return;
    //        }

    //        // Relative paths starting with leading slashes are okay
    //        sourceSubpath = sourceSubpath.TrimStart(_pathSeparators);
    //        destinationSubpath = destinationSubpath.TrimStart(_pathSeparators);

    //        // Absolute paths not permitted.
    //        if (Path.IsPathRooted(sourceSubpath) || Path.IsPathRooted(destinationSubpath))
    //        {
    //            return;
    //        }

    //        string? sourceFullPath = GetFullPath(sourceSubpath);
    //        if (sourceFullPath == null)
    //        {
    //            return;
    //        }

    //        string? destinationFullPath = GetFullPath(destinationSubpath);
    //        if (destinationFullPath == null)
    //        {
    //            return;
    //        }

    //        File.Move(sourceFullPath, destinationFullPath, overwrite);
    //    }

    //    /// <inheritdoc />
    //    public void StoreFile(string subpath, Stream stream, bool overwrite = false)
    //    {
    //        if (string.IsNullOrEmpty(subpath) || PathUtils.HasInvalidPathChars(subpath) || stream is null)
    //        {
    //            return;
    //        }

    //        // Relative paths starting with leading slashes are okay
    //        subpath = subpath.TrimStart(_pathSeparators);

    //        // Absolute paths not permitted.
    //        if (Path.IsPathRooted(subpath))
    //        {
    //            return;
    //        }

    //        string? fullPath = GetFullPath(subpath);
    //        if (fullPath == null || !Directory.Exists(fullPath))
    //        {
    //            return;
    //        }

    //        using var fileStream = new FileStream(fullPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write);
    //        stream.CopyTo(fileStream);
    //    }

    //    private string? GetFullPath(string path)
    //    {
    //        if (PathUtils.PathNavigatesAboveRoot(path))
    //        {
    //            return null;
    //        }

    //        string fullPath;
    //        try
    //        {
    //            fullPath = Path.GetFullPath(Path.Combine(Root, path));
    //        }
    //        catch
    //        {
    //            return null;
    //        }

    //        if (!IsUnderneathRoot(fullPath))
    //        {
    //            return null;
    //        }

    //        return fullPath;
    //    }

    //    private bool IsUnderneathRoot(string fullPath)
    //    {
    //        return fullPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase);
    //    }
    //}
}
