using System;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Routing;

namespace Umbraco.StorageProviders
{
    /// <summary>
    /// A <see cref="IMediaUrlProvider" /> that returns a CDN URL for a media item.
    /// </summary>
    /// <seealso cref="Umbraco.Cms.Core.Routing.DefaultMediaUrlProvider" />
    public class CdnMediaUrlProvider : DefaultMediaUrlProvider
    {
        private bool _removeMediaFromPath;
        private Uri _cdnUrl;

        /// <summary>
        /// Creates a new instance of <see cref="CdnMediaUrlProvider" />.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="mediaPathGenerators">The media path generators.</param>
        /// <param name="uriUtility">The URI utility.</param>
        /// <exception cref="System.ArgumentNullException">options</exception>
        public CdnMediaUrlProvider(IOptionsMonitor<CdnMediaUrlProviderOptions> options, MediaUrlGeneratorCollection mediaPathGenerators, UriUtility uriUtility)
            : base(mediaPathGenerators, uriUtility)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _cdnUrl = options.CurrentValue.Url;
            _removeMediaFromPath = options.CurrentValue.RemoveMediaFromPath;

            options.OnChange(OptionsOnChange);
        }

        /// <inheritdoc />
        public override UrlInfo? GetMediaUrl(IPublishedContent content, string propertyAlias, UrlMode mode, string culture, Uri current)
        {
            var mediaUrl = base.GetMediaUrl(content, propertyAlias, UrlMode.Relative, culture, current);
            if (mediaUrl == null) return null;

            return mediaUrl.IsUrl switch
            {
                false => mediaUrl,
                _ => UrlInfo.Url($"{_cdnUrl}/{mediaUrl.Text[(_removeMediaFromPath ? "/media/" : "/").Length..]}", culture)
            };
        }

        private void OptionsOnChange(CdnMediaUrlProviderOptions options, string name)
        {
            if (name != Options.DefaultName) return;

            _removeMediaFromPath = options.RemoveMediaFromPath;
            _cdnUrl = options.Url;
        }
    }
}
