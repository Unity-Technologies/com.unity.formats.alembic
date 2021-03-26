#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

namespace DefaultNamespace
{
    public static class BuildPostProcessor
    {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            var dst = Path.Combine(Path.GetTempPath(), "human.abc");
            File.Copy("Assets/human.abc", dst, true);
        }
    }
}

#endif
