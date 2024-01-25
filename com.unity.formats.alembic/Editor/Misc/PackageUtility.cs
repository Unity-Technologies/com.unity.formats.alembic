using System.IO;

namespace UnityEditor.Formats.Alembic
{
    internal static class PackageUtility
    {
        public static readonly string packageName = "com.unity.formats.alembic";
        public static readonly string packageBaseFolder = Path.Combine("Packages", packageName);
        public static readonly string editorResourcesFolder = Path.Combine(packageBaseFolder, "Editor/Editor Default Resources");
    }
}
