﻿
using BuildVersionManager;
using Cocona;
using LibGit2Sharp;

var versionStringPrefix = "Version-";

var repoLocation = Repository.Discover("C:\\src\\VersionManager");
if (repoLocation == null) throw new Exception("Not a git repository");
var repo = new Repository(repoLocation);
var repositoryHandler = new RepositoryHandler(@"Release/(?<Major>\d+)\.(?<Minor>\d+)",
    versionStringPrefix);

var app = CoconaApp.Create(); // is a shorthand for `CoconaApp.CreateBuilder().Build()`


app.AddCommand("increment", ([Argument] string build) =>
{
    var handler = new BuildJsonHandler(repositoryHandler, versionStringPrefix, Console.WriteLine);
    handler.Process(build).Wait();
});


app.Run();