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
public class AlembicMesh : AlembicElement
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



    const int MeshTextureWidth = 1024;

    public IntPtr m_abc_mesh;
    public List<Entry> m_meshes = new List<Entry>();

    public RenderTexture m_indices;
    public RenderTexture m_vertices;
    public RenderTexture m_velocities;
    public RenderTexture m_normals;
    public RenderTexture m_uvs;
    public AlembicImporter.aiTextureMeshData m_texture_mesh;
    public MaterialPropertyBlock m_mpb;


    public static RenderTexture CreateDataTexture(int num_data, RenderTextureFormat format)
    {
        if (num_data == 0) { return null; }
        var r = new RenderTexture(MeshTextureWidth, AlembicUtils.ceildiv(num_data, MeshTextureWidth), 0, format);
        r.filterMode = FilterMode.Point;
        r.enableRandomWrite = true;
        r.Create();
        return r;
    }

    public override void AbcSetup(AlembicStream abcstream)
    {
        base.AbcSetup(abcstream);
        if (abcstream.m_data_type == AlembicStream.MeshDataType.Texture)
        {
            // todo: 
            bool is_normal_indexed = false;
            bool is_uv_indexed = false;
            int index_count = 0;
            int vertex_count = 0;
            int normal_count = is_normal_indexed ? vertex_count : index_count;
            int uv_count = is_uv_indexed ? vertex_count : index_count;

            m_indices = CreateDataTexture(index_count, RenderTextureFormat.RInt);
            m_vertices = CreateDataTexture(vertex_count, RenderTextureFormat.ARGBFloat);
            m_velocities = CreateDataTexture(vertex_count, RenderTextureFormat.ARGBFloat);
            m_normals = CreateDataTexture(normal_count, RenderTextureFormat.ARGBFloat);
            m_uvs = CreateDataTexture(uv_count, RenderTextureFormat.RGFloat);
        }
    }

    public override void AbcUpdate()
    {
        // todo
        base.AbcUpdate();
    }
}
