#if UNITY_EDITOR
using UnityEditor;
#endif
namespace UnityEngine.Formats.Alembic.Importer
{
    static class Utils
    {
        public static GameObject CreateGameObjectWithUndo(string message)
        {
            var ret = new GameObject();
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(ret, message);
#endif
            return ret;
        }
    }
}
