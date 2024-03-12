namespace UnityEditor.Formats.Alembic.Importer
{
    internal class EditorHelper
    {
        internal static string BuildPathIfNecessary(string relativePath)
        {
#if UNITY_2021_3_OR_NEWER
            return FileUtil.GetPhysicalPath(relativePath);
#else
            return relativePath;
#endif
        }
    }
}
