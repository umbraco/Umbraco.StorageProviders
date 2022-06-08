using System;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;

namespace Umbraco.StorageProviders.AzureBlob
{
    /// <summary>
    /// A <see cref="IMediaUrlProvider" /> that returns a CDN URL for a media item.
    /// </summary>
    /// <seealso cref="Umbraco.Cms.Core.Routing.DefaultMediaUrlProvider" />
    public class CdnMediaUrlProvider : DefaultMediaUrlProvider
    {
        private bool _removeMediaFromPath;
        private Uri _cdnUrl;
        private string _mediaPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="CdnMediaUrlProvider"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="globalSettings">The global settings.</param>
        /// <param name="hostingEnvironment">The hosting environment.</param>
        /// <param name="mediaPathGenerators">The media path generators.</param>
        /// <param name="uriUtility">The URI utility.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="globalSettings"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="hostingEnvironment"/> is <c>null</c>.</exception>
        public CdnMediaUrlProvider(IOptionsMonitor<CdnMediaUrlProviderOptions> options, IOptionsMonitor<GlobalSettings> globalSettings, IHostingEnvironment hostingEnvironment, MediaUrlGeneratorCollection mediaPathGenerators, UriUtility uriUtility)
            : this(options, mediaPathGenerators, uriUtility, string.Empty)
        {
            if (globalSettings == null) throw new ArgumentNullException(nameof(globalSettings));
            if (hostingEnvironment == null) throw new ArgumentNullException(nameof(hostingEnvironment));

            _mediaPath = hostingEnvironment.ToAbsolute(globalSettings.CurrentValue.UmbracoMediaPath).EnsureEndsWith('/');

            globalSettings.OnChange((options, name) =>
            {
                if (name == Options.DefaultName)
                {
                    _mediaPath = hostingEnvironment.ToAbsolute(options.UmbracoMediaPath).EnsureEndsWith('/');
                }
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CdnMediaUrlProvider"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="mediaPathGenerators">The media path generators.</param>
        /// <param name="uriUtility">The URI utility.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        [Obsolete("This constructor is obsolete and will be removed in a future version. Use another constructor instead.")]
        public CdnMediaUrlProvider(IOptionsMonitor<CdnMediaUrlProviderOptions> options, MediaUrlGeneratorCollection mediaPathGenerators, UriUtility uriUtility)
            : this(options, mediaPathGenerators, uriUtility, "/media/")
        { }

        private CdnMediaUrlProvider(IOptionsMonitor<CdnMediaUrlProviderOptions> options, MediaUrlGeneratorCollection mediaPathGenerators, UriUtility uriUtility, string mediaPath)
            : base(mediaPathGenerators, uriUtility)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _cdnUrl = options.CurrentValue.Url;
            _removeMediaFromPath = options.CurrentValue.RemoveMediaFromPath;
            _mediaPath = mediaPath;

            options.OnChange((options, name) =>
            {
                if (name == Options.DefaultName)
                {
                    _removeMediaFromPath = options.RemoveMediaFromPath;
                    _cdnUrl = options.Url;
                }
            });
        }

        /// <inheritdoc />
        public override UrlInfo? GetMediaUrl(IPublishedContent content, string propertyAlias, UrlMode mode, string culture, Uri current)
        {
            var mediaUrl = base.GetMediaUrl(content, propertyAlias, UrlMode.Relative, culture, current);
            if (mediaUrl?.IsUrl == true)
            {
                string url = mediaUrl.Text;

                int startIndex = 0;
                if (_removeMediaFromPath && url.StartsWith(_mediaPath, StringComparison.OrdinalIgnoreCase))
                {
                    startIndex = _mediaPath.Length;
                }
                else if (url.StartsWith('/'))
                {
                    startIndex = 1;
                }

                return UrlInfo.Url(_cdnUrl + url[startIndex..], culture);
            }

            return mediaUrl;
        }
    }
}
