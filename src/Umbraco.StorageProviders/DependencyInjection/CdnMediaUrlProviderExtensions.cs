using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.StorageProviders;

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
        /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
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
        /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
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
        /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
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
        /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
        public static IUmbracoBuilder AddCdnMediaUrlProvider<TDep>(this IUmbracoBuilder builder, Action<CdnMediaUrlProviderOptions, TDep> configure)
            where TDep : class
            => builder.AddInternal(optionsBuilder => optionsBuilder.Configure(configure));

        /// <summary>
        /// Registers and configures the <see cref="CdnMediaUrlProvider" />.
        /// </summary>
        /// <param name="builder">The <see cref="IUmbracoBuilder" />.</param>
        /// <param name="configure">An action used to configure the <see cref="CdnMediaUrlProviderOptions" />.</param>
        /// <returns>
        /// The <see cref="IUmbracoBuilder" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="builder" /> is <c>null</c>.</exception>
        internal static IUmbracoBuilder AddInternal(this IUmbracoBuilder builder, Action<OptionsBuilder<CdnMediaUrlProviderOptions>>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.MediaUrlProviders()?.Insert<CdnMediaUrlProvider>();

            var optionsBuilder = builder.Services.AddOptions<CdnMediaUrlProviderOptions>()
                .BindConfiguration("Umbraco:Storage:Cdn")
                .ValidateDataAnnotations();

            configure?.Invoke(optionsBuilder);

            return builder;
        }
    }
}
