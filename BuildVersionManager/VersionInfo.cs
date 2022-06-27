namespace BuildVersionManager;

public record VersionInfo(int Major, int Minor, string? PreRelease, int Build, string? ReleaseTag)
{
    public string GetVersionString() => $"{Major}.{Minor}{PreRelease ?? ""}.{Build}";
    public string GetVersionTag() =>
        ReleaseTag == null
            ? $"Version-{GetVersionString()}"
            : $"Version-{ReleaseTag}{GetVersionString()}";

    public override string ToString() => GetVersionTag();
}