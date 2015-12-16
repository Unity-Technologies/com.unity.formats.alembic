using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


[ExecuteInEditMode]
public class AlembicStreamSync : MonoBehaviour
{
   public float m_time;

   float m_lastTime;
   float m_timeEps = 0.001f;

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
         if (sync == true && stream != null && stream.activeInHierarchy)
         {
            stream.SendMessage("AbcUpdate", time, SendMessageOptions.DontRequireReceiver);
         }
      }
   }

#if UNITY_EDITOR

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

#endif

   public SyncItem[] m_streams;

   void Start()
   {
      m_time = 0.0f;
      m_lastTime = 0.0f;
   }

   // Added so that AlembicStreamSync can be used recursively
   void AbcUpdate(float time)
   {
      m_time = time;
      
      if (Math.Abs(time - m_lastTime) > m_timeEps)
      {
         foreach (SyncItem item in m_streams)
         {
            item.Sync(time);
         }
         
         m_lastTime = time;
      }
   }

   void Update()
   {
      // Do not trigger stream update if we're in play mode
      if (!Application.isPlaying)
      {
         AbcUpdate(m_time);
      }
   }
}
