using System.ComponentModel.DataAnnotations;

namespace Umbraco.StorageProviders.AzureBlob
{
    /// <summary>
    /// The Azure Blob File System Options.
    /// </summary>
    public class AzureBlobFileSystemOptions
    {
        /// <summary>
        /// The media filesystem name.
        /// </summary>
        public const string MediaFileSystemName = "Media";

        /// <summary>
        /// The storage account connection string.
        /// </summary>
        [Required]
        public string ConnectionString { get; set; } = null!;

        /// <summary>
        /// The container name.
        /// </summary>
        [Required]
        public string ContainerName { get; set; } = null!;

        /// <summary>
        /// The virtual path.
        /// </summary>
        [Required]
        public string VirtualPath { get; set; } = null!;
    }
}
