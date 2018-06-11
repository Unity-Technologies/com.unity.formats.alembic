using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UTJ.Alembic
{

    public class AlembicTreeNode : IDisposable
    {
        public AlembicStream stream;
        public GameObject gameObject;
        public AlembicElement abcObject;
        public List<AlembicTreeNode> children = new List<AlembicTreeNode>();

        public void Dispose()
        {
            ResetTree();
            gameObject = null;
        }

        public void ResetTree()
        {
            foreach (var c in children)
                c.Dispose();
            children.Clear();

            if (abcObject != null)
            {
                abcObject.Dispose();
                abcObject = null;
            }
        }

        public T GetOrAddAlembicObj<T>() where T : AlembicElement, new()
        {
            var o = abcObject as T;
            if (o == null)
            {
                o = new T() { abcTreeNode = this };
                abcObject = o;
            }
            return o;
        }

        public T GetAlembicObj<T>() where T : AlembicElement, new()
        {
            return abcObject as T;
        }

        public void RemoveAlembicObject(AlembicElement obj)
        {
            if (obj != null && obj == abcObject)
            {
                abcObject = null;
            }
        }

        public AlembicTreeNode FindNode(GameObject go)
        {
            if (go == gameObject)
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
