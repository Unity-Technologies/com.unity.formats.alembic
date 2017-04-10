#if UNITY_EDITOR
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
