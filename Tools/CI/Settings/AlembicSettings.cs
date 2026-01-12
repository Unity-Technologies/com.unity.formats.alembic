using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;

namespace Alembic.Cookbook.Settings;

public class AlembicSettings : AnnotatedSettingsBase
{
    // Path from the root of the repository where packages are located.
    readonly string[] PackagesRootPaths = { "./com.unity.formats.alembic" };

    public static readonly string AlembicPackageName = "com.unity.formats.alembic";

    //A singleton instance of AlembicSettings
    static AlembicSettings _instance;

    // update this to list all packages in this repo that you want to release.
    Dictionary<string, PackageOptions> PackageOptions = new()
    {
        {
            AlembicPackageName,
            new PackageOptions()
            {
                ReleaseOptions = new ReleaseOptions() { IsReleasing = true },
                PackJobOptions = new PackJobOptions()
                {
                    Dependencies = new List<Dependency>
                    {
                        new ("BuildAlembicPlugins", "build_plugins_-_centos"),
                        //On Mac and Windows, code signing on binaries is required before packing
                        new ("CodeSigning", "sign_binaries_for_alembic_on_macos"),
                        new ("CodeSigning", "sign_binaries_for_alembic_on_windows")
                    }
                },
                ValidationOptions = new ValidationOptions()
                {
                    DisableBatchMode = true
                }
            }
        }
    };

    public AlembicSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions
        );

        Wrench.BranchNamingPattern = BranchPatterns.ReleaseSlash;
        // Add "rme", "supported" to upm-pvp check profiles together with an exemption file
        // PVP-41-1 check is skipped because of "Unreleased" entry in CHANGELOG.md
        Wrench.PvpProfilesToCheck = new HashSet<string>() { "rme", "supported", "-PVP-41-1", ".yamato/wrench/pvp-exemptions.json" };
    }

    public WrenchSettings Wrench { get; private set; }

    public static AlembicSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AlembicSettings();
            }
            return _instance;
        }
    }
}
