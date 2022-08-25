
using System.Text.RegularExpressions;
using BuildVersionManager;
using Cocona;
using LibGit2Sharp;

var versionStringPrefix = "Version-";

var repositoryHandler = new RepositoryHandler(@"release/(?<Tag>.*-){0,1}(?<Major>\d+)\.(?<Minor>\d+)",
    (major, minor, prerelease, releaseTag) =>
        releaseTag == null
            ? $@"Version-{major}\.{minor}{Regex.Escape(prerelease)}\.(?<Build>\d+)"
            : $@"Version-{Regex.Escape(releaseTag)}{major}\.{minor}{Regex.Escape(prerelease)}\.(?<Build>\d+)");

var app = CoconaApp.Create(); // is a shorthand for `CoconaApp.CreateBuilder().Build()`


app.AddCommand("increment", ([Argument] string build) =>
{
    var handler = new BuildJsonHandler(repositoryHandler, versionStringPrefix,"dev", Console.WriteLine);
    handler.Process(build).Wait();
});

app.AddCommand("newrelease", (int major, int minor, string build, [Argument] string repository) =>
{
    var handler = new BuildJsonHandler(repositoryHandler, versionStringPrefix, "dev", Console.WriteLine);
    handler.NewRelease(repository, build, major, minor).Wait();
});

app.Run();