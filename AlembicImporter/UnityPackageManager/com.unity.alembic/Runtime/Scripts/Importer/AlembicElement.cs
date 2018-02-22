using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public abstract class AlembicElement : IDisposable 
    {
        public AlembicTreeNode AlembicTreeNode { get; set; }

        protected AbcAPI.aiObject m_AbcObj;
        protected AbcAPI.aiSchema m_AbcSchema;
        protected GCHandle m_ThisHandle;

        private bool m_PendingUpdate;

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
            m_ThisHandle.Free();
           
            if (AlembicTreeNode != null )
                AlembicTreeNode.RemoveAlembicObject(this);
        }

        public virtual void AbcSetup(AbcAPI.aiObject abcObj,
                                     AbcAPI.aiSchema abcSchema)
        {
            m_AbcObj = abcObj;
            m_AbcSchema = abcSchema;
            m_ThisHandle = GCHandle.Alloc(this);

            IntPtr ptr = GCHandle.ToIntPtr(m_ThisHandle);

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

        protected void AbcDirty()
        {
            m_PendingUpdate = true;
        }

        protected void AbcClean()
        {
            m_PendingUpdate = false;
        }

        protected bool AbcIsDirty()
        {
            return m_PendingUpdate;
        }


    }
}
