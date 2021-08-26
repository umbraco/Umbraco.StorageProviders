# Umbraco.StorageProviders

This repository contains Umbraco storage providers that can replace the default physical file storage.

## Umbraco.StorageProviders.AzureBlob

The Azure Blob Storage provider has an implementation of the Umbraco `IFileSystem` that connects to an Azure Blob Storage container.

It also has the following features:
- middleware for serving media files from the `/media` path
- ImageSharp image provider/cache
- a CDN media URL provider

### Usage

This provider can be added in the `Startup.cs` file:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddUmbraco(_env, _config)
        .AddBackOffice()
        .AddWebsite()
        .AddComposers()
        // Add the Azure Blob Storage file system, ImageSharp image provider/cache and middleware for Media:
        .AddAzureBlobMediaFileSystem() 
        // Optionally add the CDN media URL provider:
        .AddCdnMediaUrlProvider()
        .Build();
}

public void Configure(IApplicationBuilder app)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseUmbraco()
        .WithMiddleware(u =>
        {
            u.UseBackOffice();
            u.UseWebsite();
            // Enables the Azure Blob Storage middleware for Media:
            u.UseAzureBlobMediaFileSystem();

        })
        .WithEndpoints(u =>
        {
            u.UseInstallerEndpoints();
            u.UseBackOfficeEndpoints();
            u.UseWebsiteEndpoints();
        });
}
```

There're multiple ways to configure the provider, it can be done in code:

```csharp
services.AddUmbraco(_env, _config)

    .AddAzureBlobMediaFileSystem(options => {
        options.ConnectionString = "";
        options.ContainerName = "";
    })

    .AddCdnMediaUrlProvider(options => {
        options.Url = new Uri("https://my-cdn.example.com/");
    });

```

In `appsettings.json`:

```json
{
  "Umbraco": {
    "Storage": {
      "AzureBlob": {
        "Media": {
          "ConnectionString": "",
          "ContainerName": "",
          "Cdn": {
            "Url": ""
          }
        }
      }
    }
  }
}
```

Or by environment variables:

```sh
UMBRACO__STORAGE__AZUREBLOB__MEDIA__CONNECTIONSTRING=<CONNECTION_STRING>
UMBRACO__STORAGE__AZUREBLOB__MEDIA__CONTAINERNAME=<CONTAINER_NAME>
UMBRACO__STORAGE__AZUREBLOB__MEDIA__CDN__URL=<CDN_BASE_URL>
```

_Note: you still have to add the provider in the `Startup.cs` file when not configuring the options in code._

## Folder structure in the Azure Blob Storage container
The container name is expected to exist and uses the following folder structure:
- `/media` - contains the Umbraco media, stored in the structure defined by the `IMediaPathScheme` registered in Umbraco (the default `UniqueMediaPathScheme` stores files with their original filename in 8 character directories, based on the content and property GUID identifier)
- `/cache` - contains the ImageSharp image cache, stored as files with a filename defined by the `ICacheHash` registered in ImageSharp (the default `CacheHash` generates SHA256 hashes of the file contents and uses the first characters configured by the `Umbraco:CMS:Imaging:CachedNameLength` setting)

Note that this is different than the behavior of other file system providers - i.e. https://github.com/umbraco-community/UmbracoFileSystemProviders.Azure that expect the media contents to be at the root level.

## Bugs, issues and Pull Requests

If you encounter a bug when using this client library you are welcome to open an issue in the issue tracker of this repository. We always welcome Pull Request and please feel free to open an issue before submitting a Pull Request to discuss what you want to submit.

Questions about usage should be posted to the forum on [our.umbraco.com](https://our.umbraco.com).

## License

Umbraco Storage Providers is [MIT licensed](LICENSE).