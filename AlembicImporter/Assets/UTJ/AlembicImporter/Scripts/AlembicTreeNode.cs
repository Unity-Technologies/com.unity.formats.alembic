using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UTJ.Alembic
{

	public class AlembicTreeNode : AlembicDisposable
	{
		public AlembicStream stream;
		public GameObject linkedGameObj;
		public SortedDictionary<string, AlembicElement> alembicObjects = new SortedDictionary<string, AlembicElement>();
		public List<AlembicTreeNode> children = new List<AlembicTreeNode>();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ResetTree();
				linkedGameObj = null;
			}

			base.Dispose(disposing);
		}


		public void ResetTree()
		{
			foreach (var c in children)
				c.Dispose();
			children.Clear();

			foreach (var o in alembicObjects.ToArray())  // elements.dispose removes itself from list
				o.Value.Dispose();
			alembicObjects.Clear();
		}

		public AlembicTreeNode FindChild(string name)
		{
			return children.FirstOrDefault(x => x.linkedGameObj.name == name);
		}

		public T GetOrAddAlembicObj<T>() where T : AlembicElement, new()
		{
			var o = GetAlembicObj<T>();
			if (o == null)
			{
				o = new T() { AlembicTreeNode = this };
				alembicObjects.Add(typeof(T).Name, o);
			}

			return o;
		}

		public T GetAlembicObj<T>() where T : AlembicElement, new()
		{
			AlembicElement o;
			if (alembicObjects.TryGetValue(typeof(T).Name, out o))
				return o as T;

			return null;
		}

		public void RemoveAlembicObject(AlembicElement obj )
		{
			foreach (var o in alembicObjects)
			{
				if (o.Value == obj)
				{
					alembicObjects.Remove(o.Key);
					return;
				}
			}
		}


		public AlembicTreeNode FindNodeRecursive( GameObject go )
		{
			if (go == linkedGameObj)
				return this;

			if( children != null )
				foreach (var child in children)
				{
					var x = child.FindNodeRecursive(go);
					if (x != null)
						return x;
				}

			return null;
		}
	}

}
