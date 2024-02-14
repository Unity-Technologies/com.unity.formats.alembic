using System.IO;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    public static class FileManipulationHelper
    {
        public static bool FileExists(string relativePath)
        {
#if UNITY_2021_3_OR_NEWER
            string path = FileUtil.GetPhysicalPath(relativePath);
#else
            string path = relativePath;
#endif
            return File.Exists(path);
        }
    }
}
