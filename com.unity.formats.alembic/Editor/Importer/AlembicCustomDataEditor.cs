using Unity.Collections;
using UnityEngine.Formats.Alembic.Importer;

namespace UnityEditor.Formats.Alembic.Importer
{
    [CustomEditor(typeof(AlembicCustomData))]
    class AlembicCustomDataEditor : Editor
    {
        bool v2fParams;
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                base.OnInspectorGUI();
                v2fParams = EditorGUILayout.Foldout(v2fParams, "VertexParams");
                if (v2fParams)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        var customData = target as AlembicCustomData;
                        foreach (var v2f in customData.VertexAttributes)
                        {
                            EditorGUILayout.LabelField(v2f.Name.Value);
                        }
                    }
                }
            }
        }
    }
}
