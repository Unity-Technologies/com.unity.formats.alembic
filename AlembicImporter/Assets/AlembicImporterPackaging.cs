#if UNITY_EDITOR
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;


public class AlembicImporterPackaging
{
    [MenuItem("Assets/AlembicImporter/MakePackage")]
    public static void MakePackage()
    {
        string[] files = new string[]
        {
"Assets/AlembicImporter/Scripts",
"Assets/Plugins",
        };
        AssetDatabase.ExportPackage(files, "AlembicImporter.unitypackage", ExportPackageOptions.Recurse);
    }

}
#endif // UNITY_EDITOR
