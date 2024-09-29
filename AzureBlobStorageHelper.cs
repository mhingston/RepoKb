using Azure.Storage.Blobs;

namespace RepoKb;

public class AzureBlobStorageHelper
{
    public static async Task UploadQdrantDataToBlobStorage(string connectionString, string containerName, string qdrantStoragePath)
    {
        Console.WriteLine("Uploading Qdrant data to Azure Blob Storage...");

        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        var directory = new DirectoryInfo(qdrantStoragePath);
        foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories))
        {
            var blobClient = containerClient.GetBlobClient(file.FullName.Replace(qdrantStoragePath + "\\", ""));
            await blobClient.UploadAsync(file.FullName, true);
            Console.WriteLine($"Uploaded {file.FullName}");
        }

        Console.WriteLine("Qdrant data uploaded to Azure Blob Storage.");
    }

    public static async Task DownloadQdrantDataFromBlobStorage(string connectionString, string containerName, string qdrantStoragePath)
    {
        Console.WriteLine("Downloading Qdrant data from Azure Blob Storage...");

        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            var localFilePath = Path.Combine(qdrantStoragePath, blobItem.Name);

            // Create directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));

            await blobClient.DownloadToAsync(localFilePath);
            Console.WriteLine($"Downloaded {blobItem.Name}");
        }

        Console.WriteLine("Qdrant data downloaded from Azure Blob Storage.");
    }
    
    public static async Task ClearBlobStorageContainer(string connectionString, string containerName)
    {
        Console.WriteLine($"Clearing Blob Storage container: {containerName}...");

        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            await containerClient.DeleteBlobAsync(blobItem.Name);
            Console.WriteLine($"Deleted blob: {blobItem.Name}");
        }

        Console.WriteLine($"Blob Storage container '{containerName}' cleared.");
    }
}