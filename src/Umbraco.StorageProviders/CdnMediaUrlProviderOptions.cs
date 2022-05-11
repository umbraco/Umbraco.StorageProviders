using System;
using System.ComponentModel.DataAnnotations;

namespace Umbraco.StorageProviders
{
    /// <summary>
    /// The CDN media URL provider options.
    /// </summary>
    public sealed class CdnMediaUrlProviderOptions
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
        /// Gets or sets a value indicating whether to remove the <see cref="Umbraco.Cms.Core.Configuration.Models.GlobalSettings.UmbracoMediaPath"/> from the path, defaults to <c>true</c>.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the media path needs to be removed from the path; otherwise, <c>false</c>.
        /// </value>
        public bool RemoveMediaFromPath { get; set; } = true;
    }
}
