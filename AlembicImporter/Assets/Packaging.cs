#if UNITY_EDITOR
using UnityEditor;


public class AlembicForUnityPackaging
{
    [MenuItem("Assets/Make AlembicForUnity.unitypackage")]
    public static void MakePackage_Alembic()
    {
        string[] files = new string[]
        {
"Assets/UTJ",
        };
        AssetDatabase.ExportPackage(files, "AlembicForUnity.unitypackage", ExportPackageOptions.Recurse);
    }
}
#endif // UNITY_EDITOR
