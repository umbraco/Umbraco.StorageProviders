using System;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Umbraco.StorageProviders.AzureBlob
{
    /// <summary>
    /// Represents an Azure Blob Storage prefix.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.FileProviders.IFileInfo" />
    public sealed class AzureBlobPrefixInfo : IFileInfo
    {
        /// <inheritdoc />
        public bool Exists => true;

        /// <inheritdoc />
        public bool IsDirectory => true;

        /// <inheritdoc />
        public DateTimeOffset LastModified => default;

        /// <inheritdoc />
        public long Length => -1;

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string PhysicalPath => null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobPrefixInfo"/> class.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        public AzureBlobPrefixInfo(string prefix)
        {
            ArgumentNullException.ThrowIfNull(prefix);

            Name = ParseName(prefix);
        }

        /// <inheritdoc />
        public Stream CreateReadStream() => throw new InvalidOperationException();

        private static string ParseName(string prefix)
        {
            var name = prefix.TrimEnd('/');

            return name.Substring(name.LastIndexOf('/') + 1);
        }
    }
}
