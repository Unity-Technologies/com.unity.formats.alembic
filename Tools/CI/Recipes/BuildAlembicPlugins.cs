using Alembic.Cookbook.Settings;
using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Platforms;

namespace Alembic.Cookbook.Recipes;

public class BuildAlembicPlugins : RecipeBase
{
    protected override ISet<Job> LoadJobs()
        => Combine.Collections(GetJobs()).SelectJobs();

    public string GetJobName(Agent agent)
        => $"Build plugins - {agent.Image.Split(new []{'/', ':'})[1]}";

    public IEnumerable<Dependency> AsDependencies()
        => this.Jobs.ToDependencies(this);

    public IEnumerable<IJobBuilder> GetJobs()
    {
        var settings = AlembicSettings.Instance;

        var platforms = settings.Wrench.Packages[AlembicSettings.AlembicPackageName].EditorPlatforms;
        //Agents to build plugins on which include Win, WinArm64, Mac and Ubuntu
        List<Agent> buildAgents = new();
        foreach (var platform in platforms)
        {
            buildAgents.Add(platform.Value.Agent);
        }

        //Add WinArm64 to build agents
        Agent winArm64 = new Agent("package-ci/win11-arm64:default", FlavorType.BuildExtraLarge, ResourceType.Azure, "arm");
        buildAgents.Add(winArm64);


        List<IJobBuilder> builders = new ();
        foreach (var agent in buildAgents)
        {
            var builder = JobBuilder.Create(GetJobName(agent))
                .WithPlatform(new Platform(agent, SystemType.Unknown))
                .WithDescription(GetJobName(agent));

            if (agent.Image.Contains("win"))
            {
                builder.WithCommands(c => c
                    .Add("git submodule update --init --recursive")
                    .Add("build.cmd"));
                if (agent.Image.Contains("arm64"))
                {
                    builder.WithArtifact("plugins", "com.unity.formats.alembic/Runtime/Plugins/ARM64/abc*.dll");
                }
                else
                {
                    builder.WithArtifact("plugins", "com.unity.formats.alembic/Runtime/Plugins/x86_64/abc*.dll");
                }
            }
            else if (agent.Image.Contains("mac"))
            {
                builder.WithCommands(c => c
                    .Add("git submodule update --init --recursive")
                    .Add("./build.sh"))
                    .WithArtifact("plugins", "com.unity.formats.alembic/Runtime/Plugins/x86_64/abci.bundle/Contents/MacOS/abc*");
            }
            else
            {
                builder.WithCommands(c => c
                        .Add("git submodule update --init --recursive")
                        .Add("./build.sh"))
                    .WithArtifact("plugins", "com.unity.formats.alembic/Runtime/Plugins/x86_64/abc*.so");
            }
            builders.Add(builder);
        }

        return builders;
    }
}
