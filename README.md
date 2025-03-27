# How to integrate .NET 9.1 Aspire with Azure Storage Account and Blob 

In this post we are going to create an Azure Storage Account and a Blob Storage Container from .NET Aspire

## 1. Prerrequisites

### 1.1. Install .NET 9

Visit this web site (https://dotnet.microsoft.com/es-es/download/dotnet/9.0) and download **Windows x64 SDK 9.0.202**

![image](https://github.com/user-attachments/assets/87e72641-7c88-4839-9bdb-91f64568c20a)

### 1.2. Install Visual Studio 2022 v17.3 Community Edition

https://visualstudio.microsoft.com/downloads/

![image](https://github.com/user-attachments/assets/653307c3-fe36-43c0-ac29-505d4dead3dd)

### 1.3. Run these commands to configure Azure 

We first log in Azure

```
az login
```

![image](https://github.com/user-attachments/assets/ff2e6b77-1656-47a9-a56f-d337d8063ffd)

![image](https://github.com/user-attachments/assets/53bc1554-751c-4699-8d43-04c2683f01f6)

We verify Azure account information

```
az account show
```

![image](https://github.com/user-attachments/assets/054f9148-3b93-4563-8dd5-72c34f25a5d2)

## 2. Create a new .NET Aspire Empty App

We run Vistual Studio 2022 and create a new project

We select the project template for .NET Aspire Empty Application

![image](https://github.com/user-attachments/assets/27487f4a-fd85-43cf-92aa-da548b6d0a6e)

We input the project name and location

We select the .NET 9 or .NET 10 framework and press on the Create button

This is the solution structure

![image](https://github.com/user-attachments/assets/867888b3-4a38-48ff-93e3-00aaba7cff86)

## 3. We load the Nuget packages in the AppHost project

![image](https://github.com/user-attachments/assets/da7581d7-7948-41ec-9943-2198d0a176b9)

## 4. AppHost project source code

**Program.cs**

```csharp
using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
    .ConfigureInfrastructure((infra) =>
    {
        var storageAccount = infra.GetProvisionableResources()
                                  .OfType<StorageAccount>()
                                  .Single();
        storageAccount.Name = "luiscontainer";
        // Storage Account Contributor and Storage Blob Data Owner roles are required by the Azure Functions host
        var principalTypeParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalType, typeof(string));
        var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
        infra.Add(storageAccount.CreateRoleAssignment(StorageBuiltInRole.StorageAccountContributor, principalTypeParameter, principalIdParameter));
        infra.Add(storageAccount.CreateRoleAssignment(StorageBuiltInRole.StorageBlobDataOwner, principalTypeParameter, principalIdParameter));

        // Ensure that public access to blobs is disabled
        storageAccount.AllowBlobPublicAccess = false;
    });

var blobs = storage.AddBlobs("blobs");

builder.AddProject<Projects.AzureStorage_Consumer>("azurestorage-consumer").WithReference(blobs).WaitFor(blobs);

builder.Build().Run();
```

We also have to set the AppHost project secrets

We right click on the AppHost project name and select the menu option **Manage User Secrets**

![image](https://github.com/user-attachments/assets/5499aa75-1d18-4d77-bc18-01d64522a685)

We input the secrets in the **secrets.json** file:

```
{
  "Azure": {
    "SubscriptionId": "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
    "AllowResourceGroupCreation": true,
    "ResourceGroup": "luispruebamyRG",
    "Location": "westeurope",
    "CredentialSource": "Default",
    "Tenant": "luiscocoenriquezhotmail.onmicrosoft.com"
  }
}
```

## 5. We create a C# console application inside the solution

![image](https://github.com/user-attachments/assets/65f5c7dc-115c-41a0-bd06-5155a8bc93e8)

## 6. We load the Nuget packages

![image](https://github.com/user-attachments/assets/f3934cd6-8e0a-451b-801d-4589fa894bd4)

We add the **.NET Aspire Orchestrator Support** in the Console application

![image](https://github.com/user-attachments/assets/e8f793a3-69b2-4c28-983c-b0af4d11be1e)

We confirm the Console project was added as reference in the AppHost project

![image](https://github.com/user-attachments/assets/2bc59168-c7bd-488c-bea7-ed096b998c4b)

## 7. We configure the appsettings.json file

```json
{
  "ConnectionStrings": {
    "blobs": "https://luiscontainer.blob.core.windows.net/"
  }
}
```

## 8. We input the C# Console application source code

**Program.cs**

```csharp
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
```

## 9. We run the appliction and verify the results 

Before running the application we have to set the **AppHost project** as the **StartUp Project**

![image](https://github.com/user-attachments/assets/b8c59d8e-59d6-42b2-ac53-6920ee65f0ee)

We run the application and automatically we are redirect to the Aspire Dashboard

![image](https://github.com/user-attachments/assets/adc2b29c-d8df-4b4e-ba82-19025125f6eb)

We also can see the application logs in the console output




