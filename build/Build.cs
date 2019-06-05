using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Package);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    [Parameter("NuGet API key for push - Default is empty string")]
    readonly string NuGetApiKey;

    [Solution] readonly Solution Solution;
    [GitVersion] readonly GitVersion GitVersion;

    AbsolutePath OutputDirectory => RootDirectory / "output";

    readonly string NuGetSource = IsLocalBuild ? "Local" : "nuget.org";

    Target Clean => _ => _
        .Executes(() =>
        {
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Package => _ => _
        .After(Clean)
        .Executes(() =>
        {
            Logger.Info(GitVersion.NuGetVersionV2);

            //dotnet pack .\template\Nullforce.DotnetTemplate.GitHub.csproj -o ..\output /p:version="0.1.0"
            DotNetPack(s => s
                .SetConfiguration(Configuration)
                .SetProperty("Version", GitVersion.NuGetVersionV2)
                .SetOutputDirectory(OutputDirectory)
                .EnableNoBuild()
            );
        });

    Target Publish => _ => _
        .DependsOn(Clean)
        .DependsOn(Package)
        .Executes(() =>
        {
            if (!IsLocalBuild && string.IsNullOrEmpty(NuGetApiKey))
            {
                Logger.Error("NuGet API key was not provided. Unable to push NuGet package.");
                return;
            }

            var nugetPackage = GlobFiles(OutputDirectory, "*.nupkg").First();

            DotNetNuGetPush(s => s
                .SetSource(NuGetSource)
                .SetTargetPath(nugetPackage)
                .SetApiKey(NuGetApiKey)
            );
        });

}
