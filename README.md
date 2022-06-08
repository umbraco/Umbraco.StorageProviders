# Umbraco storage providers
This repository contains Umbraco storage providers that can replace the default physical file storage.

## Umbraco.StorageProviders
Contains shared storage providers infrastructure, like a CDN media URL provider.

### Usage
This provider can be added in the `Startup.cs` file:
```diff
public void ConfigureServices(IServiceCollection services)
{
    services.AddUmbraco(_env, _config)
        .AddBackOffice()
        .AddWebsite()
        .AddComposers()
+       .AddCdnMediaUrlProvider()
        .Build();
}
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

_Note: you still have to add the provider in the `Startup.cs` file when not configuring the options in code._

### Configuration
Configure your CDN origin to point to your site and ensure every unique URL is cached (includes the query string), so images can be processed by the site (using ImageSharp) and the response cached by the CDN.

By default, the CDN provider removes the media path (`/media`) from the generated media URL, so you need to configure your CDN origin to include this path. This is to prevent caching/proxying other parts of your site, but you can opt-out of this behavior by setting `RemoveMediaFromPath` to `false`.

## Umbraco.StorageProviders.AzureBlob
The Azure Blob Storage provider has an implementation of the Umbraco `IFileSystem` that connects to an Azure Blob Storage container.

### Usage
This provider can be added in the `Startup.cs` file:
```diff
public void ConfigureServices(IServiceCollection services)
{
    services.AddUmbraco(_env, _config)
        .AddBackOffice()
        .AddWebsite()
        .AddComposers()
+       .AddAzureBlobMediaFileSystem() 
        .Build();
}
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

_Note: you still have to add the provider in the `Startup.cs` file when not configuring the options in code._

### Folder structure in the Azure Blob Storage container
The container name is expected to exist and uses the following folder structure:
- `/media` - contains the Umbraco media, stored in the structure defined by the `IMediaPathScheme` registered in Umbraco (the default `UniqueMediaPathScheme` stores files with their original filename in 8 character directories, based on the content and property GUID identifier)
- `/cache` - contains the ImageSharp image cache, stored as files with a filename defined by the `ICacheHash` registered in ImageSharp (the default `CacheHash` generates SHA256 hashes of the file contents and uses the first characters configured by the `Umbraco:CMS:Imaging:CacheHashLength` setting)

Note that this is different than the behavior of other file system providers - i.e. https://github.com/umbraco-community/UmbracoFileSystemProviders.Azure that expect the media contents to be at the root level.

## Using the file system providers
Please refer to our documentation on [using custom file systems](https://our.umbraco.com/documentation/Extending/FileSystemProviders/).

## Bugs, issues and Pull Requests
If you encounter a bug when using this client library you are welcome to open an issue in the issue tracker of this repository. We always welcome Pull Request and please feel free to open an issue before submitting a Pull Request to discuss what you want to submit.

Questions about usage should be posted to the forum on [our.umbraco.com](https://our.umbraco.com).

## License
Umbraco Storage Providers is [MIT licensed](LICENSE).
