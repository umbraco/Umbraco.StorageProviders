**Umbraco Storage Providers** is a set of Umbraco packages that extend the core functionality of the CMS to configure custom CDN media URLs and replace the default physical file storage with Azure Blob Storage.

The following packages are available and all require code changes and configuration (follow the instructions provided in the documentation):
- `Umbraco.StorageProviders` - Contains shared storage providers infrastructure, like a CDN media URL provider.
- `Umbraco.StorageProviders.AzureBlob` - The Azure Blob Storage provider has an implementation of the Umbraco `IFileSystem` that connects to an Azure Blob Storage container.
- `Umbraco.StorageProviders.AzureBlob.ImageSharp` - Adds ImageSharp support for storing the image cache to a pre-configured Azure Blob Storage provider.
