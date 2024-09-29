namespace RepoKb.Models.Settings;

public record AzureSettings
{
    public OpenAISettings OpenAI { get; init; }
    public BlobStorageSettings BlobStorage { get; init; }
}