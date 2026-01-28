using Alembic.Cookbook.Recipes.Extensions;
using Alembic.Cookbook.Settings;
using CaseExtensions;
using RecipeEngine.Api;
using RecipeEngine.Api.Artifacts;
using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Platforms;

namespace Alembic.Cookbook.Recipes;

public class CodeSigning : RecipeBase
{
    string alembicCodeSignListFileWindows = "windows_codesign_list.txt";

    string[] alembicBinariesToSignOnMac =
    [
        "com.unity.formats.alembic/Runtime/Plugins/x86_64/abci.bundle"
    ];
    string[] alembicBinariesToSignOnWin =
    [
        "com.unity.formats.alembic/Runtime/Plugins/ARM64/abci.dll",
        "com.unity.formats.alembic/Runtime/Plugins/x86_64/abci.dll"
    ];

    protected override ISet<Job> LoadJobs()
        => Combine.Collections(GetJobs()).SelectJobs();

    public string GetJobName(Platform platform, string packageName)
    {
        return $"Sign binaries for {packageName} on {platform.System.AsString()}";
    }

    public IEnumerable<Dependency> AsDependencies()
        => this.Jobs.ToDependencies(this);

    public IEnumerable<IJobBuilder> GetJobs()
    {
        var settings = AlembicSettings.Instance;
        var jobBuilders = new List<IJobBuilder>();
        var platforms = settings.Wrench.Packages[AlembicSettings.AlembicPackageName].EditorPlatforms;
        var packages = settings.Wrench.Packages.Where(p => p.Value.ReleaseOptions.IsReleasing);
        foreach (var package in packages)
        {
            var packageName = package.Value.ShortName;
            foreach (var platform in platforms)
            {
                if (platform.Key != SystemType.MacOS && platform.Key != SystemType.Windows)
                {
                    continue;
                }

                var signJob = CreateCodeSigningJob(platform.Value, packageName);
                if (signJob != null)
                {
                    jobBuilders.Add(signJob);
                }
            }
        }

        return jobBuilders;
    }

    IJobBuilder CreateCodeSigningJob(Platform platform, string packageName)
    {
        var packageShortName = packageName.Split(".").Last().ToPascalCase();
        return ProduceCodeSigningJob(platform, packageShortName);
    }

    IJobBuilder ProduceCodeSigningJob(Platform platform, string packageName)
    {
        IJobBuilder job = JobBuilder.Create(GetJobName(platform, packageName))
            .WithDescription(GetJobName(platform, packageName))
            .WithPlatform(platform);

        switch (platform.System)
        {
            case SystemType.MacOS:
                job.WithCodeSigningCommands(platform, string.Join(" ", alembicBinariesToSignOnMac))
                    .WithDependencies(
                        new Dependency("BuildAlembicPlugins", "build_plugins_-_macos-12")
                        )
                    .WithArtifact(new Artifact($"{packageName}_SignedBinariesOnMac", alembicBinariesToSignOnMac));
                break;
            case SystemType.Windows:
                job.WithCodeSigningCommands(platform, alembicCodeSignListFileWindows)
                    .WithDependencies(
                        new Dependency("BuildAlembicPlugins", "build_plugins_-_win10"),
                        new Dependency("BuildAlembicPlugins", "build_plugins_-_win11-arm64")
                        )
                    .WithArtifact(new Artifact($"{packageName}_SignedBinariesOnWindows", alembicBinariesToSignOnWin));
                break;
            default:
                return null;
        }
        return job;
    }
}
