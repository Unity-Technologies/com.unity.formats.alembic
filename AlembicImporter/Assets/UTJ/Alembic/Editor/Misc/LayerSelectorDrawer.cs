using UnityEngine;
using UnityEditor;

namespace UTJ.Alembic
{
    [CustomPropertyDrawer(typeof(LayerSelector))]
    class LayerSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var p = property.FindPropertyRelative("v");
            int value = p.intValue;

            EditorGUI.BeginChangeCheck();
            value = EditorGUI.LayerField(position, value);
            if (EditorGUI.EndChangeCheck())
            {
                p.intValue = value;
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
