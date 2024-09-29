namespace RepoKb.Models.Settings;

public record BlobStorageSettings
{
    public string ConnectionString { get; init; }
    public string ContainerName { get; init; }
}