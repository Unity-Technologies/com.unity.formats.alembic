using UnityEditor;
using UnityEngine.TestTools;

namespace Unity.Formats.Alembic.UnitTests
{
    // The Alembic play mode tests were written pre-FEPM and depend on a full domain reload on play-mode entry to reset native recorder state, PlayableGraph
    // allocations, and SceneManager state between test runs. Referenced by name from [PrebuildSetup] / [PostBuildCleanup] on the runtime test fixtures, so it
    // only runs when those fixtures are in the test run, and the project's previous option is restored once the run finishes.
    //
    // SessionState is used so the saved value survives the domain reloads inside the run, and the "present" guard stops a repeated Setup from overwriting
    // the saved value with the None it has already applied.
    public class ForceDomainReloadOnPlay : IPrebuildSetup, IPostBuildCleanup
    {
        const string k_SavedEPMOptions = "Alembic.ForceDomainReloadOnPlay.SavedEPMOptions";
        const string k_SavedEPMOptionsPresent = "Alembic.ForceDomainReloadOnPlay.SavedEPMOptions.Present";

        public void Setup()
        {
            if (!SessionState.GetBool(k_SavedEPMOptionsPresent, false))
            {
                SessionState.SetInt(k_SavedEPMOptions, (int)EditorSettings.enterPlayModeOptions);
                SessionState.SetBool(k_SavedEPMOptionsPresent, true);
            }
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
        }

        public void Cleanup()
        {
            if (SessionState.GetBool(k_SavedEPMOptionsPresent, false))
            {
                EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(k_SavedEPMOptions, (int)EnterPlayModeOptions.DisableDomainReload);
                SessionState.EraseBool(k_SavedEPMOptionsPresent);
                SessionState.EraseInt(k_SavedEPMOptions);
            }
        }
    }
}
