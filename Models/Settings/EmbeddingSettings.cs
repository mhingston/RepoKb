namespace RepoKb.Models.Settings;

public record EmbeddingSettings
{
    public string Deployment { get; init; }
    public int MaxTokens { get; init; }
}