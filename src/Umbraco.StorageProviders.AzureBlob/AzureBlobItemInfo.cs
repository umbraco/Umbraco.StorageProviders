using System;
using System.IO;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.FileProviders;

namespace Umbraco.StorageProviders.AzureBlob
{
    /// <summary>
    /// Represents an Azure Blob Storage blob item.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.FileProviders.IFileInfo" />
    public sealed class AzureBlobItemInfo : IFileInfo
    {
        private readonly BlobClient _blobClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobItemInfo" /> class.
        /// </summary>
        /// <param name="blobClient">The blob client.</param>
        /// <param name="properties">The properties.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="blobClient" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="properties" /> is <c>null</c>.</exception>
        public AzureBlobItemInfo(BlobClient blobClient, BlobProperties properties)
            : this(blobClient)
        {
            ArgumentNullException.ThrowIfNull(properties);

            LastModified = properties.LastModified;
            Length = properties.ContentLength;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobItemInfo" /> class.
        /// </summary>
        /// <param name="blobClient">The blob client.</param>
        /// <param name="properties">The properties.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="blobClient" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="properties" /> is <c>null</c>.</exception>
        public AzureBlobItemInfo(BlobClient blobClient, BlobItemProperties properties)
            : this(blobClient)
        {
            ArgumentNullException.ThrowIfNull(properties);

            LastModified = properties.LastModified.GetValueOrDefault();
            Length = properties.ContentLength.GetValueOrDefault(-1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobItemInfo" /> class.
        /// </summary>
        /// <param name="blobClient">The blob client.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="blobClient" /> is <c>null</c>.</exception>
        private AzureBlobItemInfo(BlobClient blobClient)
        {
            _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));

            Name = ParseName(blobClient.Name);
        }

        /// <inheritdoc />
        public bool Exists => true;

        /// <inheritdoc />
        public bool IsDirectory => false;

        /// <inheritdoc />
        public DateTimeOffset LastModified { get; }

        /// <inheritdoc />
        public long Length { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string PhysicalPath => null!;

        /// <inheritdoc />
        public Stream CreateReadStream() => _blobClient.OpenRead();

        /// <summary>
        /// Parses the name from the file path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>
        /// The name.
        /// </returns>
        internal static string ParseName(string path) => path[(path.LastIndexOf('/') + 1)..];
    }
}
