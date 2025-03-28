# Integrating .NET Aspire 9.1 with Azure Storage Account and Blob Containers

In this post, we’ll walk through how to create an Azure Storage Account and a Blob Storage Container using .NET Aspire 9.1

You’ll learn how to define and provision these resources as part of a distributed application

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

This C# code defines a distributed application using .NET Aspire, specifically targeting Azure services with infrastructure provisioning (Azure Storage Account and Blob Storage) support

This code:

a) Provisions an Azure Storage Account with a blob container

b) Assigns necessary permissions

c) Ensures security settings

d) Connects the storage to an application project (azurestorage-consumer)

e) Uses Aspire to define, provision, and orchestrate the whole setup

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

Here is a more detailed explanation about the infrastructure configuration (Azure Storage Account provisioning configuration) code:

```csharp
var storage = builder.AddAzureStorage("storage")
    .ConfigureInfrastructure((infra) =>
    {
        var storageAccount = infra.GetProvisionableResources()
                                  .OfType<StorageAccount>()
                                  .Single();

        // Set the Storage Account name property
        storageAccount.Name = "luiscontainer";

        // Storage Account Contributor and Storage Blob Data Owner roles are required by the Azure Functions host
        var principalTypeParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalType, typeof(string));
        var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
        infra.Add(storageAccount.CreateRoleAssignment(StorageBuiltInRole.StorageAccountContributor, principalTypeParameter, principalIdParameter));
        infra.Add(storageAccount.CreateRoleAssignment(StorageBuiltInRole.StorageBlobDataOwner, principalTypeParameter, principalIdParameter));

        // Ensure that public access to blobs is disabled
        storageAccount.AllowBlobPublicAccess = false;
    });
```

The above code:

a) Adds a Storage Account to the app’s infrastructure.

b) Sets its name.

c) Grants it appropriate RBAC roles (Contributor + Data Owner) for a dynamic identity.

d) Secures it by disabling public blob access.

We also have to set the **AppHost project secrets**

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

## 5. We create a C# console project inside the solution

![image](https://github.com/user-attachments/assets/65f5c7dc-115c-41a0-bd06-5155a8bc93e8)

## 6. We load the Nuget packages in C# console project

![image](https://github.com/user-attachments/assets/f3934cd6-8e0a-451b-801d-4589fa894bd4)

We add the **.NET Aspire Orchestrator Support** in the Console application

![image](https://github.com/user-attachments/assets/e8f793a3-69b2-4c28-983c-b0af4d11be1e)

We confirm the Console project was added as reference in the AppHost project

![image](https://github.com/user-attachments/assets/2bc59168-c7bd-488c-bea7-ed096b998c4b)

We also has to add the ServiceDefaults project as reference in the Console project

![image](https://github.com/user-attachments/assets/0dde9499-1aa0-4633-b000-3118a9a3632a)

## 7. We configure the appsettings.json file

```json
{
  "ConnectionStrings": {
    "blobs": "https://luiscontainer.blob.core.windows.net/"
  }
}
```

## 8. We input the C# Console application source code

This code is a simple .NET console application using the .NET Aspire style to interact with Azure Blob Storage

This is a minimal Aspire-style .NET app that:

a) Automatically wires up Azure Blob Storage using Aspire bindings.

**AddAzureBlobClient("blobs")**: Registers a BlobServiceClient using a named binding called "blobs"—configured via Aspire's service discovery and binding system.

b) Creates a container if it doesn't exist.

c) Uploads a test blob.

d) Lists all blobs.

e) Runs as a background service using IHostedService.

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

![image](https://github.com/user-attachments/assets/f0d43848-b468-47e6-85ff-e234288e63f3)

If we select in the Console icon in the left hand side menu you can see the project logs

![image](https://github.com/user-attachments/assets/3e138917-d5f3-4303-a278-44484d88f403)

We now login in Azure Portal and navigate to the Storage Accounts

We can see a new Storage Account was created

![image](https://github.com/user-attachments/assets/a69ed086-fa92-4eea-8b5d-b220eda089a4)

If we click on the Storage Account name we can verify a new Blob Container was also created

![image](https://github.com/user-attachments/assets/05c88b44-a7c6-4daf-b55a-979bfa75a9d9)

If we click on the Blob name we can verify how a new file was uploaded inside it

![image](https://github.com/user-attachments/assets/f090b0d6-5805-4800-bc0d-7a3c175e0b02)

If we click on the file name we can select the download option and see the file content

![image](https://github.com/user-attachments/assets/99d77dc2-43aa-4adb-985f-a4253a533edf)

After downloaded the file we have to see the file content

![image](https://github.com/user-attachments/assets/55e17350-2f97-4a97-8734-a9e2cdd22a2e)

![image](https://github.com/user-attachments/assets/8f108e76-2452-4353-bc13-20f8f491d7e3)






