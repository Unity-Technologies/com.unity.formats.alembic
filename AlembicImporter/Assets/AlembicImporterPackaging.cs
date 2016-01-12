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
    [MenuItem("Assets/MakePackage/AlembicImporter")]
    public static void AIMakePackage()
    {
        string[] files = new string[]
        {
"Assets/AlembicImporter",
        };
        AssetDatabase.ExportPackage(files, "AlembicImporter.unitypackage", ExportPackageOptions.Recurse);
    }

    [MenuItem("Assets/MakePackage/AlembicExporter")]
    public static void AEMakePackage()
    {
        string[] files = new string[]
        {
"Assets/AlembicExporter",
"Assets/AlembicExporterExample",
        };
        AssetDatabase.ExportPackage(files, "AlembicExporter.unitypackage", ExportPackageOptions.Recurse);
    }

}
#endif // UNITY_EDITOR
