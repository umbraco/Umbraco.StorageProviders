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
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Umbraco.Extensions;

namespace Umbraco.StorageProviders.AzureBlob.IO
{
    /// <inheritdoc />
    public class AzureBlobFileSystem : IAzureBlobFileSystem
    {
        private readonly BlobContainerClient _container;
        private readonly IContentTypeProvider _contentTypeProvider;
        private readonly IIOHelper _ioHelper;
        private readonly string _rootUrl;
        private readonly string _containerRootPath;

        /// <summary>
        ///     Creates a new instance of <see cref="AzureBlobFileSystem" />.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="ioHelper"></param>
        /// <param name="contentTypeProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public AzureBlobFileSystem(AzureBlobFileSystemOptions options, IHostingEnvironment hostingEnvironment,
            IIOHelper ioHelper, IContentTypeProvider contentTypeProvider)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (hostingEnvironment == null) throw new ArgumentNullException(nameof(hostingEnvironment));

            _ioHelper = ioHelper ?? throw new ArgumentNullException(nameof(ioHelper));
            _contentTypeProvider = contentTypeProvider ?? throw new ArgumentNullException(nameof(contentTypeProvider));

            _rootUrl = EnsureUrlSeparatorChar(hostingEnvironment.ToAbsolute(options.VirtualPath)).TrimEnd('/');
            _containerRootPath = options.ContainerRootPath ?? _rootUrl;

            var client = new BlobServiceClient(options.ConnectionString);
            _container = client.GetBlobContainerClient(options.ContainerName);
        }

        /// <inheritdoc />
        public IEnumerable<string> GetDirectories(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return ListBlobs(GetDirectoryPath(path))
                .Where(x => x.IsPrefix)
                .Select(x => GetRelativePath($"/{x.Prefix}").Trim('/'));
        }

        /// <inheritdoc />
        public void DeleteDirectory(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            DeleteDirectory(path, true);
        }

        /// <inheritdoc />
        public void DeleteDirectory(string path, bool recursive)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            foreach (var blob in ListBlobs(GetDirectoryPath(path)))
                if (blob.IsPrefix)
                    DeleteDirectory(blob.Prefix, true);
                else if (blob.IsBlob) _container.GetBlobClient(blob.Blob.Name).DeleteIfExists();
        }

        /// <inheritdoc />
        public bool DirectoryExists(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return GetBlobClient(GetDirectoryPath(path)).Exists();
        }

        /// <inheritdoc />
        public void AddFile(string path, Stream stream)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            AddFile(path, stream, true);
        }

        /// <inheritdoc />
        public void AddFile(string path, Stream stream, bool overrideIfExists)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var blob = GetBlobClient(path);
            if (!overrideIfExists && blob.Exists())
                throw new InvalidOperationException($"A file at path '{path}' already exists");

            var headers = new BlobHttpHeaders();

            if (_contentTypeProvider.TryGetContentType(path, out var contentType)) headers.ContentType = contentType;

            blob.Upload(stream, headers,
                conditions: overrideIfExists ? null : new BlobRequestConditions { IfNoneMatch = ETag.All });
        }

        /// <inheritdoc />
        public void AddFile(string path, string physicalPath, bool overrideIfExists = true, bool copy = false)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (physicalPath == null) throw new ArgumentNullException(nameof(physicalPath));

            var destinationBlob = GetBlobClient(path);
            if (!overrideIfExists && destinationBlob.Exists())
                throw new InvalidOperationException($"A file at path '{path}' already exists");

            var sourceBlob = GetBlobClient(physicalPath);

            var copyFromUriOperation = destinationBlob.StartCopyFromUri(sourceBlob.Uri,
                destinationConditions: overrideIfExists
                    ? null
                    : new BlobRequestConditions { IfNoneMatch = ETag.All });

            if (copyFromUriOperation?.HasCompleted == false)
                Task.Run(async () => await copyFromUriOperation.WaitForCompletionAsync().ConfigureAwait(false))
                    .GetAwaiter()
                    .GetResult();

            if (!copy) sourceBlob.DeleteIfExists();
        }

        /// <inheritdoc />
        public IEnumerable<string> GetFiles(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return GetFiles(path, null);
        }

        /// <inheritdoc />
        public IEnumerable<string> GetFiles(string path, string? filter)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            var files = ListBlobs(GetDirectoryPath(path))
                .Where(x => x.IsBlob)
                .Select(x => x.Blob.Name);

            if (!string.IsNullOrEmpty(filter) && filter != "*.*")
            {
                // TODO: Might be better to use a globbing library
                filter = filter.TrimStart("*");
                files = files.Where(x => x.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) > -1);
            }

            return files.Select(x => GetRelativePath($"/{x}"));
        }

        /// <inheritdoc />
        public Stream OpenFile(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return GetBlobClient(path).OpenRead();
        }

        /// <inheritdoc />
        public void DeleteFile(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            GetBlobClient(path).DeleteIfExists();
        }

        /// <inheritdoc />
        public bool FileExists(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return GetBlobClient(path).Exists();
        }

        /// <inheritdoc />
        [SuppressMessage("Design",
            "CA1054: Change the type of parameter 'fullPathOrUrl' of method 'AzureBlobFileSystem.GetRelativePath(string)' from 'string' to 'System.Uri', or provide an overload to 'AzureBlobFileSystem.GetRelativePath(string)' that allows 'fullPathOrUrl' to be passed as a 'System.Uri' object",
            Justification = "Interface implementation")]
        public string GetRelativePath(string fullPathOrUrl)
        {
            if (fullPathOrUrl == null) throw new ArgumentNullException(nameof(fullPathOrUrl));

            // test url
            var path = EnsureUrlSeparatorChar(fullPathOrUrl); // ensure url separator char

            // if it starts with the root url, strip it and trim the starting slash to make it relative
            // eg "/Media/1234/img.jpg" => "1234/img.jpg"
            if (_ioHelper.PathStartsWith(path, _rootUrl, '/'))
                path = path[_rootUrl.Length..].TrimStart('/');

            // unchanged - what else?
            return path;
        }

        /// <inheritdoc />
        public string GetFullPath(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            path = EnsureUrlSeparatorChar(path);
            return (_ioHelper.PathStartsWith(path, _rootUrl, '/') ? path : $"{_rootUrl}/{path}").Trim('/');
        }

        /// <inheritdoc />
        [SuppressMessage("Design",
            "CA1055: Change the return type of method 'AzureBlobFileSystem.GetUrl(string)' from 'string' to 'System.Uri'",
            Justification = "Interface implementation")]
        public string GetUrl(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return $"{_rootUrl}/{EnsureUrlSeparatorChar(path).Trim('/')}";
        }

        /// <inheritdoc />
        public DateTimeOffset GetLastModified(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return GetBlobClient(path).GetProperties().Value.LastModified;
        }

        /// <inheritdoc />
        public DateTimeOffset GetCreated(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return GetBlobClient(path).GetProperties().Value.CreatedOn;
        }

        /// <inheritdoc />
        public long GetSize(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return GetBlobClient(path).GetProperties().Value.ContentLength;
        }

        /// <inheritdoc />
        public BlobClient GetBlobClient(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return _container.GetBlobClient(GetBlobPath(path));
        }

        /// <inheritdoc />
        public bool CanAddPhysical => false;

        private static string EnsureUrlSeparatorChar(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return path.Replace("\\", "/", StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetDirectoryPath(string fullPathOrUrl)
        {
            if (fullPathOrUrl == null) throw new ArgumentNullException(nameof(fullPathOrUrl));

            var path = GetFullPath(fullPathOrUrl);
            return path.Length == 0 ? path : path.EnsureEndsWith('/');
        }

        private IEnumerable<BlobHierarchyItem> ListBlobs(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return _container.GetBlobsByHierarchy(prefix: path);
        }

        private string GetBlobPath(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

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
        /// <exception cref="System.ArgumentNullException">options</exception>
        public static Response<BlobContainerInfo> CreateIfNotExists(AzureBlobFileSystemOptions options, PublicAccessType accessType = PublicAccessType.None)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return new BlobContainerClient(options.ConnectionString, options.ContainerName).CreateIfNotExists(accessType);
        }
    }
}
