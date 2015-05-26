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
        public int[] index_cache;
        public Vector3[] vertex_cache;
        public Vector2[] uv_cache;
        public Mesh mesh;
        public GameObject host;
    }

    public IntPtr m_abc_mesh;
    public List<Entry> m_meshes = new List<Entry>();

    public RenderTexture m_indices;
    public RenderTexture m_vertices;
    public RenderTexture m_normals;
    public RenderTexture m_uvs;


    static RenderTexture CreateDataTexture(int num_data, RenderTextureFormat format)
    {
        const int width = 1024;
        var r = new RenderTexture(width, num_data / width, 0, format);
        r.enableRandomWrite = true;
        r.Create();
        return r;
    }

    void Update()
    {

    }

    void LateUpdate()
    {

    }
}
