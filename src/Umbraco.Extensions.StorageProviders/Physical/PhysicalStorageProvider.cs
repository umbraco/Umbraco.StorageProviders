using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using System;
using System.IO;
using Umbraco.Extensions.StorageProviders.Internal;

namespace Umbraco.Extensions.StorageProviders
{
    /// <summary>
    /// Storage provider for the on-disk file system.
    /// </summary>
    /// <seealso cref="Umbraco.Extensions.StorageProviders.IStorageProvider" />
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
        public void CopyFile(string sourceSubpath, string destinationSubpath, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(sourceSubpath) || PathUtils.HasInvalidPathChars(sourceSubpath) ||
                string.IsNullOrEmpty(destinationSubpath) || PathUtils.HasInvalidPathChars(destinationSubpath))
            {
                return;
            }

            // Relative paths starting with leading slashes are okay
            sourceSubpath = sourceSubpath.TrimStart(_pathSeparators);
            destinationSubpath = destinationSubpath.TrimStart(_pathSeparators);

            // Absolute paths not permitted.
            if (Path.IsPathRooted(sourceSubpath) || Path.IsPathRooted(destinationSubpath))
            {
                return;
            }

            string? sourceFullPath = GetFullPath(sourceSubpath);
            if (sourceFullPath == null)
            {
                return;
            }

            string? destinationFullPath = GetFullPath(destinationSubpath);
            if (destinationFullPath == null)
            {
                return;
            }

            File.Copy(sourceFullPath, destinationFullPath, overwrite);
        }

        /// <inheritdoc />
        public void DeleteDirectory(string subpath, bool recursive = false)
        {
            if (subpath == null || PathUtils.HasInvalidPathChars(subpath))
            {
                return;
            }

            // Relative paths starting with leading slashes are okay
            subpath = subpath.TrimStart(_pathSeparators);

            // Absolute paths not permitted.
            if (Path.IsPathRooted(subpath))
            {
                return;
            }

            string? fullPath = GetFullPath(subpath);
            if (fullPath == null || !Directory.Exists(fullPath))
            {
                return;
            }

            Directory.Delete(fullPath, recursive);
        }

        /// <inheritdoc />
        public void DeleteFile(string subpath)
        {
            if (string.IsNullOrEmpty(subpath) || PathUtils.HasInvalidPathChars(subpath))
            {
                return;
            }

            // Relative paths starting with leading slashes are okay
            subpath = subpath.TrimStart(_pathSeparators);

            // Absolute paths not permitted.
            if (Path.IsPathRooted(subpath))
            {
                return;
            }

            string? fullPath = GetFullPath(subpath);
            if (fullPath == null || !Directory.Exists(fullPath))
            {
                return;
            }

            File.Delete(fullPath);
        }

        /// <inheritdoc />
        public void MoveFile(string sourceSubpath, string destinationSubpath, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(sourceSubpath) || PathUtils.HasInvalidPathChars(sourceSubpath) ||
                string.IsNullOrEmpty(destinationSubpath) || PathUtils.HasInvalidPathChars(destinationSubpath))
            {
                return;
            }

            // Relative paths starting with leading slashes are okay
            sourceSubpath = sourceSubpath.TrimStart(_pathSeparators);
            destinationSubpath = destinationSubpath.TrimStart(_pathSeparators);

            // Absolute paths not permitted.
            if (Path.IsPathRooted(sourceSubpath) || Path.IsPathRooted(destinationSubpath))
            {
                return;
            }

            string? sourceFullPath = GetFullPath(sourceSubpath);
            if (sourceFullPath == null)
            {
                return;
            }

            string? destinationFullPath = GetFullPath(destinationSubpath);
            if (destinationFullPath == null)
            {
                return;
            }

            File.Move(sourceFullPath, destinationFullPath, overwrite);
        }

        /// <inheritdoc />
        public void StoreFile(string subpath, Stream stream, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(subpath) || PathUtils.HasInvalidPathChars(subpath) || stream is null)
            {
                return;
            }

            // Relative paths starting with leading slashes are okay
            subpath = subpath.TrimStart(_pathSeparators);

            // Absolute paths not permitted.
            if (Path.IsPathRooted(subpath))
            {
                return;
            }

            string? fullPath = GetFullPath(subpath);
            if (fullPath == null || !Directory.Exists(fullPath))
            {
                return;
            }

            using var fileStream = new FileStream(fullPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write);
            stream.CopyTo(fileStream);
        }

        private string? GetFullPath(string path)
        {
            if (PathUtils.PathNavigatesAboveRoot(path))
            {
                return null;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Path.Combine(Root, path));
            }
            catch
            {
                return null;
            }

            if (!IsUnderneathRoot(fullPath))
            {
                return null;
            }

            return fullPath;
        }

        private bool IsUnderneathRoot(string fullPath)
        {
            return fullPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase);
        }
    }
}
