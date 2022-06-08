using System;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;

namespace Umbraco.StorageProviders
{
    /// <summary>
    /// A <see cref="IMediaUrlProvider" /> that returns a CDN URL for a media item.
    /// </summary>
    /// <seealso cref="Umbraco.Cms.Core.Routing.DefaultMediaUrlProvider" />
    public sealed class CdnMediaUrlProvider : DefaultMediaUrlProvider
    {
        private Uri _cdnUrl;
        private bool _removeMediaFromPath;
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
            : base(mediaPathGenerators, uriUtility)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(globalSettings);
            ArgumentNullException.ThrowIfNull(hostingEnvironment);

            _cdnUrl = options.CurrentValue.Url;
            _removeMediaFromPath = options.CurrentValue.RemoveMediaFromPath;
            _mediaPath = hostingEnvironment.ToAbsolute(globalSettings.CurrentValue.UmbracoMediaPath).EnsureEndsWith('/');

            options.OnChange((options, name) =>
            {
                if (name == Options.DefaultName)
                {
                    _removeMediaFromPath = options.RemoveMediaFromPath;
                    _cdnUrl = options.Url;
                }
            });

            globalSettings.OnChange((options, name) =>
            {
                if (name == Options.DefaultName)
                {
                    _mediaPath = hostingEnvironment.ToAbsolute(options.UmbracoMediaPath).EnsureEndsWith('/');
                }
            });
        }

        /// <inheritdoc />
        public override UrlInfo? GetMediaUrl(IPublishedContent content, string propertyAlias, UrlMode mode, string? culture, Uri current)
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
