using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine.Formats.Alembic.Importer
{
    internal sealed class AlembicTreeNode : IDisposable
    {
        public AlembicStream stream { get; set; }
        public GameObject gameObject { get; set; }
        internal AlembicElement abcObject { get; set; }

        private List<AlembicTreeNode> children = new List<AlembicTreeNode>();
        public List<AlembicTreeNode> Children
        {
            get { return children; }
        }

        public void Dispose()
        {
            ResetTree();
            gameObject = null;
        }

        public void ResetTree()
        {
            foreach (var c in Children)
                c.Dispose();
            Children.Clear();

            if (abcObject != null)
            {
                abcObject.Dispose();
                abcObject = null;
            }
        }

        internal T GetOrAddAlembicObj<T>() where T : AlembicElement, new()
        {
            var o = abcObject as T;
            if (o == null)
            {
                o = new T() { abcTreeNode = this };
                abcObject = o;
            }
            return o;
        }

        internal T GetAlembicObj<T>() where T : AlembicElement, new()
        {
            return abcObject as T;
        }

        internal void RemoveAlembicObject(AlembicElement obj)
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

            foreach (var child in Children)
            {
                var x = child.FindNode(go);
                if (x != null)
                    return x;
            }
            return null;
        }

        public void VisitRecursively(Action<AlembicElement> cb)
        {
            cb.Invoke(abcObject);
            foreach (var child in Children)
            {
                child.VisitRecursively(cb);
            }
        }
    }
}
