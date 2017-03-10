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
    [MenuItem("Assets/Make AlembicImporter.unitypackage")]
    public static void MakePackage_Alembic()
    {
        string[] files = new string[]
        {
"Assets/UTJ",
"Assets/StreamingAssets/UTJ",
        };
        AssetDatabase.ExportPackage(files, "AlembicImporter.unitypackage", ExportPackageOptions.Recurse);
    }
}
#endif // UNITY_EDITOR
