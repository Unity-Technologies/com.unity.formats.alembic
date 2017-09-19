using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public abstract class AlembicElement : IDisposable 
    {
        public AlembicTreeNode AlembicTreeNode { get; set; }

        protected AbcAPI.aiObject m_abcObj;
        protected AbcAPI.aiSchema m_abcSchema;
        protected GCHandle m_thisHandle;

        private bool m_verbose;
        private bool m_pendingUpdate;

        static void ConfigCallback(IntPtr __this, ref AbcAPI.aiConfig config)
        {
            var _this = GCHandle.FromIntPtr(__this).Target as AlembicElement;
            _this.AbcGetConfig(ref config);
        }

        static void SampleCallback(IntPtr __this, AbcAPI.aiSample sample, Bool topologyChanged)
        {
            var _this = GCHandle.FromIntPtr(__this).Target as AlembicElement;
            _this.AbcSampleUpdated(sample, topologyChanged);
        }

        public T GetOrAddComponent<T>() where T : Component
        {
            var c = AlembicTreeNode.linkedGameObj.GetComponent<T>();
            if (c == null)
            {
                c = AlembicTreeNode.linkedGameObj.AddComponent<T>();
            }
            return c;
        }

        public void Dispose()
        {
            m_thisHandle.Free();
           
            if (AlembicTreeNode != null )
                AlembicTreeNode.RemoveAlembicObject(this);
        }

        public virtual void AbcSetup(AbcAPI.aiObject abcObj,
                                     AbcAPI.aiSchema abcSchema)
        {
            m_abcObj = abcObj;
            m_abcSchema = abcSchema;
            m_thisHandle = GCHandle.Alloc(this);

            IntPtr ptr = GCHandle.ToIntPtr(m_thisHandle);

            AbcAPI.aiSchemaSetConfigCallback(abcSchema, ConfigCallback, ptr);
            AbcAPI.aiSchemaSetSampleCallback(abcSchema, SampleCallback, ptr);
        }

        // Called by loading thread (not necessarily the main thread)
        public virtual void AbcGetConfig(ref AbcAPI.aiConfig config)
        {
            // Overrides aiConfig options here if needed
        }

        // Called in main thread before update sample. 
        public abstract void AbcUpdateConfig();

        // Called by loading thread (not necessarily the main thread)
        public abstract void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged);

        // Called in main thread after update sample.
        public abstract void AbcUpdate();


        protected void AbcVerboseLog(string msg)
        {
            if (AlembicTreeNode != null && AlembicTreeNode.stream != null && AlembicTreeNode.stream.m_diagSettings.m_verbose)
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
