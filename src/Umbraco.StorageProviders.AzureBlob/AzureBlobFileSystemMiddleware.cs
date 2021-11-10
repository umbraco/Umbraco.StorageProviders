using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Umbraco.Cms.Core.Hosting;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.StorageProviders.AzureBlob
{
    /// <summary>
    /// The Azure Blob file system middleware.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Http.IMiddleware" />
    public class AzureBlobFileSystemMiddleware : IMiddleware
    {
        private readonly string _name;
        private readonly IAzureBlobFileSystemProvider _fileSystemProvider;
        private string _rootPath;
        private string _containerRootPath;
        private readonly TimeSpan? _maxAge = TimeSpan.FromDays(7);

        /// <summary>
        /// Creates a new instance of <see cref="AzureBlobFileSystemMiddleware" />.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="fileSystemProvider">The file system provider.</param>
        /// <param name="hostingEnvironment">The hosting environment.</param>

        public AzureBlobFileSystemMiddleware(IOptionsMonitor<AzureBlobFileSystemOptions> options, IAzureBlobFileSystemProvider fileSystemProvider, IHostingEnvironment hostingEnvironment)
            : this(AzureBlobFileSystemOptions.MediaFileSystemName, options, fileSystemProvider, hostingEnvironment)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFileSystemMiddleware" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <param name="fileSystemProvider">The file system provider.</param>
        /// <param name="hostingEnvironment">The hosting environment.</param>
        /// <exception cref="System.ArgumentNullException">options
        /// or
        /// hostingEnvironment
        /// or
        /// name
        /// or
        /// fileSystemProvider</exception>
        protected AzureBlobFileSystemMiddleware(string name, IOptionsMonitor<AzureBlobFileSystemOptions> options, IAzureBlobFileSystemProvider fileSystemProvider, IHostingEnvironment hostingEnvironment)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (hostingEnvironment == null) throw new ArgumentNullException(nameof(hostingEnvironment));

            _name = name ?? throw new ArgumentNullException(nameof(name));
            _fileSystemProvider = fileSystemProvider ?? throw new ArgumentNullException(nameof(fileSystemProvider));

            var fileSystemOptions = options.Get(name);
            _rootPath = hostingEnvironment.ToAbsolute(fileSystemOptions.VirtualPath);
            _containerRootPath = fileSystemOptions.ContainerRootPath ?? _rootPath;

            options.OnChange((o, n) => OptionsOnChange(o, n, hostingEnvironment));
        }

        /// <inheritdoc />
        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (next == null) throw new ArgumentNullException(nameof(next));

            return HandleRequestAsync(context, next);
        }

        private async Task HandleRequestAsync(HttpContext context, RequestDelegate next)
        {
            var request = context.Request;
            var response = context.Response;

            if (!context.Request.Path.StartsWithSegments(_rootPath, StringComparison.InvariantCultureIgnoreCase))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            string containerPath = $"{_containerRootPath.TrimEnd('/')}/{(request.Path.Value.Remove(0, _rootPath.Length)).TrimStart('/')}";
            var blob = _fileSystemProvider.GetFileSystem(_name).GetBlobClient(containerPath);

            var blobRequestConditions = GetAccessCondition(context.Request);

            Response<BlobProperties> properties;
            var ignoreRange = false;

            try
            {
                properties = await blob.GetPropertiesAsync(blobRequestConditions, context.RequestAborted).ConfigureAwait(false);
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                // the blob or file does not exist, let other middleware handle it
                await next(context).ConfigureAwait(false);
                return;
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.PreconditionFailed)
            {
                // If-Range or If-Unmodified-Since is not met
                // if the resource has been modified, we need to send the whole file back with a 200 OK
                // a Content-Range header is needed with the new length
                ignoreRange = true;
                properties = await blob.GetPropertiesAsync().ConfigureAwait(false);
                response.Headers.Append("Content-Range", $"bytes */{properties.Value.ContentLength}");
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotModified)
            {
                // If-None-Match or If-Modified-Since is not met
                // we need to pass the status code back to the client
                // so it knows it can reuse the cached data
                response.StatusCode = (int)HttpStatusCode.NotModified;
                return;
            }
            // for some reason we get an internal exception type with the message
            // and not a request failed with status NotModified :(
            catch (Exception ex) when (ex.Message == "The condition specified using HTTP conditional header(s) is not met.")
            {
                if (blobRequestConditions != null
                    && (blobRequestConditions.IfMatch.HasValue || blobRequestConditions.IfUnmodifiedSince.HasValue))
                {
                    // If-Range or If-Unmodified-Since is not met
                    // if the resource has been modified, we need to send the whole file back with a 200 OK
                    // a Content-Range header is needed with the new length
                    ignoreRange = true;
                    properties = await blob.GetPropertiesAsync().ConfigureAwait(false);
                    response.Headers.Append("Content-Range", $"bytes */{properties.Value.ContentLength}");
                }
                else
                {
                    // If-None-Match or If-Modified-Since is not met
                    // we need to pass the status code back to the client
                    // so it knows it can reuse the cached data
                    response.StatusCode = (int)HttpStatusCode.NotModified;
                    return;
                }
            }
            catch (TaskCanceledException)
            {
                // client cancelled the request before it could finish, just ignore
                return;
            }

            var responseHeaders = response.GetTypedHeaders();

            responseHeaders.CacheControl =
                new CacheControlHeaderValue
                {
                    Public = true,
                    MustRevalidate = true,
                    MaxAge = _maxAge,
                };

            responseHeaders.LastModified = properties.Value.LastModified;
            responseHeaders.ETag = new EntityTagHeaderValue($"\"{properties.Value.ETag}\"");
            responseHeaders.Append(HeaderNames.Vary, "Accept-Encoding");

            var requestHeaders = request.GetTypedHeaders();

            var rangeHeader = requestHeaders.Range;

            if (!ignoreRange && rangeHeader != null)
            {
                if (!ValidateRanges(rangeHeader.Ranges, properties.Value.ContentLength))
                {
                    // no ranges could be parsed
                    response.Clear();
                    response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
                    responseHeaders.ContentRange = new ContentRangeHeaderValue(properties.Value.ContentLength);
                    return;
                }

                if (rangeHeader.Ranges.Count == 1)
                {
                    var range = rangeHeader.Ranges.First();
                    var contentRange = GetRangeHeader(properties, range);

                    response.StatusCode = (int)HttpStatusCode.PartialContent;
                    response.ContentType = properties.Value.ContentType;
                    responseHeaders.ContentRange = contentRange;

                    await DownloadRangeToStreamAsync(blob, properties, response.Body, contentRange, context.RequestAborted).ConfigureAwait(false);
                    return;
                }

                if (rangeHeader.Ranges.Count > 1)
                {
                    // handle multipart ranges
                    var boundary = Guid.NewGuid().ToString();
                    response.StatusCode = (int)HttpStatusCode.PartialContent;
                    response.ContentType = $"multipart/byteranges; boundary={boundary}";

                    foreach (var range in rangeHeader.Ranges)
                    {
                        var contentRange = GetRangeHeader(properties, range);

                        await response.WriteAsync($"--{boundary}").ConfigureAwait(false);
                        await response.WriteAsync("\n").ConfigureAwait(false);
                        await response.WriteAsync($"{HeaderNames.ContentType}: {properties.Value.ContentType}").ConfigureAwait(false);
                        await response.WriteAsync("\n").ConfigureAwait(false);
                        await response.WriteAsync($"{HeaderNames.ContentRange}: {contentRange}").ConfigureAwait(false);
                        await response.WriteAsync("\n").ConfigureAwait(false);
                        await response.WriteAsync("\n").ConfigureAwait(false);

                        await DownloadRangeToStreamAsync(blob, properties, response.Body, contentRange, context.RequestAborted).ConfigureAwait(false);
                        await response.WriteAsync("\n").ConfigureAwait(false);
                    }

                    await response.WriteAsync($"--{boundary}--").ConfigureAwait(false);
                    await response.WriteAsync("\n").ConfigureAwait(false);
                    return;
                }
            }
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = properties.Value.ContentType;
            responseHeaders.ContentLength = properties.Value.ContentLength;
            responseHeaders.Append(HeaderNames.AcceptRanges, "bytes");

            await response.StartAsync().ConfigureAwait(false);
            await DownloadRangeToStreamAsync(blob, response.Body, 0L, properties.Value.ContentLength, context.RequestAborted).ConfigureAwait(false);
        }

        private static BlobRequestConditions? GetAccessCondition(HttpRequest request)
        {
            var range = request.Headers["Range"];
            if (string.IsNullOrEmpty(range))
            {
                // etag
                var ifNoneMatch = request.Headers["If-None-Match"];
                if (!string.IsNullOrEmpty(ifNoneMatch))
                {
                    return new BlobRequestConditions
                    {
                        IfNoneMatch = new ETag(ifNoneMatch)
                    };
                }

                var ifModifiedSince = request.Headers["If-Modified-Since"];
                if (!string.IsNullOrEmpty(ifModifiedSince))
                {
                    return new BlobRequestConditions
                    {
                        IfModifiedSince = DateTimeOffset.Parse(ifModifiedSince, CultureInfo.InvariantCulture)
                    };
                }
            }
            else
            {
                // handle If-Range header, it can be either an etag or a date
                // see https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-Range and https://tools.ietf.org/html/rfc7233#section-3.2
                var ifRange = request.Headers["If-Range"];
                if (!string.IsNullOrEmpty(ifRange))
                {
                    var conditions = new BlobRequestConditions();

                    if (DateTimeOffset.TryParse(ifRange, out var date))
                    {
                        conditions.IfUnmodifiedSince = date;
                    }
                    else
                    {
                        conditions.IfMatch = new ETag(ifRange);
                    }
                }

                var ifUnmodifiedSince = request.Headers["If-Unmodified-Since"];
                if (!string.IsNullOrEmpty(ifUnmodifiedSince))
                {
                    return new BlobRequestConditions
                    {
                        IfUnmodifiedSince = DateTimeOffset.Parse(ifUnmodifiedSince, CultureInfo.InvariantCulture)
                    };
                }
            }

            return null;
        }

        private static bool ValidateRanges(ICollection<RangeItemHeaderValue> ranges, long length)
        {
            if (ranges.Count == 0)
                return false;

            foreach (var range in ranges)
            {
                if (range.From > range.To)
                    return false;
                if (range.To >= length)
                    return false;
            }

            return true;
        }

        private static ContentRangeHeaderValue GetRangeHeader(BlobProperties properties, RangeItemHeaderValue range)
        {
            var length = properties.ContentLength - 1;

            long from;
            long to;
            if (range.To.HasValue)
            {
                if (range.From.HasValue)
                {
                    to = Math.Min(range.To.Value, length);
                    from = range.From.Value;
                }
                else
                {
                    to = length;
                    from = Math.Max(properties.ContentLength - range.To.Value, 0L);
                }
            }
            else if (range.From.HasValue)
            {
                to = length;
                from = range.From.Value;
            }
            else
            {
                to = length;
                from = 0L;
            }

            return new ContentRangeHeaderValue(from, to, properties.ContentLength);
        }

        private static async Task DownloadRangeToStreamAsync(BlobClient blob, BlobProperties properties,
            Stream outputStream, ContentRangeHeaderValue contentRange, CancellationToken cancellationToken)
        {
            var offset = contentRange.From.GetValueOrDefault(0L);
            var length = properties.ContentLength;

            if (contentRange.To.HasValue && contentRange.From.HasValue)
            {
                length = contentRange.To.Value - contentRange.From.Value + 1;
            }
            else if (contentRange.To.HasValue)
            {
                length = contentRange.To.Value + 1;
            }
            else if (contentRange.From.HasValue)
            {
                length = properties.ContentLength - contentRange.From.Value + 1;
            }

            await DownloadRangeToStreamAsync(blob, outputStream, offset, length, cancellationToken).ConfigureAwait(false);
        }

        private static async Task DownloadRangeToStreamAsync(BlobClient blob, Stream outputStream,
            long offset, long length, CancellationToken cancellationToken)
        {
            try
            {
                if (length == 0) return;
                var response = await blob.DownloadAsync(new HttpRange(offset, length), cancellationToken: cancellationToken).ConfigureAwait(false);
                await response.Value.Content.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // client cancelled the request before it could finish, just ignore
            }
        }

        private void OptionsOnChange(AzureBlobFileSystemOptions options, string name, IHostingEnvironment hostingEnvironment)
        {
            if (name != _name) return;

            _rootPath = hostingEnvironment.ToAbsolute(options.VirtualPath);
            _containerRootPath = options.ContainerRootPath ?? _rootPath;
        }
    }
}
