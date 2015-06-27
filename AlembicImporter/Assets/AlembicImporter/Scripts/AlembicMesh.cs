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
        public int[] buf_indices;
        public Vector3[] buf_vertices;
        public Vector3[] buf_normals;
        public Vector2[] buf_uvs;
        public Vector3[] buf_velocities;
        public Mesh mesh;
        public MeshRenderer renderer;
        public MaterialPropertyBlock mpb;
        public GameObject host;
        public bool update_index;
        public bool active;
    }

    const int MeshTextureWidth = 1024;

    Transform m_trans;
    public List<Entry> m_meshes = new List<Entry>();
    public RenderTexture m_indices;
    public RenderTexture m_vertices;
    public RenderTexture m_normals;
    public RenderTexture m_uvs;
    public RenderTexture m_velocities;
    public AbcAPI.aiTextureMeshData m_mesh_tex = default(AbcAPI.aiTextureMeshData);

    AbcAPI.aiPolyMeshSummary m_mesh_summary;

    const int max_indices = 64998;
    const int max_vertices = 65000;


    public static RenderTexture CreateDataTexture(int num_data, int num_elements, RenderTextureFormat format)
    {
        if (num_data == 0) { return null; }
        int width = MeshTextureWidth * num_elements;
        int height = AlembicUtils.ceildiv(num_data, MeshTextureWidth);
        //Debug.Log("CreateDataTexture(): " + width + " x " + height + " " + format);
        var r = new RenderTexture(width, height, 0, format);
        r.filterMode = FilterMode.Point;
        r.wrapMode = TextureWrapMode.Repeat;
        r.enableRandomWrite = true;
        r.Create();
        return r;
    }

    public override void AbcSetup(
        AlembicStream abcstream,
        AbcAPI.aiObject abcobj,
        AbcAPI.aiSchema abcschema)
    {
        base.AbcSetup(abcstream, abcobj, abcschema);
        m_trans = GetComponent<Transform>();

        int peak_index_count = AbcAPI.aiPolyMeshGetPeakIndexCount(abcschema);
        int peak_vertex_count = AbcAPI.aiPolyMeshGetPeakVertexCount(abcschema);

        if(GetComponent<MeshRenderer>()==null)
        {
            int num_mesh_objects = AlembicUtils.ceildiv(peak_index_count, max_indices);

            AddMeshComponents(abcobj, m_trans, abcstream.m_data_type);
            var entry = new AlembicMesh.Entry
            {
                host = m_trans.gameObject,
                mesh = GetComponent<MeshFilter>().sharedMesh,
                renderer = GetComponent<MeshRenderer>(),
            };
            m_meshes.Add(entry);
#if UNITY_EDITOR
            if (abcstream.m_data_type == AlembicStream.MeshDataType.Mesh)
            {
                GetComponent<MeshRenderer>().sharedMaterial = GetDefaultMaterial();
            }
            else if(abcstream.m_data_type == AlembicStream.MeshDataType.Texture)
            {
                GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/AlembicImporter/Materials/AlembicStandard.mat");
            }
#endif

            for (int i = 1; i < num_mesh_objects; ++i)
            {
                string name = "Submesh_" + i;

                GameObject go = new GameObject();
                Transform child = go.GetComponent<Transform>();
                go.name = name;
                child.parent = m_trans;
                child.localPosition = Vector3.zero;
                child.localEulerAngles = Vector3.zero;
                child.localScale = Vector3.one;
                Mesh mesh = AddMeshComponents(abcobj, child, m_abcstream.m_data_type);
                mesh.name = name;
                child.GetComponent<MeshRenderer>().sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;

                entry = new Entry
                {
                    host = go,
                    mesh = mesh,
                    renderer = child.GetComponent<MeshRenderer>(),
                };
                m_meshes.Add(entry);
            }
        }

        if (abcstream.m_data_type == AlembicStream.MeshDataType.Mesh)
        {
            for (int i = 0; i < m_meshes.Count; ++i)
            {
                m_meshes[i].buf_indices = new int[0];
                m_meshes[i].buf_vertices = new Vector3[0];
                m_meshes[i].buf_normals = new Vector3[0];
                m_meshes[i].buf_uvs = new Vector2[0];
            }
        }
        else if (abcstream.m_data_type == AlembicStream.MeshDataType.Texture)
        {
            m_mesh_tex = new AbcAPI.aiTextureMeshData();
            m_mesh_tex.tex_width = MeshTextureWidth;

            m_indices = CreateDataTexture(peak_index_count, 1, RenderTextureFormat.RInt);
            m_mesh_tex.tex_indices = m_indices.GetNativeTexturePtr();

            m_vertices = CreateDataTexture(peak_vertex_count, 3, RenderTextureFormat.RFloat);
            m_mesh_tex.tex_vertices = m_vertices.GetNativeTexturePtr();

            //if (AlembicImporter.aiPolyMeshHasNormals(abcobj))
            //{
            //    bool is_normal_indexed = AlembicImporter.aiPolyMeshIsNormalIndexed(abcobj);
            //    int normal_count = is_normal_indexed ? peak_vertex_count : peak_index_count;
            //    m_normals = CreateDataTexture(normal_count, 3, RenderTextureFormat.RFloat);
            //    m_mesh_tex.tex_normals = m_normals.GetNativeTexturePtr();
            //}
            //if (AlembicImporter.aiPolyMeshHasUVs(abcobj))
            //{
            //    bool is_uv_indexed = AlembicImporter.aiPolyMeshIsUVIndexed(abcobj);
            //    int uv_count = is_uv_indexed ? peak_vertex_count : peak_index_count;
            //    m_uvs = CreateDataTexture(uv_count, 2, RenderTextureFormat.RFloat);
            //    m_mesh_tex.tex_uvs = m_uvs.GetNativeTexturePtr();
            //}
            //if (AlembicImporter.aiPolyMeshHasVelocities(abcobj))
            //{
            //    m_velocities = CreateDataTexture(peak_vertex_count, 3, RenderTextureFormat.RFloat);
            //    m_mesh_tex.tex_velocities = m_velocities.GetNativeTexturePtr();
            //}


            for (int i = 0; i < m_meshes.Count; ++i)
            {
                m_meshes[i].mpb = new MaterialPropertyBlock();
                m_meshes[i].mpb.SetVector("_DrawData", Vector4.zero);

                m_meshes[i].mpb.SetTexture("_Indices", m_indices);
                m_meshes[i].mpb.SetTexture("_Vertices", m_vertices);
                if (m_normals != null) { m_meshes[i].mpb.SetTexture("_Normals", m_normals); }
                if (m_uvs != null) { m_meshes[i].mpb.SetTexture("_UVs", m_uvs); }
                if (m_velocities != null) { m_meshes[i].mpb.SetTexture("_Velocities", m_velocities); }

                m_meshes[i].renderer.SetPropertyBlock(m_meshes[i].mpb);
            }
        }
    }


    // called by loading thread
    public override void AbcOnUpdateSample(AbcAPI.aiSample sample)
    {
        switch (m_abcstream.m_data_type)
        {
            case AlembicStream.MeshDataType.Texture:
                AbcOnUpdateSample_Texture(sample);
                break;
            case AlembicStream.MeshDataType.Mesh:
                AbcOnUpdateSample_Mesh(sample);
                break;
        }
    }

    // called by loading thread
    void AbcOnUpdateSample_Texture(AbcAPI.aiSample sample)
    {
        AbcAPI.aiPolyMeshCopyToTexture(sample, ref m_mesh_tex);
    }

    // called by loading thread
    void AbcOnUpdateSample_Mesh(AbcAPI.aiSample sample)
    {
        var schema = m_abcschema;
        var smi_prev = default(AbcAPI.aiSplitedMeshInfo);
        var smi = default(AbcAPI.aiSplitedMeshInfo);
        AbcAPI.aiPolyMeshGetSummary(sample, ref m_mesh_summary);

        int nth_submesh = 0;
        for (; ; )
        {
            smi_prev = smi;
            smi = default(AbcAPI.aiSplitedMeshInfo);
            bool is_end = AbcAPI.aiPolyMeshGetSplitedMeshInfo(sample, ref smi, ref smi_prev, max_vertices);

            AlembicMesh.Entry entry;
            if (nth_submesh < m_meshes.Count)
            {
                entry = m_meshes[nth_submesh];
                entry.active = true;
            }
            else
            {
                Debug.Log("AlembicMesh: not enough submeshes!");
                break;
            }

            entry.update_index = entry.buf_indices.Length == 0 ||
                AbcAPI.aiPolyMeshGetTopologyVariance(schema) == AbcAPI.aiTopologyVariance.Heterogeneous;

            Array.Resize(ref entry.buf_vertices, (int)smi.vertex_count);
            smi.dst_vertices = Marshal.UnsafeAddrOfPinnedArrayElement(entry.buf_vertices, 0);

            if (m_mesh_summary.has_normals != 0)
            {
                Array.Resize(ref entry.buf_normals, (int)smi.vertex_count);
                smi.dst_normals = Marshal.UnsafeAddrOfPinnedArrayElement(entry.buf_normals, 0);
            }

            if (entry.update_index)
            {
                if (m_mesh_summary.has_uvs != 0)
                {
                    Array.Resize(ref entry.buf_uvs, (int)smi.vertex_count);
                    smi.dst_uvs = Marshal.UnsafeAddrOfPinnedArrayElement(entry.buf_uvs, 0);
                }

                Array.Resize(ref entry.buf_indices, (int)smi.triangulated_index_count);
                smi.dst_indices = Marshal.UnsafeAddrOfPinnedArrayElement(entry.buf_indices, 0);
            }

            AbcAPI.aiPolyMeshCopySplitedMesh(sample, ref smi);

            ++nth_submesh;
            if (is_end) { break; }
        }

        for (int i = nth_submesh + 1; i < m_meshes.Count; ++i)
        {
            m_meshes[i].active = false;
        }
    }



    public override void AbcUpdate()
    {
        switch (m_abcstream.m_data_type)
        {
            case AlembicStream.MeshDataType.Texture:
                AbcUpdate_Texture();
                break;
            case AlembicStream.MeshDataType.Mesh:
                AbcUpdate_Mesh();
                break;
        }
    }

    void AbcUpdate_Texture()
    {
        for (int i = 0; i < m_meshes.Count; ++i)
        {
            //[0]: begin_index, [1]: index_count, [2]: vertex_count
            var drawdata = new Vector4(max_indices * i, m_mesh_tex.index_count, m_mesh_tex.vertex_count, 0.0f);
            m_meshes[i].mpb.SetVector("_DrawData", drawdata);
            m_meshes[i].renderer.SetPropertyBlock(m_meshes[i].mpb);
        }
    }

    void AbcUpdate_Mesh()
    {
        for (int i = 0; i < m_meshes.Count; ++i)
        {
            AlembicMesh.Entry entry = m_meshes[i];
            entry.host.SetActive(entry.active);

            if (entry.update_index)
            {
                entry.mesh.Clear();
            }

            entry.mesh.vertices = entry.buf_vertices;
            if (m_mesh_summary.has_normals != 0) {
                entry.mesh.normals = entry.buf_normals;
            }
            if (entry.update_index)
            {
                if (m_mesh_summary.has_uvs != 0)
                {
                    entry.mesh.uv = entry.buf_uvs;
                }
                entry.mesh.SetIndices(entry.buf_indices, MeshTopology.Triangles, 0);
                entry.update_index = false;
            }

            // recalculate normals if needed
            if (m_mesh_summary.has_normals == 0)
            {
                entry.mesh.RecalculateNormals();
            }
        }
    }


    static Mesh AddMeshComponents(AbcAPI.aiObject abc, Transform trans, AlembicStream.MeshDataType mdt)
    {
        Mesh mesh = null;

        var mesh_filter = trans.GetComponent<MeshFilter>();
        var mesh_renderer = trans.GetComponent<MeshRenderer>();
        if (mesh_filter == null)
        {
            mesh_filter = trans.gameObject.AddComponent<MeshFilter>();
        }
        if (mesh_renderer == null)
        {
            trans.gameObject.AddComponent<MeshRenderer>();
        }

        if (mdt == AlembicStream.MeshDataType.Texture)
        {
#if UNITY_EDITOR
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/AlembicImporter/Meshes/IndexOnlyMesh.asset");
#endif
        }
        else
        {
            mesh = new Mesh();
            mesh.name = AbcAPI.aiGetName(abc);
            mesh.MarkDynamic();
        }
        mesh_filter.sharedMesh = mesh;

        return mesh;
    }

#if UNITY_EDITOR
    static MethodInfo s_GetBuiltinExtraResourcesMethod;

    static Material GetDefaultMaterial()
    {
        if (s_GetBuiltinExtraResourcesMethod == null)
        {
            BindingFlags bfs = BindingFlags.NonPublic | BindingFlags.Static;
            s_GetBuiltinExtraResourcesMethod = typeof(EditorGUIUtility).GetMethod("GetBuiltinExtraResource", bfs);
        }
        return (Material)s_GetBuiltinExtraResourcesMethod.Invoke(null, new object[] { typeof(Material), "Default-Material.mat" });
    }
#endif
}
