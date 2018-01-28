using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public abstract class AlembicElement : IDisposable 
    {
        protected AbcAPI.aiObject m_abcObj;
        protected AbcAPI.aiSchema m_abcSchema;

        public AlembicTreeNode abcTreeNode { get; set; }

        public T GetOrAddComponent<T>() where T : Component
        {
            var c = abcTreeNode.linkedGameObj.GetComponent<T>();
            if (c == null)
                c = abcTreeNode.linkedGameObj.AddComponent<T>();
            return c;
        }

        public void Dispose()
        {
            if (abcTreeNode != null )
                abcTreeNode.RemoveAlembicObject(this);
        }

        public virtual void AbcSetup(AbcAPI.aiObject abcObj, AbcAPI.aiSchema abcSchema)
        {
            m_abcObj = abcObj;
            m_abcSchema = abcSchema;
        }

        // Called in main thread before update sample. 
        public virtual void AbcBeforeUpdateSamples() { }

        // Called by loading thread (not necessarily the main thread)
        public abstract void AbcSampleUpdated(AbcAPI.aiSample sample);

        // Called in main thread after update sample.
        public abstract void AbcUpdate();

    }
}
