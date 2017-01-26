#if UNITY_5_7_OR_NEWER || ENABLE_SCRIPTED_IMPORTERS

using System;
using UnityEditor;
using UnityEngine;

namespace UTJ.Alembic
{
	[CustomEditor(typeof(AlembicImporter))]
	public class AlembicImporterEditor : ScriptedImporterInspector
	{
		public override void OnInspectorGUI()
		{
			var property = this.serializedObject.FindProperty("m_importMode");
			AddEnumPopup(property, "Import Mode", typeof(AlembicImportMode));

			base.OnInspectorGUI();
		}

		void AddEnumPopup(SerializedProperty porperty, string text, Type typeOfEnum)
		{
			Rect ourRect = EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginProperty(ourRect, GUIContent.none, porperty);
			EditorGUI.BeginChangeCheck();

			int actualSelected = 1;
			int selectionFromInspector = porperty.intValue;
			string[] enumNamesList = System.Enum.GetNames(typeOfEnum);
			actualSelected = EditorGUILayout.Popup(text, selectionFromInspector, enumNamesList);
			porperty.intValue = actualSelected;

			EditorGUI.EndProperty();
			EditorGUILayout.EndHorizontal();
		}

	}
}

#endif
