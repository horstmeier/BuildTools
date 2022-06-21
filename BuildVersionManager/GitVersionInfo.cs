using LibGit2Sharp;

namespace BuildVersionManager;

public record GitVersionInfo(int Major, int Minor, string? PreRelease, int Build, Tag? Tag, string? ReleaseTag)
    : VersionInfo(Major, Minor, PreRelease, Build, ReleaseTag)
{
    public VersionInfo NextVersion() =>
        new VersionInfo(Major, Minor, PreRelease, Tag == null ? Build + 1 : Build, ReleaseTag);
}