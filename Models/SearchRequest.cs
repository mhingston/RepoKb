using System.ComponentModel;

namespace RepoKb.Models;

public record SearchRequest : BaseRequest
{
    [DefaultValue(10)]
    public int Limit { get; set; } = 10;
}