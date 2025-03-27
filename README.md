# How to integrate .NET 9.1 Aspire with Azure Storage Account and Blob 

In this post we are going to create an Azure Storage Account and a Blob Storage Container from .NET Aspire

## 1. Prerrequisites

### 1.1. Install .NET 9 or .NET 10


### 1.2. Install Visual Studio 2022 Community Edition v17.3 or v.17.4


## 2. Create a new .NET Aspire Empty App

We run Vistual Studio 2022 and create a new project

We select the project template for .NET Aspire Empty Application

![image](https://github.com/user-attachments/assets/27487f4a-fd85-43cf-92aa-da548b6d0a6e)

We input the project name and location

We select the .NET 9 or .NET 10 framework and press on the Create button

This is the solution structure

![image](https://github.com/user-attachments/assets/867888b3-4a38-48ff-93e3-00aaba7cff86)

## 3. We load the Nuget packages in the AppHost project






## 3. AppHost project source code

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

****



## 3. 




## Secrets

```
{
  "Azure": {
    "SubscriptionId": "3392e711-a640-4d25-8dc1-112db40f09dd",
    "AllowResourceGroupCreation": true,
    "ResourceGroup": "luispruebamyRG",
    "Location": "westeurope",
    "CredentialSource": "Default",
    "Tenant": "luiscocoenriquezhotmail.onmicrosoft.com"
  }
}
```
