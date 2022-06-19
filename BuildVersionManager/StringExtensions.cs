namespace BuildVersionManager;

public static class StringExtensions
{
    public static bool InvariantEquals(this string a, string b) => 
        string.Equals(a, b, StringComparison.InvariantCultureIgnoreCase);
}