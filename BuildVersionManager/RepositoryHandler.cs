using System.Diagnostics;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace BuildVersionManager;

public class RepositoryHandler
{
    private readonly Func<int, int, string, string?, string>  _versionTagExpression;
    private readonly Regex _releaseBranch;

    public RepositoryHandler(string releaseBranch, Func<int, int, string, string?, string> versionTagExpression)
    {
        _versionTagExpression = versionTagExpression;
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

    private string ReleaseTagFromBranchName(string branchName)
    {
        var lastName = branchName.Split('/').Last();
        var regex = new Regex(@"^\w+-\d+-");
        var match = regex.Match(lastName);
        return match.Success
            ? match.Value.Replace("-", "")
            : string.Concat(branchName.Where(it => Char.IsLetter(it) || Char.IsDigit(it)));

    }
    
    public GitVersionInfo VersionInfoFromRepository(Repository repository, int defMajor, int defMinor)
    {
        var headSha = repository.Head.Tip.Sha;
        var branchName = repository.Head.FriendlyName ?? headSha;
        var releaseMatch = _releaseBranch.Match(branchName);
        var (major, minor, prerelease, releaseTag) = releaseMatch.Success
            ? (int.Parse(releaseMatch.Groups["Major"].Value), int.Parse(releaseMatch.Groups["Minor"].Value), "", 
                releaseMatch.Groups["Tag"].Value)
            : (defMajor, defMinor, "-" + ReleaseTagFromBranchName(branchName), null);

        var versionTagExpression = new Regex(
            _versionTagExpression(major, minor, prerelease, releaseTag),
            RegexOptions.IgnoreCase);

        var tags =
            from tag in repository.Tags
            let match = versionTagExpression.Match(tag.FriendlyName)
            where match.Success
            let build = int.Parse(match.Groups["Build"].Value) 
            select (tag, build);

        var buildNo = -1;
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

        return new GitVersionInfo(major, minor, prerelease, buildNo, buildTag, releaseTag);

    }
}