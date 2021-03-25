using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;


namespace UnityEngine.Formats.Alembic.Importer
{
    internal abstract class AlembicElement : IDisposable
    {
        private aiObject m_abcObj;
        public bool disposed {  protected set;  get; }
        public AlembicTreeNode abcTreeNode { get; set; }
        public aiObject abcObject { get { return m_abcObj; } }
        internal abstract aiSchema abcSchema { get; }
        public abstract bool visibility { get; }

        public Camera GetOrAddCamera()
        {
            var c = abcTreeNode.gameObject.GetComponent<Camera>();
            if (c == null)
            {
                c = abcTreeNode.gameObject.AddComponent<Camera>();
                c.usePhysicalProperties = true;
            }

            return c;
        }

        protected virtual void Dispose(bool v)
        {
            if (abcTreeNode != null)
                abcTreeNode.RemoveAlembicObject(this);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            disposed = true;
        }

        internal virtual void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            m_abcObj = abcObj;
        }

        // called before update samples
        public virtual void AbcPrepareSample() {}

        // called after update samples kicked
        // (possibly not finished yet. call aiPolyMesh.Sync() etc. to sync)
        public virtual void AbcSyncDataBegin() {}

        // called after AbcSyncDataBegin()
        // intended to wait vertex buffer copy task (kicked in AbcSyncDataBegin()) and update meshes in this
        public virtual void AbcSyncDataEnd() {}
    }
}
