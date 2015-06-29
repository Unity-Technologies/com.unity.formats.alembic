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

    public class TextureMeshData
    {
        public RenderTexture indices;
        public RenderTexture vertices;
        public RenderTexture normals;
        public RenderTexture uvs;
        public RenderTexture velocities;
    }

    const int MeshTextureWidth = 1024;

    Transform m_trans;
    public List<Entry> m_meshes = new List<Entry>();
    TextureMeshData m_mtex;
    AbcAPI.aiTextureMeshData m_abc_mtex = default(AbcAPI.aiTextureMeshData);
    AbcAPI.aiPolyMeshSchemaSummary m_schema_summary;
    AbcAPI.aiPolyMeshSampleSummary m_mesh_summary;

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

        AbcAPI.aiPolyMeshGetSchemaSummary(abcschema, ref m_schema_summary);
        int peak_index_count = (int)m_schema_summary.peak_index_count;
        int peak_vertex_count = (int)m_schema_summary.peak_vertex_count;

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
            m_mtex = new TextureMeshData();
            m_abc_mtex = new AbcAPI.aiTextureMeshData();
            m_abc_mtex.tex_width = MeshTextureWidth;

            m_mtex.indices = CreateDataTexture(peak_index_count, 1, RenderTextureFormat.RInt);
            m_abc_mtex.tex_indices = m_mtex.indices.GetNativeTexturePtr();

            m_mtex.vertices = CreateDataTexture(peak_vertex_count, 3, RenderTextureFormat.RFloat);
            m_abc_mtex.tex_vertices = m_mtex.vertices.GetNativeTexturePtr();

            if (m_schema_summary.has_normals != 0)
            {
                int normal_count = m_schema_summary.is_normals_indexed != 0 ? peak_vertex_count : peak_index_count;
                m_mtex.normals = CreateDataTexture(normal_count, 3, RenderTextureFormat.RFloat);
                m_abc_mtex.tex_normals = m_mtex.normals.GetNativeTexturePtr();
            }
            if (m_schema_summary.has_uvs != 0)
            {
                int uv_count = m_schema_summary.is_uvs_indexed != 0 ? peak_vertex_count : peak_index_count;
                m_mtex.uvs = CreateDataTexture(uv_count, 2, RenderTextureFormat.RFloat);
                m_abc_mtex.tex_uvs = m_mtex.uvs.GetNativeTexturePtr();
            }
            if (m_schema_summary.has_velocities != 0)
            {
                m_mtex.velocities = CreateDataTexture(peak_vertex_count, 3, RenderTextureFormat.RFloat);
                m_abc_mtex.tex_velocities = m_mtex.velocities.GetNativeTexturePtr();
            }

            for (int i = 0; i < m_meshes.Count; ++i)
            {
                m_meshes[i].mpb = new MaterialPropertyBlock();
                m_meshes[i].mpb.SetVector("_DrawData", Vector4.zero);

                m_meshes[i].mpb.SetTexture("_Indices", m_mtex.indices);
                m_meshes[i].mpb.SetTexture("_Vertices", m_mtex.vertices);
                if (m_mtex.normals != null) { m_meshes[i].mpb.SetTexture("_Normals", m_mtex.normals); }
                if (m_mtex.uvs != null) { m_meshes[i].mpb.SetTexture("_UVs", m_mtex.uvs); }
                if (m_mtex.velocities != null) { m_meshes[i].mpb.SetTexture("_Velocities", m_mtex.velocities); }

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
    }

    // called by loading thread
    void AbcOnUpdateSample_Mesh(AbcAPI.aiSample sample)
    {
        var schema = m_abcschema;
        var smi_prev = default(AbcAPI.aiSplitedMeshInfo);
        var smi = default(AbcAPI.aiSplitedMeshInfo);
        AbcAPI.aiPolyMeshGetSampleSummary(sample, ref m_mesh_summary);

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
                m_schema_summary.topology_variance == AbcAPI.aiTopologyVariance.Heterogeneous;

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
        AbcAPI.aiPolyMeshCopyToTexture(AbcAPI.aiSchemaGetSample(m_abcschema, m_abcstream.time_prev), ref m_abc_mtex);
        for (int i = 0; i < m_meshes.Count; ++i)
        {
            //[0]: begin_index, [1]: index_count, [2]: vertex_count
            var drawdata = new Vector4(max_indices * i, m_abc_mtex.index_count, m_abc_mtex.vertex_count, 0.0f);
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
