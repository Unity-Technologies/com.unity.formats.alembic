namespace UnityEditor.Formats.Alembic.Importer
{
    internal class EditorHelper
    {
        internal static string BuildPathIfNecessary(string relativePath)
        {
            return FileUtil.GetPhysicalPath(relativePath);
        }
    }
}
