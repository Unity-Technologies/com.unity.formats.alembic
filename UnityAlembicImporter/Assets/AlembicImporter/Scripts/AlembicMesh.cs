using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class AlembicMesh : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public Vector3[] vertex_cache;
        public int[] index_cache;
        public Mesh mesh;
        public GameObject host;
    }

    public List<Entry> m_meshes = new List<Entry>();
}
