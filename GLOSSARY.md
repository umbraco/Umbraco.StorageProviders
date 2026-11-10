# Umbraco Storage Providers

Lets an Umbraco site keep its files (media, and the ImageSharp image cache) in Azure Blob Storage instead of on local disk.

## Language

### File systems

**File system**:
A named Azure Blob Storage location that Umbraco reads and writes files through, configured under `Umbraco:Storage:AzureBlob:{name}`. A site can have several.
_Avoid_: storage provider, blob storage (for the configured unit)

**Media file system**:
The file system named `Media`, which Umbraco uses for its media library.
_Avoid_: default file system

### Clients

**Container client**:
The connection to one blob container in a storage account. Every file system works through exactly one container client.
_Avoid_: blob client, container (for the connection)

**Service client**:
The connection to a whole storage account, from which a container client is obtained by container name.
_Avoid_: account client, storage client

**Supplied client**:
A container client or service client that the site builds itself (often registered in dependency injection, for example by .NET Aspire) and hands to a file system, instead of the file system creating its own. A supplied client is used exactly as given: the file system's retry settings do not apply to it.
_Avoid_: external client, injected client, DI client

**Connection string**:
The setting a file system uses to create its own container client. Only needed when no client is supplied.
