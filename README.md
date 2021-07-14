# Umbraco.StorageProviders

Umbraco Azure Storage Providers

## Azure Blob

The Azure Blob Storage Provider has an implementation of Umbraco `IFileSystem`
that connects to an Azure Blob Container instead of the physical filesystem.

It also has the following features:

- a middleware for serving media files from the `/media` path
- ImageSharp Blob image provider
- ImageSharp Blob image cache
- a CDN Media UrlProvider

### Usage

The Media FileSystem, CDN UrlProvider and middleware can be enabled in the `Startup.cs` file:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddUmbraco(Environment, Configuration)
        .AddBackOffice()
        .AddWebsite()
        .AddComposers()

        .AddAzureBlobMediaFileSystem() // add the Azure Blob Media FileSystem, and the ImageSharp providers

        .AddCdnMediaUrlProvider() // (optional) add the CDN Media UrlProvider

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
            u.WithBackOffice();
            u.WithWebsite();

            u.WithAzureBlobMediaFileSystem(); // enables the Azure Blob Media FileSystem middleware

        })
        .WithEndpoints(u =>
        {
            u.UseInstallerEndpoints();
            u.UseBackOfficeEndpoints();
            u.UseWebsiteEndpoints();
        });
}
```

There's multiple ways co configure the blob provider it can be done in code

```csharp
services.AddUmbraco(Environment, Configuration)

    .AddAzureBlobMediaFileSystem(options => {
        options.ConnectionString = "";
        options.ContainerName = "";
    })

    .AddCdnMediaUrlProvider(options => {
        options.Url = new Uri("https://my-cdn.example.com/");
    });

```

or in `appSettings.json`

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

or by environment variables

```sh
UMBRACO__STORAGE__AZUREBLOB__MEDIA__CONNECTIONSTRING=<CONNECTION_STRING>
UMBRACO__STORAGE__AZUREBLOB__MEDIA__CONTAINERNAME=<CONTAINER_NAME>
UMBRACO__STORAGE__AZUREBLOB__MEDIA__CDN__URL=<CDN_BASE_URL>
```

## Bugs, issues and Pull Requests

If you encounter a bug when using this client library you are welcome to open an issue in the issue tracker of this repository. We always welcome Pull Request and please feel free to open an issue before submitting a Pull Request to discuss what you want to submit.

Questions about usage should be posted to the forum on [our.umbraco.com](https://our.umbraco.com).

## License

Umbraco Storage Providers is [MIT licensed](LICENSE).