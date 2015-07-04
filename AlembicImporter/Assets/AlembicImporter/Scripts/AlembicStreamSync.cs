using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class AlembicStreamSync : MonoBehaviour
{
   public float m_time;

   float m_time_prev;
   float m_time_eps = 0.001f;

   [Serializable]
   public class SyncItem
   {
      public GameObject stream;
      public bool sync;

      public SyncItem()
      {
         sync = true;
         stream = null;
      }

      public void Sync(float time)
      {
         if (sync == true && stream != null)
         {
            stream.SendMessage("UpdateAbc", time, SendMessageOptions.DontRequireReceiver);
         }
      }
   }

   [CustomPropertyDrawer(typeof(SyncItem))]
   public class SyncItemDrawer : PropertyDrawer
   {
      override public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
      {
         EditorGUI.BeginProperty(position, label, property);

         position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(label.text.Replace("Element", "Stream")));

         // reset indent level
         var indent = EditorGUI.indentLevel;
         EditorGUI.indentLevel = 0;

         Rect chkr = new Rect(position.x, position.y, 10, position.height);
         Rect fldr = new Rect(position.x + 15, position.y, position.width - 15, position.height);

         EditorGUI.PropertyField(chkr, property.FindPropertyRelative("sync"), GUIContent.none);
         EditorGUI.PropertyField(fldr, property.FindPropertyRelative("stream"), GUIContent.none);

         // restore indent level
         EditorGUI.indentLevel = indent;

         EditorGUI.EndProperty();
      }
   }

   public SyncItem[] m_streams;

   void Start()
   {
      m_time = 0.0f;
      m_time_prev = 0.0f;
   }

   void Update()
   {
      if (!Application.isPlaying)
      {
         if (Math.Abs(m_time - m_time_prev) > m_time_eps)
         {
            foreach (SyncItem item in m_streams)
            {
               item.Sync(m_time);
            }
         }

         m_time_prev = m_time;
      }
   }
}
