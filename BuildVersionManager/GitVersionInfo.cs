using LibGit2Sharp;

namespace BuildVersionManager;

public record GitVersionInfo(int Major, int Minor, string? PreRelease, int Build, Tag? Tag)
    : VersionInfo(Major, Minor, PreRelease, Build)
{
    public VersionInfo NextVersion() =>
        new VersionInfo(Major, Minor, PreRelease, Tag == null ? Build + 1 : Build);
}