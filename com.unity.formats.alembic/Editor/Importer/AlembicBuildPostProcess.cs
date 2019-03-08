using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
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

                File.Copy(files.Key,files.Value);
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
                ProcessAlembicStreamPlayerAssets(p, pathToStreamingAssets, report.summary.outputPath);
            }
            SceneManager.SetActiveScene(activeScene);
        }

        static void ProcessAlembicStreamPlayerAssets(AlembicStreamPlayer streamPlayer, string streamingAssetsPath, string basePath)
        {
            streamPlayer.StreamDescriptor = Object.Instantiate(streamPlayer.StreamDescriptor);// make a copy
            var srcPath = streamPlayer.StreamDescriptor.PathToAbc;
            var fileName = Path.GetFileName(srcPath);
            var dstPath = Path.Combine(streamingAssetsPath, fileName);
            AlembicBuildPostProcess.FilesToCopy.Add(new KeyValuePair<string, string>(srcPath,dstPath));

            streamPlayer.StreamDescriptor.PathToAbc = GetAbsolutePath(dstPath, basePath);
        }

        static string GetStreamingAssetsPath(BuildSummary summary)
        {
            switch (summary.platform)
            {
                case BuildTarget.StandaloneOSX:
                    return Path.Combine(summary.outputPath, "Contents/Resources/Data/StreamingAsset");
                case BuildTarget.StandaloneWindows64:
                    var name = Path.GetDirectoryName(summary.outputPath);
                    name = Path.Combine(name, "_Data/StreamingAssets");
                    return Path.Combine(summary.outputPath, name);
                default:
                    throw new NotImplementedException();   
            }
        }

        static string GetAbsolutePath(string fullPath, string basePath)
        {
            var fullPathUri = new Uri(fullPath, UriKind.Absolute);
            var basePathUri = new Uri(basePath, UriKind.Absolute);
            return basePathUri.MakeRelativeUri(fullPathUri).ToString();
        }
    }
}