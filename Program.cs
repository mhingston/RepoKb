using System.Text.RegularExpressions;
using CommandLine;
using LibGit2Sharp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using RepoKb;
using RepoKb.Models;
using RepoKb.Models.Settings;

partial class Program
{
    private const string QdrantStorageFolder = "qdrant_storage";

    private static async Task Main(string[] args)
    {
        // Parse command line arguments
        await Parser.Default.ParseArguments<CliOptions>(args)
            .WithParsedAsync(async options =>
            {
                // Load configuration
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddUserSecrets<Program>()
                    .Build();

                // Bind settings
                var azureSettings = configuration.GetSection("Azure").Get<AzureSettings>()!;
                var qdrantSettings = configuration.GetSection("Qdrant").Get<QdrantSettings>()!;

                // Initialize Kernel Memory
                var memory = await InitializeKernelMemory(azureSettings, qdrantSettings);

                switch (options.Mode)
                {
                    case "index":
                        await HandleIndexMode(options, azureSettings, memory);
                        break;
                    case "search":
                        await HandleSearchMode(azureSettings, memory);
                        break;
                    default:
                        Console.WriteLine("Error: Invalid mode. Please specify either 'index' or 'search'.");
                        break;
                }
            });
    }

    private static async Task<IKernelMemory> InitializeKernelMemory(AzureSettings azureSettings, QdrantSettings qdrantSettings)
    {
        var chatModel = new AzureOpenAIConfig
        {
            APIKey = azureSettings.OpenAI.ApiKey,
            Endpoint = azureSettings.OpenAI.Endpoint,
            Deployment = azureSettings.OpenAI.Chat.Deployment,
            APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
            Auth = AzureOpenAIConfig.AuthTypes.APIKey,
            MaxTokenTotal = azureSettings.OpenAI.Chat.MaxTokens,
        };

        var embeddingModel = new AzureOpenAIConfig
        {
            APIKey = azureSettings.OpenAI.ApiKey,
            Endpoint = azureSettings.OpenAI.Endpoint,
            Deployment = azureSettings.OpenAI.Embedding.Deployment,
            APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
            Auth = AzureOpenAIConfig.AuthTypes.APIKey,
            MaxTokenTotal = azureSettings.OpenAI.Embedding.MaxTokens,
        };

        return new KernelMemoryBuilder()
            .WithAzureOpenAITextGeneration(chatModel)
            .WithAzureOpenAITextEmbeddingGeneration(embeddingModel)
            .WithQdrantMemoryDb(qdrantSettings.Url)
            .Build<MemoryServerless>();
    }

    private static async Task HandleIndexMode(CliOptions options, AzureSettings azureSettings, IKernelMemory memory)
    {
        if (string.IsNullOrEmpty(options.Path))
        {
            Console.WriteLine("Error: --path or -p is required when --mode or -m is index.");
            return;
        }
        
        await AzureBlobStorageHelper.ClearBlobStorageContainer(azureSettings.BlobStorage.ConnectionString, azureSettings.BlobStorage.ContainerName);
        
        var files = GetFiles(options.Path);
        
        // We're just doing this sequentially for now. Parallelization may cause us to hit rate limits.
        foreach (var file in files)
        {
            await IngestFile(memory, options.Path, file);
        }
        
        Console.WriteLine("Ingestion complete.");
        Console.WriteLine("\nPlease stop the Qdrant container now. Press Enter to continue once stopped.");
        Console.ReadLine();
        await AzureBlobStorageHelper.UploadQdrantDataToBlobStorage(azureSettings.BlobStorage.ConnectionString, azureSettings.BlobStorage.ContainerName, Path.Join(SourceDirectory, QdrantStorageFolder));
    }
    
    private static async Task IngestFile(IKernelMemory memory, string path, string file)
    {
        var fullPath = Path.Join(path, file);
        var documentId = string.Join("", AllowedCharacters().Matches(file));
        var content = await File.ReadAllTextAsync(fullPath);
        await memory.ImportTextAsync(content, documentId);

        // Wait for ingestion to complete
        while (!await memory.IsDocumentReadyAsync(documentId))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1500));
        }

        Console.WriteLine($"Imported {file}");
    }

    private static async Task HandleSearchMode(AzureSettings azureSettings, IKernelMemory memory)
    {
        if (Directory.Exists(Path.Join(SourceDirectory, QdrantStorageFolder)) && !Directory.EnumerateFiles(Path.Join(SourceDirectory, QdrantStorageFolder)).Any())
        {
            await AzureBlobStorageHelper.DownloadQdrantDataFromBlobStorage(azureSettings.BlobStorage.ConnectionString, azureSettings.BlobStorage.ContainerName, Path.Join(SourceDirectory, QdrantStorageFolder));
            Console.WriteLine("\nPlease start the Qdrant container now. Press Enter to continue once started.");
            Console.ReadLine();   
        }
        
        var builder = WebApplication.CreateBuilder();
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SchemaFilter<ApplyDefaultValues>(); 
        });
        
        var app = builder.Build();
        app.UseSwagger();
        app.UseSwaggerUI();
        
        app.UseHttpsRedirection();

        app.MapPost("/search", async (SearchRequest request, HttpContext context) =>
        {
            var searchResults = await memory.SearchAsync(request.Query, minRelevance: request.Relevance, limit: request.Limit);
            await context.Response.WriteAsJsonAsync(searchResults);
        });
        
        app.MapPost("/ask", async (BaseRequest request, HttpContext context) =>
        {
            var answer = await memory.AskAsync(request.Query, minRelevance: request.Relevance);
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(answer.Result);
        });

        await app.RunAsync();
    }
    
    private static string SourceDirectory => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));

    private static IEnumerable<string> GetFiles(string path)
    {
        var isDirectory = Directory.Exists(path);
        
        if(!isDirectory)
            throw new ArgumentException("Please provide a path to a directory.");
        
        var repo = new Repository(path);
        var ignoredPatterns = new[] { "yarn.lock", ".yarn", ".png", ".svg", ".ico", ".woff2" };
        return repo.Index
            .Select(f => f.Path)
            .Where(f => !ignoredPatterns.Any(f.Contains));
    }

    [GeneratedRegex(@"[A-Za-z0-9._-]+")]
    private static partial Regex AllowedCharacters();
}