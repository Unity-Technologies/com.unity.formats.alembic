using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    [CustomEditor(typeof(AlembicPointsRenderer))]
    public class AlembicPointsRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var iterator = this.serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                if(iterator.name == "m_layer")
                {
                    iterator.intValue = EditorGUILayout.LayerField("Layer", iterator.intValue);
                }
                else
                {
                    using (new EditorGUI.DisabledScope(false))
                    {
                        EditorGUILayout.PropertyField(iterator, true, new GUILayoutOption[0]);
                    }

                }
            }
            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
