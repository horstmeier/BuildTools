using System.Diagnostics;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace BuildVersionManager;

public class RepositoryHandler
{
    private readonly string _versionPrefix;
    private readonly Regex _releaseBranch;

    public RepositoryHandler(string releaseBranch, string versionPrefix)
    {
        _versionPrefix = versionPrefix;
        _releaseBranch = new Regex(releaseBranch, RegexOptions.IgnoreCase);
    }
    
    public Repository RepositoryFromBuildFile(string buildFile)
    {
        var b = new FileInfo(buildFile);
        var dir = b.Directory
                  ?? throw new Exception("Failed to read directory");
        var repoLocation = Repository.Discover(dir.FullName);
        if (repoLocation == null) throw new Exception("Not a git repository");
        return new Repository(repoLocation);
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