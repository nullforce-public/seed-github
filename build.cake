#tool "nuget:?package=GitVersion.CommandLine"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// VARIABLES
//////////////////////////////////////////////////////////////////////

var distDir = Directory("./dist");
GitVersion versionInfo = null;

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
.Does(() => {
    CleanDirectory(distDir);
});

Task("VersionBuildServer")
.WithCriteria(() => AppVeyor.IsRunningOnAppVeyor)
.Does(() =>
{
    var settings = new GitVersionSettings
    {
        OutputType = GitVersionOutput.BuildServer
    };

    GitVersion(settings);
});

Task("Version")
.IsDependentOn("VersionBuildServer")
.Does(() => {
    var settings = new GitVersionSettings
    {
        OutputType = GitVersionOutput.Json
    };

    versionInfo = GitVersion(settings);
});

Task("Pack")
.IsDependentOn("Clean")
.IsDependentOn("Version")
.Does(() => {
    var settings = new NuGetPackSettings
    {
        Version = versionInfo.SemVer,
        NoPackageAnalysis = true,
        OutputDirectory = distDir
    };

    NuGetPack("./template/Nullforce.DotnetTemplate.GitHub.nuspec", settings);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////
Task("Default")
.IsDependentOn("Pack");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
