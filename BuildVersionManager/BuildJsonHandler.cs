using System.Text.Json;
using LibGit2Sharp;
using Microsoft.Extensions.Configuration;

namespace BuildVersionManager;

public class BuildJsonHandler
{
    private readonly RepositoryHandler _handler;
    private readonly string _versionPrefix;
    private readonly string _devBranch;
    private readonly Action<string>? _log;

    private record VersionData(int Major, int Minor, string[] Projects);
    
    public BuildJsonHandler(RepositoryHandler handler, string versionPrefix, string devBranch, Action<string>? log)
    {
        _handler = handler;
        _versionPrefix = versionPrefix;
        _devBranch = devBranch;
        _log = log;
    }

    public async Task NewRelease(string repositoryPath, string buildFile, int major, int minor)
    {
        var repository = new Repository(repositoryPath);
        var buildJson = Path.Combine(repositoryPath, buildFile);
        if (buildJson == buildFile)
        {
            throw new Exception("Path to build json must be relative to the repository path");
        }
        var sourceBranch = repository.Head.FriendlyName;
        if (!sourceBranch.InvariantEquals(_devBranch))
        {
            throw new Exception("A release must start from the dev branch");
        }
        var targetBranch = $"Release/{major}.{minor}";
        if (repository.Branches.Any(it => it.FriendlyName == targetBranch))
        {
            throw new Exception($"Branch {targetBranch} already exists");
        }

        var branch = repository.CreateBranch(targetBranch);

        var jsonObj = JsonSerializer.Deserialize<VersionData>(await File.ReadAllTextAsync(buildJson),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true });
        if (jsonObj == null) throw new Exception($"Invalid file: {buildJson}");

        var json = new VersionData(major, minor + 1, jsonObj.Projects);
        await File.WriteAllTextAsync(buildJson, JsonSerializer.Serialize(json, new JsonSerializerOptions
        {
            WriteIndented = true,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        }));
        repository.Index.Add(buildFile);
        repository.Commit($"Switched to version {json.Major}.{json.Minor}",
            new Signature(new Identity("A", "A@FCCL.DE"), DateTime.Now),
            new Signature(new Identity("A", "A@FCCL.DE"), DateTime.Now),
            new CommitOptions
            {
                
            });

        Commands.Checkout(repository, branch);
        await File.WriteAllTextAsync(buildJson, JsonSerializer.Serialize(
            new VersionData(major, minor, jsonObj.Projects), new JsonSerializerOptions
            {
                WriteIndented = true,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            }));
        await Process(buildJson);

    }

    public async Task Process(string buildJson)
    {
        var buildFile = new FileInfo(buildJson);
        var cfg = new ConfigurationBuilder()
            .AddJsonFile(buildFile.FullName)
            .AddEnvironmentVariables()
            .Build();
        var defMajorStr = cfg["Major"] ?? throw new Exception("No major version specified");
        var defMinorStr = cfg["Minor"] ?? throw new Exception("No minor version specified");
        if (!int.TryParse(defMajorStr, out var defMajor)) throw new Exception("Major is not an integer");
        if (!int.TryParse(defMinorStr, out var defMinor)) throw new Exception("Minor is not an integer");
        
        var repo = _handler.RepositoryFromBuildFile(buildFile.FullName);
        var version = _handler.VersionInfoFromRepository(
            repo,
            defMajor, 
            defMinor);
        
        var nextVersion = version.NextVersion();
        _log?.Invoke($"Using version {nextVersion}");
        if (version.Tag == null)
        {
            repo.Tags.Add( nextVersion.GetVersionTag(), repo.Head.Tip);
        }
        var dir = buildFile.Directory
                  ?? throw new Exception("Failed to read directory");
        var projects = cfg.GetSection("Projects").Get<string[]>()
                       ?? throw new Exception("No projects specified");
        foreach (var project in projects)
        {
            var d = Path.Combine(dir.FullName, project);
            _log?.Invoke($"Processing {d}");
            await VersionWriter.Update(d, nextVersion.GetVersionString(), CancellationToken.None);
        }
    }
}