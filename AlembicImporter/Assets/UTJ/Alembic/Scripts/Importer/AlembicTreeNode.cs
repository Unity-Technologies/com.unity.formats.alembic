using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UTJ.Alembic
{

    public class AlembicTreeNode : IDisposable
    {
        public AlembicStream stream;
        public GameObject linkedGameObj;
        public AlembicElement alembicObject;
        public List<AlembicTreeNode> children = new List<AlembicTreeNode>();

        public void Dispose()
        {
            ResetTree();
            linkedGameObj = null;
        }

        public void ResetTree()
        {
            foreach (var c in children)
                c.Dispose();
            children.Clear();

            if (alembicObject != null)
            {
                alembicObject.Dispose();
                alembicObject = null;
            }
        }

        public T GetOrAddAlembicObj<T>() where T : AlembicElement, new()
        {
            var o = alembicObject as T;
            if (o == null)
            {
                o = new T() { abcTreeNode = this };
                alembicObject = o;
            }
            return o;
        }

        public T GetAlembicObj<T>() where T : AlembicElement, new()
        {
            return alembicObject as T;
        }

        public void RemoveAlembicObject(AlembicElement obj)
        {
            if (obj != null && obj == alembicObject)
            {
                alembicObject = null;
            }
        }

        public AlembicTreeNode FindNode(GameObject go)
        {
            if (go == linkedGameObj)
                return this;

            foreach (var child in children)
            {
                var x = child.FindNode(go);
                if (x != null)
                    return x;
            }
            return null;
        }
    }

}
