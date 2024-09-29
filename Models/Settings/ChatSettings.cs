namespace RepoKb.Models.Settings;

public record ChatSettings
{
    public string Deployment { get; init; }
    public int MaxTokens { get; init; }
}