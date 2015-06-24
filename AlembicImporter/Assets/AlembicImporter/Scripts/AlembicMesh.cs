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
        public MaterialPropertyBlock mpb;
        public GameObject host;
    }



    const int MeshTextureWidth = 1024;

    Transform m_trans;
    public List<Entry> m_meshes = new List<Entry>();
    public RenderTexture m_indices;
    public RenderTexture m_vertices;
    public RenderTexture m_normals;
    public RenderTexture m_uvs;
    public RenderTexture m_velocities;
    public AlembicImporter.aiTextureMeshData m_mesh_tex;

    const int max_indices = 64498;
    const int max_vertices = 65000;


    public static RenderTexture CreateDataTexture(int num_data, RenderTextureFormat format)
    {
        if (num_data == 0) { return null; }
        var r = new RenderTexture(MeshTextureWidth, AlembicUtils.ceildiv(num_data, MeshTextureWidth), 0, format);
        r.filterMode = FilterMode.Point;
        r.enableRandomWrite = true;
        r.Create();
        return r;
    }

    public override void AbcSetup(AlembicStream abcstream, AlembicImporter.aiObject abcobj)
    {
        base.AbcSetup(abcstream, abcobj);
        m_trans = GetComponent<Transform>();

        int peak_index_count = AlembicImporter.aiPolyMeshGetPeakIndexCount(abcobj);
        int peak_vertex_count = AlembicImporter.aiPolyMeshGetPeakVertexCount(abcobj);

        if(GetComponent<MeshRenderer>()==null)
        {
            int num_mesh_objects = AlembicUtils.ceildiv(peak_index_count, 64998);

            AddMeshComponents(abcobj, m_trans, abcstream.m_data_type);
            var entry = new AlembicMesh.Entry
            {
                host = m_trans.gameObject,
                mesh = AddMeshComponents(abcobj, m_trans, abcstream.m_data_type),
                vertex_cache = null,
                uv_cache = null,
                index_cache = null,
                mpb = null,
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
                    vertex_cache = new Vector3[0],
                    uv_cache = new Vector2[0],
                    index_cache = new int[0],
                    mpb = null,
                };
                m_meshes.Add(entry);
            }
        }

        if (abcstream.m_data_type == AlembicStream.MeshDataType.Texture)
        {
            m_mesh_tex = new AlembicImporter.aiTextureMeshData();
            m_mesh_tex.tex_width = MeshTextureWidth;

            m_indices = CreateDataTexture(peak_index_count, RenderTextureFormat.RInt);
            m_mesh_tex.tex_indices = m_indices.GetNativeTexturePtr();

            m_vertices = CreateDataTexture(peak_vertex_count, RenderTextureFormat.ARGBFloat);
            m_mesh_tex.tex_vertices = m_vertices.GetNativeTexturePtr();

            if (AlembicImporter.aiPolyMeshHasNormals(abcobj))
            {
                bool is_normal_indexed = AlembicImporter.aiPolyMeshIsNormalIndexed(abcobj);
                int normal_count = is_normal_indexed ? peak_vertex_count : peak_index_count;
                m_normals = CreateDataTexture(normal_count, RenderTextureFormat.ARGBFloat);
                m_mesh_tex.tex_normals = m_normals.GetNativeTexturePtr();
            }
            if (AlembicImporter.aiPolyMeshHasUVs(abcobj))
            {
                bool is_uv_indexed = AlembicImporter.aiPolyMeshIsUVIndexed(abcobj);
                int uv_count = is_uv_indexed ? peak_vertex_count : peak_index_count;
                m_uvs = CreateDataTexture(uv_count, RenderTextureFormat.RGFloat);
                m_mesh_tex.tex_uvs = m_uvs.GetNativeTexturePtr();
            }
            if (AlembicImporter.aiPolyMeshHasVelocities(abcobj))
            {
                m_velocities = CreateDataTexture(peak_vertex_count, RenderTextureFormat.ARGBFloat);
                m_mesh_tex.tex_velocities = m_velocities.GetNativeTexturePtr();
            }

            for (int i = 0; i < m_meshes.Count; ++i )
            {
                m_meshes[i].mpb = new MaterialPropertyBlock();
                m_meshes[i].mpb.SetVector("_DrawData", Vector4.zero);

                m_meshes[i].mpb.SetTexture("_Indices", m_indices);
                m_meshes[i].mpb.SetTexture("_Vertices", m_vertices);
                if (m_normals != null)      { m_meshes[i].mpb.SetTexture("_Normals", m_normals); }
                if (m_uvs != null)          { m_meshes[i].mpb.SetTexture("_UVs", m_uvs); }
                if (m_velocities != null)   { m_meshes[i].mpb.SetTexture("_Velocities", m_velocities); }
            }
        }
    }

    public override void AbcUpdate()
    {
        base.AbcUpdate();
        switch (m_abcstream.m_data_type)
        {
            case AlembicStream.MeshDataType.Texture:
                AbcUpdateMeshTexture();
                break;
            case AlembicStream.MeshDataType.Mesh:
                AbcUpdateMesh();
                break;
        }
    }

    void AbcUpdateMeshTexture()
    {
        AlembicImporter.aiPolyMeshCopyToTexture(m_abcobj, ref m_mesh_tex);
        for (int i = 0; i < m_meshes.Count; ++i)
        {
            //[0]: begin_index, [1]: index_count, [2]: is_normal_indexed, [3]: is_uv_indexed
            m_meshes[i].mpb.SetVector("_DrawData", new Vector4(
                max_indices * i,
                m_mesh_tex.index_count,
                m_mesh_tex.is_normal_indexed ? 1.0f : 0.0f,
                m_mesh_tex.is_uv_indexed ? 1.0f : 0.0f ));
        }
    }

    void AbcUpdateMesh()
    {

        var trans = GetComponent<Transform>();
        var abc = m_abcobj;
        Material material = trans.GetComponent<MeshRenderer>().sharedMaterial;

        var smi_prev = new AlembicImporter.aiSplitedMeshInfo();
        var smi = new AlembicImporter.aiSplitedMeshInfo();

        int nth_submesh = 0;
        for (; ; )
        {
            smi_prev = smi;
            smi = new AlembicImporter.aiSplitedMeshInfo();
            bool is_end = AlembicImporter.aiPolyMeshGetSplitedMeshInfo(abc, ref smi, ref smi_prev, max_vertices);

            AlembicMesh.Entry entry;
            if (nth_submesh < m_meshes.Count)
            {
                entry = m_meshes[nth_submesh];
                entry.host.SetActive(true);
            }
            else
            {
                Debug.Log("AlembicMesh: not enough submeshes!");
                break;
            }

            bool needs_index_update = entry.mesh.vertexCount == 0 ||
                AlembicImporter.aiPolyMeshGetTopologyVariance(abc) == AlembicImporter.aiTopologyVariance.Heterogeneous;
            if (needs_index_update)
            {
                entry.mesh.Clear();
            }

            // update positions
            {
                Array.Resize(ref entry.vertex_cache, smi.vertex_count);
                AlembicImporter.aiPolyMeshCopySplitedVertices(abc, Marshal.UnsafeAddrOfPinnedArrayElement(entry.vertex_cache, 0), ref smi);
                entry.mesh.vertices = entry.vertex_cache;
            }

            // update normals
            if (AlembicImporter.aiPolyMeshHasNormals(abc))
            {
                // normals can reuse entry.vertex_cache
                AlembicImporter.aiPolyMeshCopySplitedNormals(abc, Marshal.UnsafeAddrOfPinnedArrayElement(entry.vertex_cache, 0), ref smi);
                entry.mesh.normals = entry.vertex_cache;
            }

            if (needs_index_update)
            {
                // update uvs
                if (AlembicImporter.aiPolyMeshHasUVs(abc))
                {
                    Array.Resize(ref entry.uv_cache, smi.vertex_count);
                    AlembicImporter.aiPolyMeshCopySplitedUVs(abc, Marshal.UnsafeAddrOfPinnedArrayElement(entry.uv_cache, 0), ref smi);
                    entry.mesh.uv = entry.uv_cache;
                }

                // update indices
                Array.Resize(ref entry.index_cache, smi.triangulated_index_count);
                AlembicImporter.aiPolyMeshCopySplitedIndices(abc, Marshal.UnsafeAddrOfPinnedArrayElement(entry.index_cache, 0), ref smi);
                entry.mesh.SetIndices(entry.index_cache, MeshTopology.Triangles, 0);
            }

            // recalculate normals
            if (!AlembicImporter.aiPolyMeshHasNormals(abc))
            {
                entry.mesh.RecalculateNormals();
            }

            ++nth_submesh;
            if (is_end) { break; }
        }

        for (int i = nth_submesh + 1; i < m_meshes.Count; ++i)
        {
            m_meshes[i].host.SetActive(false);
        }
    }


    static Mesh AddMeshComponents(AlembicImporter.aiObject abc, Transform trans, AlembicStream.MeshDataType mdt)
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
            mesh.name = AlembicImporter.aiGetName(abc);
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
