using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;

namespace Umbraco.StorageProviders;

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
    /// Initializes a new instance of the <see cref="CdnMediaUrlProvider" /> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="globalSettings">The global settings.</param>
    /// <param name="hostingEnvironment">The hosting environment.</param>
    /// <param name="mediaPathGenerators">The media path generators.</param>
    /// <param name="urlAssembler">The URL assembler.</param>
    public CdnMediaUrlProvider(IOptionsMonitor<CdnMediaUrlProviderOptions> options, IOptionsMonitor<GlobalSettings> globalSettings, IHostingEnvironment hostingEnvironment, MediaUrlGeneratorCollection mediaPathGenerators, IUrlAssembler urlAssembler)
        : base(mediaPathGenerators, urlAssembler)
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
        UrlInfo? mediaUrl = base.GetMediaUrl(content, propertyAlias, UrlMode.Relative, culture, current);
        if (mediaUrl?.Url is Uri url)
        {
            var path = url.ToString();

            int startIndex = 0;
            if (_removeMediaFromPath && path.StartsWith(_mediaPath, StringComparison.OrdinalIgnoreCase))
            {
                startIndex = _mediaPath.Length;
            }
            else if (path.StartsWith('/'))
            {
                startIndex = 1;
            }

            return UrlInfo.AsUrl(_cdnUrl + path[startIndex..], mediaUrl.Provider, mediaUrl.Culture, mediaUrl.IsExternal);
        }

        return mediaUrl;
    }
}
