using System;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.StorageProviders.AzureBlob;

// ReSharper disable once CheckNamespace
// uses same namespace as Umbraco Core for easier discoverability
namespace Umbraco.Cms.Core.DependencyInjection
{
    /// <summary>
    /// Extension methods to help registering a CDN media URL provider.
    /// </summary>
    public static class CdnMediaUrlProviderExtensions
    {
        /// <summary>
        /// Registers and configures the <see cref="CdnMediaUrlProvider" />.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">builder</exception>
        public static IUmbracoBuilder AddCdnMediaUrlProvider(this IUmbracoBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.Services.AddOptions<CdnMediaUrlProviderOptions>()
                .BindConfiguration("Umbraco:Storage:AzureBlob:Media:Cdn")
                .ValidateDataAnnotations();

            builder.MediaUrlProviders().Insert<CdnMediaUrlProvider>();

            return builder;
        }

        /// <summary>
        /// Registers and configures the <see cref="CdnMediaUrlProvider" />.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="configure">An action used to configure the <see cref="CdnMediaUrlProviderOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">builder
        /// or
        /// configure</exception>
        public static IUmbracoBuilder AddCdnMediaUrlProvider(this IUmbracoBuilder builder, Action<CdnMediaUrlProviderOptions> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            AddCdnMediaUrlProvider(builder);

            builder.Services
                .AddOptions<CdnMediaUrlProviderOptions>()
                .Configure(configure);

            return builder;
        }

        /// <summary>
        /// Registers and configures the <see cref="CdnMediaUrlProvider" />.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="configure">An action used to configure the <see cref="CdnMediaUrlProviderOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">builder
        /// or
        /// configure</exception>
        public static IUmbracoBuilder AddCdnMediaUrlProvider(this IUmbracoBuilder builder, Action<CdnMediaUrlProviderOptions, IServiceProvider> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            AddCdnMediaUrlProvider(builder);

            builder.Services
                .AddOptions<CdnMediaUrlProviderOptions>()
                .Configure(configure);

            return builder;
        }
    }
}
