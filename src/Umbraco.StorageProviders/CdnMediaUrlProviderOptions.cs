using System;
using System.ComponentModel.DataAnnotations;

namespace Umbraco.StorageProviders
{
    /// <summary>
    /// The CDN media URL provider options.
    /// </summary>
    public class CdnMediaUrlProviderOptions
    {
        /// <summary>
        /// Gets or sets the CDN media root URL.
        /// </summary>
        /// <value>
        /// The CDN media root URL.
        /// </value>
        [Required]
        public Uri Url { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether to remove <c>/media/</c> from the path, defaults to <c>true</c>.
        /// </summary>
        /// <value>
        ///   <c>true</c> if <c>/media/</c> needs to be removed from the path; otherwise, <c>false</c>.
        /// </value>
        public bool RemoveMediaFromPath { get; set; } = true;
    }
}
