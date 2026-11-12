# Read streams reuse the file's metadata instead of calling OpenRead

`AzureBlobItemInfo.CreateReadStream()` used to return `BlobClient.OpenRead()`. Before it returns a stream, `OpenRead()` always makes a synchronous `GetProperties` request to get the blob's ETag and length ([#98](https://github.com/umbraco/Umbraco.StorageProviders/issues/98)). The `AzureBlobItemInfo` already holds those properties, often from the metadata cache, so every media response paid a second, blocking round trip that the cache couldn't remove. We decided that the read stream is built from the properties the item already has: creating it makes no request, and the first read starts one asynchronous download (`DownloadStreamingAsync`) from the current position, with `If-Match` set to the item's ETag. Seeking closes the open download, and the next read starts a new one from the new position.

## Considered options

- **Keep `OpenRead()` and pass the known ETag in `BlobOpenReadOptions`.** Doesn't help: `OpenRead()` makes the `GetProperties` request whatever options it's given.
- **Call `DownloadStreaming()` in `CreateReadStream()`.** Replaces the HEAD with a GET, but still makes a synchronous round trip on the request thread, because `IFileInfo.CreateReadStream()` is synchronous.
- **Don't send `If-Match`, so a replaced blob is served as its current version.** Rejected: the response headers (`Content-Length`, `Last-Modified`, `ETag`) come from the item's metadata. A body from a different version would be truncated or overrun, or served under the old validators and cached as the old version.

## Consequences

- A media response makes no synchronous blob requests once the metadata is cached, and one asynchronous request for the content.
- A blob replaced outside this instance while its metadata is still cached fails to download (`412 Precondition Failed`) until the entry expires, up to the configured hit duration. Through `StaticFileMiddleware` the request fails with a 500 before any of the body is sent. Before, the same window sent the new content under the old `Content-Length`, so a replacement of a different size produced a truncated or aborted response.
- Callers that read synchronously still block, on the download instead of on `GetProperties`.
- A whole blob downloads in one request, where `OpenRead()` fetched it in 4 MB ranges. The SDK's retriable stream resumes from the current offset, under the same `If-Match`, if the connection drops partway.
- A download isn't limited to a range length, so a range request that reads only part of a blob closes its download early. `OpenRead()` also over-fetched, up to 4 MB past the start of the range.
