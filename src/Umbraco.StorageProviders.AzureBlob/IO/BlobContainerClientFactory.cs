using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Azure.Identity;
using Azure.Storage.Blobs;

namespace Umbraco.StorageProviders.AzureBlob.IO
{
    /// <summary>
    /// The default <see cref="BlobContainerClient"/> factory.
    /// </summary>
    public class BlobContainerClientFactory : IBlobContainerClientFactory
    {
        /// <summary>
        /// The Azure storage ConnectionString parser expression.
        /// </summary>
        /// <remarks>
        /// Unfortunatly the official parser is internal.
        /// </remarks>
        private static readonly Regex ConnectionStringParser = new Regex("(?<Key>[^=]+)=(?<Value>[^;]+);?", RegexOptions.Compiled);

        /// <inheritdoc/>
        public BlobContainerClient Build(AzureBlobFileSystemOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var connectionStringParameter = GetConnectionStringParameters(options.ConnectionString);
            if (connectionStringParameter.TryGetValue("Authentication", out var authentication) &&
                "Active Directory Default".Equals(authentication, StringComparison.OrdinalIgnoreCase) &&
                connectionStringParameter.TryGetValue("AccountName", out var accountName))
            {
                if (!connectionStringParameter.TryGetValue("EndpointSuffix", out var endpointSuffix))
                {
                    endpointSuffix = "core.windows.net";
                }

                if (!connectionStringParameter.TryGetValue("DefaultEndpointsProtocol", out var endpointProtocol))
                {
                    endpointProtocol = "https";
                }

                var containerEndpoint = new Uri($"{endpointProtocol}://{accountName}.blob.{endpointSuffix}/{options.ContainerName}");
                return new BlobContainerClient(containerEndpoint, new DefaultAzureCredential());
            }
            else
            {
                return new BlobContainerClient(options.ConnectionString, options.ContainerName);
            }
        }

        /// <summary>
        /// Gets the connection string parameters from the specified <paramref name="connectionString"/>.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The connection string parameters.</returns>
        private static IDictionary<string, string> GetConnectionStringParameters(string connectionString)
            => ConnectionStringParser.Matches(connectionString ?? throw new ArgumentNullException(nameof(connectionString)))
                                     .ToDictionary(
                                        keySelector: match => match.Groups["Key"].Value.Trim(),
                                        elementSelector: match => match.Groups["Value"].Value.Trim(),
                                        comparer: StringComparer.OrdinalIgnoreCase);
    }
}
