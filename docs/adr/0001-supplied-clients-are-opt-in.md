# Supplied clients are opt-in per file system

Sites that already register a `BlobContainerClient` or `BlobServiceClient` in dependency injection (for example through .NET Aspire, which adds telemetry and health checks) need a way for a file system to use it instead of creating its own from a connection string ([#79](https://github.com/umbraco/Umbraco.StorageProviders/issues/79)). We decided that a file system only uses such a client when it is explicitly handed one through `UseBlobContainerClient` or `UseBlobServiceClient` on its options. The file system never picks a client up from dependency injection automatically: a site can register a container client for something unrelated, and media would then silently start writing to that container. An explicit call per file system also works naturally with named file systems and keyed services, and it needs no new builder API.

## Considered options

- **Use a registered client automatically when one exists.** Rejected: it couples the file system to whatever else the site registers, and changes where files are written without any change to this package's configuration.
- **A builder shortcut that resolves the client from services** (e.g. `UseBlobContainerClientFromServices(serviceKey)`). Not needed now: the existing configure overloads that receive a dependency or the `IServiceProvider` already do this in one line. It can be added later without breaking anything.

## Consequences

- A supplied client is used as given, so the file system's `Retry` settings don't apply to it.
- `ConnectionString` is only required when the file system creates its own client, and `ContainerName` is only required unless a container client is supplied. When both a supplied container client and a configured `ContainerName` are present, they must match.
