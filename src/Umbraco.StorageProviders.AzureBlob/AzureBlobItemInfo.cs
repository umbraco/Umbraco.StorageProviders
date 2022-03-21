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
    public class AzureBlobItemInfo : IFileInfo
    {
        private readonly BlobClient _blobClient;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobItemInfo" /> class.
        /// </summary>
        /// <param name="blobClient">The blob client.</param>
        /// <exception cref="System.ArgumentNullException">blobClient</exception>
        protected AzureBlobItemInfo(BlobClient blobClient)
        {
            _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));

            Name = ParseName(blobClient.Name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobItemInfo" /> class.
        /// </summary>
        /// <param name="blobClient">The blob client.</param>
        /// <param name="properties">The properties.</param>
        /// <exception cref="System.ArgumentNullException">properties</exception>
        public AzureBlobItemInfo(BlobClient blobClient, BlobProperties properties)
            : this(blobClient)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            LastModified = properties.LastModified;
            Length = properties.ContentLength;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobItemInfo" /> class.
        /// </summary>
        /// <param name="blobClient">The blob client.</param>
        /// <param name="properties">The properties.</param>
        /// <exception cref="System.ArgumentNullException">properties</exception>
        public AzureBlobItemInfo(BlobClient blobClient, BlobItemProperties properties)
            : this(blobClient)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            LastModified = properties.LastModified.GetValueOrDefault();
            Length = properties.ContentLength.GetValueOrDefault(-1);
        }

        /// <inheritdoc />
        public Stream CreateReadStream() => _blobClient.OpenRead();

        internal static string ParseName(string path) => path.Substring(path.LastIndexOf('/') + 1);
    }
}
