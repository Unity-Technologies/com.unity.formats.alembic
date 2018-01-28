using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public abstract class AlembicElement : IDisposable 
    {
        protected aiObject m_abcObj;

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

        public virtual void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            m_abcObj = abcObj;
        }

        // Called in main thread before update sample. 
        public virtual void AbcBeforeUpdateSamples() { }

        // Called by loading thread (not necessarily the main thread)
        public abstract void AbcSampleUpdated(aiSample sample);

        // Called in main thread after update sample.
        public abstract void AbcUpdate();

    }
}
