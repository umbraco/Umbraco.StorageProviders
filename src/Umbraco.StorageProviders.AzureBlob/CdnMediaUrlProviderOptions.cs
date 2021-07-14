using System;
using System.ComponentModel.DataAnnotations;

namespace Umbraco.StorageProviders.AzureBlob
{
    /// <summary>
    /// The Cdn Media Url Provider Options.
    /// </summary>
    public class CdnMediaUrlProviderOptions
    {
        /// <summary>
        /// The CDN root url.
        /// </summary>
        [Required]
        public Uri Url { get; set; } = null!;

        /// <summary>
        /// If <c>true</c> <c>/media/</c> will be removed from the media path.
        /// </summary>
        /// <remarks>Default is <c>true</c>.</remarks>
        public bool RemoveMediaFromPath { get; set; } = true;
    }
}
