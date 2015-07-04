using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

[ExecuteInEditMode]
public class AlembicMesh : MonoBehaviour
{
    [Serializable]
    public class Split
    {
        public Vector3[] position_cache;
        public Vector3[] normal_cache;
        public Vector2[] uv_cache;
        public Mesh mesh;
        public GameObject host;
    }

    [Serializable]
    public class Submesh
    {
        public int[] index_cache;
        public int faceset_index;
        public int split_index;
    }

    public List<Submesh> m_submeshes = new List<Submesh>();
    public List<Split> m_splits = new List<Split>();
    public bool has_facesets = false;

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
