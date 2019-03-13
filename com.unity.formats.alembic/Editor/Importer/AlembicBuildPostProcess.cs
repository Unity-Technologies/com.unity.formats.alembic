using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor.Formats.Alembic.Importer
{
    public static class AlembicBuildPostProcess
    {
        internal static readonly List<KeyValuePair<string,string>> FilesToCopy = new List<KeyValuePair<string,string>>();
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.StandaloneOSX && target != BuildTarget.StandaloneWindows64)
            {
                return;
            }

            foreach (var files in FilesToCopy)
            {
                var dir = Path.GetDirectoryName(files.Value);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.Copy(files.Key,files.Value, true);
            }
            
            FilesToCopy.Clear();
        }
    }

    class AlembicProcessScene : IProcessSceneWithReport
    {
        public int callbackOrder
        {
            get { return 9999;}  
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || 
                (report.summary.platform != BuildTarget.StandaloneOSX &&
                report.summary.platform != BuildTarget.StandaloneWindows64))
            {
                return;
                
            }
            var activeScene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            var players = Object.FindObjectsOfType<AlembicStreamPlayer>();
            var pathToStreamingAssets = GetStreamingAssetsPath(report.summary);
            foreach (var p in players)
            {
                ProcessAlembicStreamPlayerAssets(p, pathToStreamingAssets);
            }
            SceneManager.SetActiveScene(activeScene);
        }

        static void ProcessAlembicStreamPlayerAssets(AlembicStreamPlayer streamPlayer, string streamingAssetsPath)
        {
            streamPlayer.StreamDescriptor = Object.Instantiate(streamPlayer.StreamDescriptor);// make a copy
            var srcPath = streamPlayer.StreamDescriptor.PathToAbc;
            
            // Avoid name collisions by hashing the full path 
            var hashedFilename = HashSha1(srcPath) + ".abc";
            var dstPath = Path.Combine(streamingAssetsPath, hashedFilename);
            AlembicBuildPostProcess.FilesToCopy.Add(new KeyValuePair<string, string>(srcPath,dstPath));

            streamPlayer.StreamDescriptor.PathToAbc = hashedFilename;
        }

        static string GetStreamingAssetsPath(BuildSummary summary)
        {
            switch (summary.platform)
            {
                case BuildTarget.StandaloneOSX:
                    return Path.Combine(summary.outputPath, "Contents/Resources/Data/StreamingAssets");
                case BuildTarget.StandaloneWindows64:
                    var name = Path.ChangeExtension(summary.outputPath, null);
                    return name+"_Data/StreamingAssets";
                   
                default:
                    throw new NotImplementedException();   
            }
        }

        static string HashSha1(string value)
        {
            var sha1 = SHA1.Create();
            var inputBytes = Encoding.ASCII.GetBytes(value);
            var hash = sha1.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            foreach (var t in hash)
            {
                sb.Append(t.ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
