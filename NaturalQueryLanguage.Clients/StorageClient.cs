using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace NaturalQueryLanguage.Clients;

public class StorageClient(IConfiguration configuration)
{
    private readonly BlobServiceClient _blobService = new(configuration.GetConnectionString("StorageClient"));

    public async Task<Stream> OpenReadAsync(string containerName, string blobName)
    {
        var container = await CreateContainerIfNotExists(containerName);
        var blob = container.GetBlobClient(blobName);
        return await blob.OpenReadAsync();
    }

    public async Task<Stream> OpenWriteAsync(string containerName, string blobName)
    {
        var container = await CreateContainerIfNotExists(containerName);
        var blob = container.GetBlobClient(blobName);
        return await blob.OpenWriteAsync(true);
    }

    public async Task DeleteBlobAsync(string containerName, string blobName)
    {
        var container = await CreateContainerIfNotExists(containerName);
        await container.DeleteBlobIfExistsAsync(blobName, DeleteSnapshotsOption.IncludeSnapshots);
    }

    private async Task<BlobContainerClient> CreateContainerIfNotExists(string containerName)
    {
        var container = _blobService.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();
        return container;
    }
}
