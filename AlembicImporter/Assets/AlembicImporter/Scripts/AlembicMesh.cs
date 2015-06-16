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

    Transform m_trans;
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

    public override void AbcSetup(AlembicStream abcstream, AlembicImporter.aiObject abcobj)
    {
        base.AbcSetup(abcstream, abcobj);
        m_trans = GetComponent<Transform>();

        if(GetComponent<MeshRenderer>()==null)
        {
            AddMeshComponents(abcobj, m_trans);
            var entry = new AlembicMesh.Entry
            {
                host = m_trans.gameObject,
                mesh = AddMeshComponents(abcobj, m_trans),
                vertex_cache = null,
                uv_cache = null,
                index_cache = null,
            };
            m_meshes.Add(entry);
#if UNITY_EDITOR
            m_trans.GetComponent<MeshRenderer>().sharedMaterial = GetDefaultMaterial();
#endif
        }

        if (abcstream.m_data_type == AlembicStream.MeshDataType.Texture)
        {
            int index_count = AlembicImporter.aiPolyMeshGetPeakIndexCount(abcobj);
            int vertex_count = AlembicImporter.aiPolyMeshGetPeakVertexCount(abcobj);

            m_indices = CreateDataTexture(index_count, RenderTextureFormat.RInt);
            m_vertices = CreateDataTexture(vertex_count, RenderTextureFormat.ARGBFloat);
            if (AlembicImporter.aiPolyMeshHasNormals(abcobj))
            {
                bool is_normal_indexed = AlembicImporter.aiPolyMeshIsNormalIndexed(abcobj);
                int normal_count = is_normal_indexed ? vertex_count : index_count;
                m_normals = CreateDataTexture(normal_count, RenderTextureFormat.ARGBFloat);
            }
            if (AlembicImporter.aiPolyMeshHasUVs(abcobj))
            {
                bool is_uv_indexed = AlembicImporter.aiPolyMeshIsUVIndexed(abcobj);
                int uv_count = is_uv_indexed ? vertex_count : index_count;
                m_uvs = CreateDataTexture(uv_count, RenderTextureFormat.RGFloat);
            }
            if (AlembicImporter.aiPolyMeshHasVelocities(abcobj))
            {
                m_velocities = CreateDataTexture(vertex_count, RenderTextureFormat.ARGBFloat);
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
        // todo
    }

    void AbcUpdateMesh()
    {
        const int max_vertices = 65000;

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
                string name = "Submesh_" + nth_submesh;

                GameObject go = new GameObject();
                Transform child = go.GetComponent<Transform>();
                go.name = name;
                child.parent = trans;
                child.localPosition = Vector3.zero;
                child.localEulerAngles = Vector3.zero;
                child.localScale = Vector3.one;
                Mesh mesh = AddMeshComponents(abc, child);
                mesh.name = name;
                child.GetComponent<MeshRenderer>().sharedMaterial = material;

                entry = new AlembicMesh.Entry
                {
                    host = go,
                    mesh = mesh,
                    vertex_cache = new Vector3[0],
                    uv_cache = new Vector2[0],
                    index_cache = new int[0],
                };
                m_meshes.Add(entry);
            }

            bool needs_index_update = entry.mesh.vertexCount == 0 || !AlembicImporter.aiPolyMeshIsTopologyConstant(abc);
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
            //m_meshes[i].host.SetActive(false);
        }
    }


    static Mesh AddMeshComponents(AlembicImporter.aiObject abc, Transform trans)
    {
        Mesh mesh;
        var mesh_filter = trans.GetComponent<MeshFilter>();
        if (mesh_filter == null)
        {
            mesh = new Mesh();
            mesh.name = AlembicImporter.aiGetName(abc);
            mesh.MarkDynamic();
            mesh_filter = trans.gameObject.AddComponent<MeshFilter>();
            mesh_filter.sharedMesh = mesh;

            trans.gameObject.AddComponent<MeshRenderer>();
        }
        else
        {
            mesh = mesh_filter.sharedMesh;
        }
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
