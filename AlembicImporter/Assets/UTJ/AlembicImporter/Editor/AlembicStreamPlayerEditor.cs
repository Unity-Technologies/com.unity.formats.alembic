using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UTJ.Alembic
{
	[ExecuteInEditMode]
	[CustomEditor(typeof(AlembicStreamPlayer))]
	public class AlembicStreamPlayerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			SerializedProperty iterator = this.serializedObject.GetIterator();
			for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
			{
				using (new EditorGUI.DisabledScope(false))
					EditorGUILayout.PropertyField(iterator, true, new GUILayoutOption[0]);                    
			}
			this.serializedObject.ApplyModifiedProperties();
		}
	}
}
