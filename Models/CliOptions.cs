using CommandLine;

namespace RepoKb.Models;

public class CliOptions
{
    [Option('m', "mode", Required = true, HelpText = "Mode of operation (index or search)")]
    public string Mode { get; set; }

    [Option('p', "path", Required = false, HelpText = "Path to the repository to index")]
    public string Path { get; set; }
}