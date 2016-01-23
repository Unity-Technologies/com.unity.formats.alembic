using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UTJ
{
    [ExecuteInEditMode]
    public abstract class AlembicElement : MonoBehaviour
    {
        public AlembicStream m_abcStream;
        public AbcAPI.aiObject m_abcObj;
        public AbcAPI.aiSchema m_abcSchema;
        public GCHandle m_thisHandle;

        protected Transform m_trans;

        bool m_verbose;
        bool m_pendingUpdate;


        static void ConfigCallback(IntPtr __this, ref AbcAPI.aiConfig config)
        {
            var _this = GCHandle.FromIntPtr(__this).Target as AlembicElement;
            _this.AbcGetConfig(ref config);
        }

        static void SampleCallback(IntPtr __this, AbcAPI.aiSample sample, bool topologyChanged)
        {
            var _this = GCHandle.FromIntPtr(__this).Target as AlembicElement;
            _this.AbcSampleUpdated(sample, topologyChanged);
        }


        public T GetOrAddComponent<T>() where T : Component
        {
            var c = gameObject.GetComponent<T>();
            if (c == null)
            {
                c = gameObject.AddComponent<T>();
            }
            return c;
        }

        public virtual void OnDestroy()
        {
            m_thisHandle.Free();

            if (!Application.isPlaying)
            {
    #if UNITY_EDITOR
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    AbcDestroy();

                    if (m_abcStream != null)
                    {
                        m_abcStream.AbcRemoveElement(this);
                    }
                }
    #else
                AbcDestroy();

                if (m_abcStream != null)
                {
                    m_abcStream.AbcRemoveElement(this);
                }
    #endif
            }
        }

        public virtual void AbcSetup(AlembicStream abcStream,
                                     AbcAPI.aiObject abcObj,
                                     AbcAPI.aiSchema abcSchema)
        {
            m_abcStream = abcStream;
            m_abcObj = abcObj;
            m_abcSchema = abcSchema;
            m_thisHandle = GCHandle.Alloc(this);
            m_trans = GetComponent<Transform>();

            IntPtr ptr = GCHandle.ToIntPtr(m_thisHandle);

            AbcAPI.aiSchemaSetConfigCallback(abcSchema, ConfigCallback, ptr);
            AbcAPI.aiSchemaSetSampleCallback(abcSchema, SampleCallback, ptr);
        }

        public virtual void AbcDestroy()
        {
        }

        // Called by loading thread (not necessarily the main thread)
        public virtual void AbcGetConfig(ref AbcAPI.aiConfig config)
        {
            // Overrides aiConfig options here if needed
        }

        // Called by loading thread (not necessarily the main thread)
        public abstract void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged);

        // Called in main thread
        public abstract void AbcUpdate();


        protected void AbcVerboseLog(string msg)
        {
            if (m_abcStream != null && m_abcStream.m_verbose)
            {
                Debug.Log(msg);
            }
        }

        protected void AbcDirty()
        {
            m_pendingUpdate = true;
        }

        protected void AbcClean()
        {
            m_pendingUpdate = false;
        }

        protected bool AbcIsDirty()
        {
            return m_pendingUpdate;
        }
    }
}
