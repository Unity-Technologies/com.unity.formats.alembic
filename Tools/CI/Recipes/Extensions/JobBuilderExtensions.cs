using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Platforms;

namespace Alembic.Cookbook.Recipes.Extensions;

internal static class JobBuilderExtensions
{
        /// <summary>
    /// Adds platform-specific code signing commands to the given job.
    /// </summary>
    /// <param name="job">The job builder to which the code signing commands will be added.</param>
    /// <param name="platform">The platform for which the code signing commands are configured.</param>
    /// <param name="binariesToSign">Specifies the binaries or file list to sign. For Mac, it is a space-separated string of binary paths. For Windows, it is a file containing the list of binary paths.</param>
    /// <returns>The updated job builder with the code signing commands.</returns>
    public static IJobBuilder WithCodeSigningCommands(this IJobBuilder job, Platform platform, string binariesToSign)
    {
        switch (platform.System)
        {
            case SystemType.MacOS:
                job.WithCommands(c => c
                        .AddBrick("git@github.cds.internal.unity3d.com:unity/macos.cds.ci.code-signing.git@v2.0.8",
                            ("CERTIFICATE_NAME", "apple-developer-id-application-unity-technologies-sf")))
                    .WithBlockCommand(3, 2,b => b
                        .WithLine("security unlock-keychain -p $UNITY_KEYCHAIN_PASSWORD /Users/$USER/Library/Keychains/login.keychain-db")
                        .WithLine($"codesign --verbose=3 --timestamp --options=runtime --force --sign \"$(<certificate_thumbprint.txt)\" {binariesToSign}")
                        .WithLine("security lock-keychain /Users/$USER/Library/Keychains/login.keychain-db")
                        .WithLine($"codesign --verify --verbose {binariesToSign}")
                    );
                break;
            case SystemType.Windows:
                job.WithCommands(c => c
                    .AddBrick("git@github.cds.internal.unity3d.com:unity/batch.cds.ci.code-signing.git@v2.0.8",
                        ("AZURE_VAULT_URI", "https://unity-cs-kv-euw1-prd.vault.azure.net/"),
                        ("AZURE_CERTIFICATE", "ev-unity-technologies-sf"),
                        ("FILE_LIST", $"{binariesToSign}"))
                    .Add($"python Tools/Scripts/verify_win_executables_signed.py --architecture x64 --codesign-list-file {binariesToSign}"));
                break;
            default:
                return null;
        }
        return job;
    }
}
