namespace RepoKb.Models.Settings;

public record OpenAISettings
{
    public string ApiKey { get; init; }
    public string Endpoint { get; init; }
    public ChatSettings Chat { get; init; }
    public EmbeddingSettings Embedding { get; init; }
}