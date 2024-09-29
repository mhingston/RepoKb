using System.ComponentModel;

namespace RepoKb.Models;

public record BaseRequest
{
    public string Query { get; set; }

    [DefaultValue(0.5)]
    public double Relevance { get; set; } = 0.5;
}