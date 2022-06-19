using System.Diagnostics;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.Extensions.Configuration;

namespace BuildVersionManager;

public class BuildJsonHandler
{
    private readonly RepositoryHandler _handler;
    private readonly string _versionPrefix;
    private readonly Action<string>? _log;

    public BuildJsonHandler(RepositoryHandler handler, string versionPrefix, Action<string>? log)
    {
        _handler = handler;
        _versionPrefix = versionPrefix;
        _log = log;
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
        
        var dir = buildFile.Directory
                  ?? throw new Exception("Failed to read directory");
        var repoLocation = Repository.Discover(dir.FullName);
        if (repoLocation == null) throw new Exception("Not a git repository");
        var repo = new Repository(repoLocation);
        var version = _handler.VersionInfoFromRepository(
            repo,
            defMajor, 
            defMinor);
        
        var nextVersion = version.NextVersion();
        _log?.Invoke($"Using version {nextVersion}");
        if (version.Tag == null)
        {
            repo.Tags.Add(_versionPrefix + nextVersion, repo.Head.Tip);
        }
        var projects = cfg.GetSection("Projects").Get<string[]>()
                       ?? throw new Exception("No projects specified");
        foreach (var project in projects)
        {
            var d = Path.Combine(dir.FullName, project);
            _log?.Invoke($"Processing {d}");
            await VersionWriter.Update(d, nextVersion.ToString(), CancellationToken.None);
        }
    }
}

public class RepositoryHandler
{
    private readonly string _versionPrefix;
    private readonly Regex _releaseBranch;

    public RepositoryHandler(string releaseBranch, string versionPrefix)
    {
        _versionPrefix = versionPrefix;
        _releaseBranch = new Regex(releaseBranch, RegexOptions.IgnoreCase);
    }
    
    public GitVersionInfo VersionInfoFromRepository(Repository repository, int defMajor, int defMinor)
    {
        var headSha = repository.Head.Tip.Sha;
        var branchName = repository.Head.FriendlyName ?? headSha;
        var releaseMatch = _releaseBranch.Match(branchName);
        var (major, minor, prerelease) = releaseMatch.Success
            ? (int.Parse(releaseMatch.Groups["Major"].Value), int.Parse(releaseMatch.Groups["Minor"].Value), "")
            : (defMajor, defMinor, "-" + branchName);

        var versionTagExpression = new Regex(
            $@"{_versionPrefix}{major}\.{minor}{prerelease}\.(?<build>\d+)",
            RegexOptions.IgnoreCase);

        var tags =
            from tag in repository.Tags
            let match = versionTagExpression.Match(tag.FriendlyName)
            where match.Success
            let build = int.Parse(match.Groups["build"].Value) 
            select (tag, build);

        var buildNo = 0;
        Tag? buildTag = null;

        foreach (var t in tags)
        {
            if (t.tag.Target.Sha.InvariantEquals(headSha))
            {
                buildNo = t.build;
                buildTag = t.tag;
                break;
            }

            if (buildTag == null || buildNo < t.build)
            {
                buildNo = t.build;
            }
        }

        return new GitVersionInfo(major, minor, prerelease, buildNo, buildTag);

    }
}