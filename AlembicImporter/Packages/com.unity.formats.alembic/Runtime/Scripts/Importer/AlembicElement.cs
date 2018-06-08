using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public abstract class AlembicElement : IDisposable 
    {
        protected aiObject m_abcObj;

        public AlembicTreeNode abcTreeNode { get; set; }
        public aiObject abcObject { get { return m_abcObj; } }
        public abstract aiSchema abcSchema { get; }
        public abstract bool visibility { get; }

        public T GetOrAddComponent<T>() where T : Component
        {
            var c = abcTreeNode.gameObject.GetComponent<T>();
            if (c == null)
                c = abcTreeNode.gameObject.AddComponent<T>();
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

        // called before update samples
        public virtual void AbcPrepareSample() { }

        // called after update samples kicked
        // (possibly not finished yet. call aiPolyMesh.Sync() etc. to sync)
        public virtual void AbcSyncDataBegin() { }

        // called after AbcSyncDataBegin()
        // intended to wait vertex buffer copy task (kicked in AbcSyncDataBegin()) and update meshes in this
        public virtual void AbcSyncDataEnd() { }

    }
}
