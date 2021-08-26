using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Umbraco.Cms.Core.IO;

namespace Umbraco.StorageProviders.IO
{
    /// <summary>
    /// Exposes an <see cref="IFileSystem" /> as an <see cref="IFileProvider" />.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.FileProviders.IFileProvider" />
    public class FileSystemFileProvider : IFileProvider
    {
        private readonly IFileSystem _fileSystem;
        private readonly string? _pathPrefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemFileProvider" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="pathPrefix">The path prefix.</param>
        /// <exception cref="System.ArgumentNullException">fileSystem</exception>
        public FileSystemFileProvider(IFileSystem fileSystem, string? pathPrefix = null)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _pathPrefix = pathPrefix;
        }

        /// <inheritdoc />
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            var path = _pathPrefix + subpath;

            if (path == null || _fileSystem.DirectoryExists(path) == false)
            {
                return NotFoundDirectoryContents.Singleton;
            }

            return new FileSystemDirectoryContents(_fileSystem, path);
        }

        /// <inheritdoc />
        public IFileInfo GetFileInfo(string subpath)
        {
            var path = _pathPrefix + subpath;

            if (path == null || _fileSystem.FileExists(path) == false)
            {
                return new NotFoundFileInfo(path);
            }

            return new FileSystemFileInfo(_fileSystem, path);
        }

        /// <inheritdoc />
        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }
}
