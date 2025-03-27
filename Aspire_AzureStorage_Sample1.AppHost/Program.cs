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

//var builder = DistributedApplication.CreateBuilder(args);

//// Create the Azure Storage account with a specific name
//var storage = builder.AddAzureStorage("storage");

//// Create two blob containers
//var blobs = storage.AddBlobs("blobs");
//var luisblob = storage.AddBlobs("luisblob");

//// Reference both in the consumer project
//builder.AddProject<Projects.AzureStorage_Consumer>("azurestorage-consumer")
//       .WithReference(blobs)
//       .WithReference(luisblob);

//builder.Build().Run();


