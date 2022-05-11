using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Umbraco.Extensions;

namespace Umbraco.StorageProviders.AzureBlob.IO
{
    /// <inheritdoc />
    public sealed class AzureBlobFileSystem : IAzureBlobFileSystem, IFileProviderFactory
    {
        private readonly string _rootUrl;
        private readonly string _containerRootPath;
        private readonly BlobContainerClient _container;
        private readonly IIOHelper _ioHelper;
        private readonly IContentTypeProvider _contentTypeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFileSystem"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="hostingEnvironment">The hosting environment.</param>
        /// <param name="ioHelper">The I/O helper.</param>
        /// <param name="contentTypeProvider">The content type provider.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="hostingEnvironment" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="ioHelper" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="contentTypeProvider" /> is <c>null</c>.</exception>
        public AzureBlobFileSystem(AzureBlobFileSystemOptions options, IHostingEnvironment hostingEnvironment, IIOHelper ioHelper, IContentTypeProvider contentTypeProvider)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(hostingEnvironment);

            _rootUrl = EnsureUrlSeparatorChar(hostingEnvironment.ToAbsolute(options.VirtualPath)).TrimEnd('/');
            _containerRootPath = options.ContainerRootPath ?? _rootUrl;
            _container = new BlobContainerClient(options.ConnectionString, options.ContainerName);
            _ioHelper = ioHelper ?? throw new ArgumentNullException(nameof(ioHelper));
            _contentTypeProvider = contentTypeProvider ?? throw new ArgumentNullException(nameof(contentTypeProvider));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFileSystem"/> class.
        /// </summary>
        /// <param name="rootUrl">The root URL.</param>
        /// <param name="blobContainerClient">The blob container client.</param>
        /// <param name="ioHelper">The I/O helper.</param>
        /// <param name="contentTypeProvider">The content type provider.</param>
        /// <param name="containerRootPath">The container root path (uses the root URL if not set).</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="rootUrl" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="blobContainerClient" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="ioHelper" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="contentTypeProvider" /> is <c>null</c>.</exception>
        public AzureBlobFileSystem(string rootUrl, BlobContainerClient blobContainerClient, IIOHelper ioHelper, IContentTypeProvider contentTypeProvider, string? containerRootPath = null)
        {
            ArgumentNullException.ThrowIfNull(rootUrl);

            _rootUrl = EnsureUrlSeparatorChar(rootUrl).TrimEnd('/');
            _containerRootPath = containerRootPath ?? _rootUrl;
            _container = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));
            _ioHelper = ioHelper ?? throw new ArgumentNullException(nameof(ioHelper));
            _contentTypeProvider = contentTypeProvider ?? throw new ArgumentNullException(nameof(contentTypeProvider));
        }

        /// <inheritdoc />
        public bool CanAddPhysical => false;

        /// <summary>
        /// Creates a new container under the specified account if a container with the same name does not already exist.
        /// </summary>
        /// <param name="options">The Azure Blob Storage file system options.</param>
        /// <param name="accessType">Optionally specifies whether data in the container may be accessed publicly and the level of access.
        /// <see cref="PublicAccessType.BlobContainer" /> specifies full public read access for container and blob data. Clients can enumerate blobs within the container via anonymous request, but cannot enumerate containers within the storage account.
        /// <see cref="PublicAccessType.Blob" /> specifies public read access for blobs. Blob data within this container can be read via anonymous request, but container data is not available. Clients cannot enumerate blobs within the container via anonymous request.
        /// <see cref="PublicAccessType.None" /> specifies that the container data is private to the account owner.</param>
        /// <returns>
        /// If the container does not already exist, a <see cref="Response{T}" /> describing the newly created container. If the container already exists, <see langword="null" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
        public static Response<BlobContainerInfo> CreateIfNotExists(AzureBlobFileSystemOptions options, PublicAccessType accessType = PublicAccessType.None)
        {
            ArgumentNullException.ThrowIfNull(options);

            return new BlobContainerClient(options.ConnectionString, options.ContainerName).CreateIfNotExists(accessType);
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public IEnumerable<string> GetDirectories(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return ListBlobs(GetDirectoryPath(path))
                .Where(x => x.IsPrefix)
                .Select(x => GetRelativePath($"/{x.Prefix}").Trim('/'));
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public void DeleteDirectory(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            DeleteDirectory(path, true);
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public void DeleteDirectory(string path, bool recursive)
        {
            ArgumentNullException.ThrowIfNull(path);

            foreach (var blob in ListBlobs(GetDirectoryPath(path)))
            {
                if (blob.IsPrefix)
                {
                    DeleteDirectory(blob.Prefix, true);
                }
                else if (blob.IsBlob)
                {
                    _container.GetBlobClient(blob.Blob.Name).DeleteIfExists();
                }
            }
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public bool DirectoryExists(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return GetBlobClient(GetDirectoryPath(path)).Exists();
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream" /> is <c>null</c>.</exception>
        public void AddFile(string path, Stream stream)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(stream);

            AddFile(path, stream, true);
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream" /> is <c>null</c>.</exception>
        public void AddFile(string path, Stream stream, bool overrideIfExists)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(stream);

            var blob = GetBlobClient(path);
            if (!overrideIfExists && blob.Exists())
            {
                throw new InvalidOperationException($"A file at path '{path}' already exists");
            }

            var headers = new BlobHttpHeaders();
            if (_contentTypeProvider.TryGetContentType(path, out var contentType))
            {
                headers.ContentType = contentType;
            }

            var conditions = overrideIfExists ? null : new BlobRequestConditions
            {
                IfNoneMatch = ETag.All
            };

            blob.Upload(stream, headers, conditions: conditions);
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="physicalPath" /> is <c>null</c>.</exception>
        public void AddFile(string path, string physicalPath, bool overrideIfExists = true, bool copy = false)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(physicalPath);

            var destinationBlob = GetBlobClient(path);
            if (!overrideIfExists && destinationBlob.Exists())
            {
                throw new InvalidOperationException($"A file at path '{path}' already exists");
            }

            var sourceBlob = GetBlobClient(physicalPath);
            var destinationConditions = overrideIfExists ? null : new BlobRequestConditions
            {
                IfNoneMatch = ETag.All
            };
            var copyFromUriOperation = destinationBlob.StartCopyFromUri(sourceBlob.Uri, destinationConditions: destinationConditions);
            if (copyFromUriOperation?.HasCompleted == false)
            {
                Task.Run(async () => await copyFromUriOperation.WaitForCompletionAsync().ConfigureAwait(false))
                    .GetAwaiter()
                    .GetResult();
            }

            if (!copy)
            {
                sourceBlob.DeleteIfExists();
            }
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public IEnumerable<string> GetFiles(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return GetFiles(path, null);
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public IEnumerable<string> GetFiles(string path, string? filter)
        {
            ArgumentNullException.ThrowIfNull(path);

            var files = ListBlobs(GetDirectoryPath(path)).Where(x => x.IsBlob).Select(x => x.Blob.Name);
            if (!string.IsNullOrEmpty(filter) && filter != "*.*")
            {
                // TODO: Might be better to use a globbing library
                filter = filter.TrimStart("*");
                files = files.Where(x => x.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) > -1);
            }

            return files.Select(x => GetRelativePath($"/{x}"));
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public Stream OpenFile(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return GetBlobClient(path).OpenRead();
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public void DeleteFile(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            GetBlobClient(path).DeleteIfExists();
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public bool FileExists(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return GetBlobClient(path).Exists();
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="fullPathOrUrl" /> is <c>null</c>.</exception>
        public string GetRelativePath(string fullPathOrUrl)
        {
            ArgumentNullException.ThrowIfNull(fullPathOrUrl);

            // test url
            var path = EnsureUrlSeparatorChar(fullPathOrUrl); // ensure url separator char

            // if it starts with the root url, strip it and trim the starting slash to make it relative
            // eg "/Media/1234/img.jpg" => "1234/img.jpg"
            if (_ioHelper.PathStartsWith(path, _rootUrl, '/'))
            {
                path = path[_rootUrl.Length..].TrimStart('/');
            }

            // unchanged - what else?
            return path;
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public string GetFullPath(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            path = EnsureUrlSeparatorChar(path);
            return (_ioHelper.PathStartsWith(path, _rootUrl, '/') ? path : $"{_rootUrl}/{path}").Trim('/');
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public string GetUrl(string? path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return $"{_rootUrl}/{EnsureUrlSeparatorChar(path).Trim('/')}";
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public DateTimeOffset GetLastModified(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return GetBlobClient(path).GetProperties().Value.LastModified;
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public DateTimeOffset GetCreated(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return GetBlobClient(path).GetProperties().Value.CreatedOn;
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public long GetSize(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return GetBlobClient(path).GetProperties().Value.ContentLength;
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"><paramref name="path" /> is <c>null</c>.</exception>
        public BlobClient GetBlobClient(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return _container.GetBlobClient(GetBlobPath(path));
        }

        /// <inheritdoc />
        public IFileProvider Create() => new AzureBlobFileProvider(_container, _containerRootPath);

        private static string EnsureUrlSeparatorChar(string path)
            => path.Replace("\\", "/", StringComparison.InvariantCultureIgnoreCase);

        private string GetDirectoryPath(string fullPathOrUrl)
        {
            var path = GetFullPath(fullPathOrUrl);

            return path.Length == 0 ? path : path.EnsureEndsWith('/');
        }

        private IEnumerable<BlobHierarchyItem> ListBlobs(string path)
        {
            return _container.GetBlobsByHierarchy(prefix: path);
        }

        private string GetBlobPath(string path)
        {
            path = EnsureUrlSeparatorChar(path);

            if (_ioHelper.PathStartsWith(path, _containerRootPath, '/'))
            {
                return path;
            }

            if (_ioHelper.PathStartsWith(path, _rootUrl, '/'))
            {
                path = path[_rootUrl.Length..];
            }

            path = $"{_containerRootPath}/{path.TrimStart('/')}";

            return path.Trim('/');
        }
    }
}
