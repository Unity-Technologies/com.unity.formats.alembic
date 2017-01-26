using System;
using UnityEditor;
using UnityEngine;

namespace UTJ.Alembic
{
	public class AlembicUI
	{
		public static void AddHorzLine(Action action)
		{
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.BeginHorizontal();
			action.Invoke();
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndChangeCheck();
		}

		[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
		public class ReadOnlyDrawer : PropertyDrawer
		{
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				return EditorGUI.GetPropertyHeight(property, label, true);
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				GUI.enabled = false;
				EditorGUI.PropertyField(position, property, label, true);
				GUI.enabled = true;
			}
		}

	}

}
