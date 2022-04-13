using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.StorageProviders;

// ReSharper disable once CheckNamespace
// uses same namespace as Umbraco Core for easier discoverability
namespace Umbraco.Cms.Core.DependencyInjection
{
    /// <summary>
    /// Extension methods to help registering a CDN media URL provider.
    /// </summary>
    public static class CdnMediaUrlProviderExtensions
    {
        internal static IUmbracoBuilder AddInternal(this IUmbracoBuilder builder, Action<OptionsBuilder<CdnMediaUrlProviderOptions>>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.MediaUrlProviders().Insert<CdnMediaUrlProvider>();

            var optionsBuilder = builder.Services.AddOptions<CdnMediaUrlProviderOptions>()
                .BindConfiguration("Umbraco:Storage:Cdn")
                .ValidateDataAnnotations();

            configure?.Invoke(optionsBuilder);

            return builder;
        }

        /// <summary>
        /// Registers and configures the <see cref="CdnMediaUrlProvider" />.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        public static IUmbracoBuilder AddCdnMediaUrlProvider(this IUmbracoBuilder builder)
            => builder.AddInternal();

        /// <summary>
        /// Registers and configures the <see cref="CdnMediaUrlProvider" />.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="configure">An action used to configure the <see cref="CdnMediaUrlProviderOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        public static IUmbracoBuilder AddCdnMediaUrlProvider(this IUmbracoBuilder builder, Action<CdnMediaUrlProviderOptions> configure)
            => builder.AddInternal(optionsBuilder => optionsBuilder.Configure(configure));

        /// <summary>
        /// Registers and configures the <see cref="CdnMediaUrlProvider" />.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="configure">An action used to configure the <see cref="CdnMediaUrlProviderOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        public static IUmbracoBuilder AddCdnMediaUrlProvider(this IUmbracoBuilder builder, Action<CdnMediaUrlProviderOptions, IServiceProvider> configure)
            => builder.AddInternal(optionsBuilder => optionsBuilder.Configure(configure));

        /// <summary>
        /// Registers and configures the <see cref="CdnMediaUrlProvider" />.
        /// </summary>
        /// <typeparam name="TDep">A dependency used by the configure action.</typeparam>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="configure">An action used to configure the <see cref="CdnMediaUrlProviderOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        public static IUmbracoBuilder AddCdnMediaUrlProvider<TDep>(this IUmbracoBuilder builder, Action<CdnMediaUrlProviderOptions, TDep> configure)
            where TDep : class
            => builder.AddInternal(optionsBuilder => optionsBuilder.Configure(configure));
    }
}
