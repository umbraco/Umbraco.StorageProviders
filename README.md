# Umbraco storage providers
This repository contains Umbraco storage providers that can replace the default physical file storage.

> **Note**
> Use the following documentation for previous Umbraco CMS versions:
> * [Umbraco CMS 15](https://github.com/umbraco/Umbraco.StorageProviders/blob/support/15.x/README.md)
> * [Umbraco CMS 14](https://github.com/umbraco/Umbraco.StorageProviders/blob/support/14.x/README.md)
> * [Umbraco CMS 13](https://github.com/umbraco/Umbraco.StorageProviders/blob/support/13.x/README.md)
> * [Umbraco CMS 12](https://github.com/umbraco/Umbraco.StorageProviders/blob/support/12.0.x/README.md)
> * [Umbraco CMS 11](https://github.com/umbraco/Umbraco.StorageProviders/blob/support/11.0.x/README.md)
> * [Umbraco CMS 10 - v10 (aligned with CMS major version from now on)](https://github.com/umbraco/Umbraco.StorageProviders/blob/support/10.0.x/README.md)
> * [Umbraco CMS 9 - v1](https://github.com/umbraco/Umbraco.StorageProviders/blob/support/1.1.x/README.md)

## Umbraco.StorageProviders
Contains shared storage providers infrastructure, like a CDN media URL provider.

### Usage
This provider can be added in the `Program.cs` file:
```diff
builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
+   .AddCdnMediaUrlProvider()
    .Build();
```

There are multiple ways to configure the CDN provider. It can be done in code (replacing the code added above):
```csharp
.AddCdnMediaUrlProvider(options => {
    options.Url = new Uri("https://cdn.example.com/");
    options.RemoveMediaFromPath = true;
});
```

In `appsettings.json`:
```json
{
  "Umbraco": {
    "Storage": {
      "Cdn": {
        "Url": "https://cdn.example.com/",
        "RemoveMediaFromPath": true
      }
    }
  }
}
```

Or by environment variables:
```sh
UMBRACO__STORAGE__CDN__URL=https://cdn.example.com/
UMBRACO__STORAGE__CDN__REMOVEMEDIAFROMPATH=true
```

> **Note**
> You still have to add the provider in the `Program.cs` file when not configuring the options in code.

### Configuration
Configure your CDN origin to point to your site and ensure every unique URL is cached (includes the query string), so images can be processed by the site and the response cached by the CDN.

By default, the CDN provider removes the media path (`/media`) from the generated media URL, so you need to configure your CDN origin to include this path. This is to prevent caching/proxying other parts of your site, but you can opt-out of this behavior by setting `RemoveMediaFromPath` to `false`.

## Umbraco.StorageProviders.AzureBlob
The Azure Blob Storage provider has an implementation of the Umbraco `IFileSystem` that connects to an Azure Blob Storage container.

### Usage
This provider can be added in the `Program.cs` file:
```diff
builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
+   .AddAzureBlobMediaFileSystem()
    .Build();
```

There are multiple ways to configure the provider. It can be done in code (replacing the code added above):
```csharp
.AddAzureBlobMediaFileSystem(options => {
    options.ConnectionString = "UseDevelopmentStorage=true";
    options.ContainerName = "sample-container";
})
```

In `appsettings.json`:
```json
{
  "Umbraco": {
    "Storage": {
      "AzureBlob": {
        "Media": {
          "ConnectionString": "UseDevelopmentStorage=true",
          "ContainerName": "sample-container"
        }
      }
    }
  }
}
```

Or by environment variables:
```sh
UMBRACO__STORAGE__AZUREBLOB__MEDIA__CONNECTIONSTRING=UseDevelopmentStorage=true
UMBRACO__STORAGE__AZUREBLOB__MEDIA__CONTAINERNAME=sample-container
```

> **Note**
> You still have to add the provider in the `Program.cs` file when not configuring the options in code.

### Custom blob container options
To override the default blob container options, you can use the following extension methods on `AzureBlobFileSystemOptions`:
```csharp
// Add using default options (overly verbose, but shows how to revert back to the default)
.AddAzureBlobMediaFileSystem(options => options.CreateBlobContainerClientUsingDefault())
// Add using options
.AddAzureBlobMediaFileSystem(options => options.CreateBlobContainerClientUsingOptions(_blobClientOptions))
// If the connection string is parsed to a URI, use the delegate to create a BlobContainerClient
.AddAzureBlobMediaFileSystem(options => options.TryCreateBlobContainerClientUsingUri(uri => new BlobContainerClient(uri, _blobClientOptions)))
```

This can also be used together with the `Azure.Identity` package to authenticate with Azure AD (using managed identities):
```csharp
using Azure.Identity;
using Azure.Storage.Blobs;
using Umbraco.Cms.Core.Composing;
using Umbraco.StorageProviders.AzureBlob.IO;

internal sealed class AzureBlobFileSystemComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddAzureBlobMediaFileSystem(options =>
        {
            options.ConnectionString = "https://[storage-account].blob.core.windows.net";
            options.ContainerName = "media";
            options.TryCreateBlobContainerClientUsingUri(uri => new BlobContainerClient(uri, new DefaultAzureCredential()));
        });
}
```

## Umbraco.StorageProviders.AzureBlob.ImageSharp
Adds ImageSharp support for storing the image cache to a pre-configured Azure Blob Storage provider.

### Usage
This provider can be added in the `Program.cs` file:
```diff
builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .AddAzureBlobMediaFileSystem()
+   .AddAzureBlobImageSharpCache()
    .Build();
```

By default the media file system configuration will be used and files will be stored in a separate folder ([see below](#folder-structure-in-the-azure-blob-storage-container)). You can specify the name of another (already configured) Azure Blob file system to store the files in another container:
```csharp
.AddAzureBlobFileSystem("Cache")
.AddAzureBlobImageSharpCache("Cache")
```


## Folder structure in the Azure Blob Storage container
The container name is expected to exist and uses the following folder structure:
- `/media` - contains the Umbraco media, stored in the structure defined by the `IMediaPathScheme` registered in Umbraco (the default `UniqueMediaPathScheme` stores files with their original filename in 8 character directories, based on the content and property GUID identifier)
- `/cache` - contains the ImageSharp image cache, stored as files with a filename defined by the `ICacheHash` registered in ImageSharp (the default `CacheHash` generates SHA256 hashes of the file contents and uses the first characters configured by the `Umbraco:CMS:Imaging:CacheHashLength` setting)

> **Note**
> This is different than the behavior of other file system providers, i.e. [UmbracoFileSystemProviders.Azure](https://github.com/umbraco-community/UmbracoFileSystemProviders.Azure) that expect the media contents to be at the root level.

## Using the file system providers
Please refer to our documentation on [using custom file systems](https://docs.umbraco.com/umbraco-cms/extending/filesystemproviders).

## Bugs, issues and Pull Requests
If you encounter a bug when using this client library you are welcome to open an issue in the issue tracker of this repository. We always welcome Pull Request and please feel free to open an issue before submitting a Pull Request to discuss what you want to submit.

Questions about usage should be posted to the forum on [forum.umbraco.com](https://forum.umbraco.com).

## License
Umbraco Storage Providers is [MIT licensed](LICENSE).
