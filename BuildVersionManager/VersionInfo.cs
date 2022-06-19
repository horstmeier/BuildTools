namespace BuildVersionManager;

public record VersionInfo(int Major, int Minor, string? PreRelease, int Build)
{
    public override string ToString() => $"{Major}.{Minor}{PreRelease ?? ""}.{Build}";
}