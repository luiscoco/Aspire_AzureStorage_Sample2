using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Azure.Storage.Blobs;
using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;

var builder = Host.CreateApplicationBuilder(args);

// Aspire-style service wiring
builder.AddServiceDefaults();

// Register Azure Blob client using binding name (from Aspire)
builder.AddAzureBlobClient("blobs");

// Register your hosted service that consumes the BlobServiceClient
builder.Services.AddHostedService<BlobWorker>();

var app = builder.Build();
app.Run();

// === Worker Service (DI entry point like a controller) ===

public class BlobWorker : IHostedService
{
    private readonly BlobServiceClient _client;

    public BlobWorker(BlobServiceClient client)
    {
        _client = client;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting BlobWorker...");

        var container = _client.GetBlobContainerClient("luiscocoblobthird");
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        //var blobServiceClient = new BlobServiceClient(
        //new Uri("https://" + "storage6mkenb7s33vbg" + ".blob.core.windows.net"),
        //new DefaultAzureCredential());

        //var container = blobServiceClient.GetBlobContainerClient("mycontainerluisprueba999");
        //await container.CreateIfNotExistsAsync();

        var blobNameAndContent = Guid.NewGuid().ToString();

        await container.UploadBlobAsync(blobNameAndContent, new BinaryData("Sample blob content"), cancellationToken);
        Console.WriteLine($"Uploaded blob: {blobNameAndContent}");

        await foreach (var blob in container.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            Console.WriteLine($"- {blob.Name}");
        }

        Console.WriteLine("BlobWorker finished.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
