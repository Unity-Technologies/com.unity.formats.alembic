#if UNITY_EDITOR_OSX
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.OSXStandalone;
using UnityEngine;

namespace DefaultNamespace
{
    // Since 6000.6 a clean checkout builds the macOS player for Apple Silicon by default, which an Intel Mac agent cannot run ("Bad CPU type in executable").
    // Build for the architecture of the Editor that is running so the player can be launched on the same machine.
    [InitializeOnLoad]
    public static class IntelMacBuildArchitecture
    {
        static IntelMacBuildArchitecture()
        {
            if (RuntimeInformation.ProcessArchitecture != System.Runtime.InteropServices.Architecture.X64)
                return;

            UserBuildSettings.architecture = OSArchitecture.x64;
            Debug.Log("Intel Mac Editor: building the macOS player for x64.");
        }
    }
}
#endif
