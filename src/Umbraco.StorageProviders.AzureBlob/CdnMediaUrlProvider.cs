using System;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Routing;

namespace Umbraco.StorageProviders.AzureBlob
{
    /// <summary>
    /// A <see cref="IMediaUrlProvider"/> that returns a CDN url for a media item.
    /// </summary>
    public class CdnMediaUrlProvider : DefaultMediaUrlProvider
    {
        private bool _removeMediaFromPath;
        private Uri _cdnUrl;

        /// <summary>
        /// Creates a new instance of <see cref="CdnMediaUrlProvider"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="mediaPathGenerators"></param>
        /// <param name="uriUtility"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public CdnMediaUrlProvider(IOptionsMonitor<CdnMediaUrlProviderOptions> options,
            MediaUrlGeneratorCollection mediaPathGenerators, UriUtility uriUtility)
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

        private void OptionsOnChange(CdnMediaUrlProviderOptions options, string _)
        {
            _removeMediaFromPath = options.RemoveMediaFromPath;
            _cdnUrl = options.Url;
        }
    }
}
