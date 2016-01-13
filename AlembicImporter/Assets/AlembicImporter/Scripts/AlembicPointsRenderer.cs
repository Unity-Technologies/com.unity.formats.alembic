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
[AddComponentMenu("Alembic/PointsRenderer")]
[RequireComponent(typeof(AlembicPoints))]
public class AlembicPointsRenderer : MonoBehaviour
{
    const int TextureWidth = 2048;

    public Mesh m_mesh;
    public Material[] m_materials;
    public bool m_cast_shadow = false;
    public bool m_receive_shadow = false;
    public float m_count_rate = 1.0f;
    public Vector3 m_model_scale = Vector3.one;
    public Vector3 m_trans_scale = Vector3.one;
#if UNITY_EDITOR
    public bool m_show_bounds = true;
#endif

    int m_instances_par_batch;
    int m_layer;
    Mesh m_expanded_mesh;
    Bounds m_bounds;
    List<List<Material>> m_actual_materials;

    RenderTexture m_texPositions;
    RenderTexture m_texIDs;


    #region static

    public static int ceildiv(int v, int d)
    {
        return v / d + (v % d == 0 ? 0 : 1);
    }

    public static Vector3 mul(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x*b.x, a.y*b.y, a.z*b.z);
    }

    static RenderTexture CreateDataTexture(int w, int h, RenderTextureFormat f)
    {
        RenderTexture r = new RenderTexture(w, h, 0, f);
        r.filterMode = FilterMode.Point;
        r.useMipMap = false;
        r.generateMips = false;
        r.Create();
        return r;
    }

    public const int MaxVertices = 65000; // Mesh's limitation

    public static Mesh CreateExpandedMesh(Mesh mesh, int required_instances, out int instances_par_batch)
    {
        Vector3[] vertices_base = mesh.vertices;
        Vector3[] normals_base = (mesh.normals == null || mesh.normals.Length == 0) ? null : mesh.normals;
        Vector4[] tangents_base = (mesh.tangents == null || mesh.tangents.Length == 0) ? null : mesh.tangents;
        Vector2[] uv_base = (mesh.uv == null || mesh.uv.Length == 0) ? null : mesh.uv;
        Color[] colors_base = (mesh.colors == null || mesh.colors.Length == 0) ? null : mesh.colors;
        int[] indices_base = (mesh.triangles == null || mesh.triangles.Length == 0) ? null : mesh.triangles;
        instances_par_batch = Mathf.Min(MaxVertices / mesh.vertexCount, required_instances);

        Vector3[] vertices = new Vector3[vertices_base.Length * instances_par_batch];
        Vector2[] idata = new Vector2[vertices_base.Length * instances_par_batch];
        Vector3[] normals = normals_base == null ? null : new Vector3[normals_base.Length * instances_par_batch];
        Vector4[] tangents = tangents_base == null ? null : new Vector4[tangents_base.Length * instances_par_batch];
        Vector2[] uv = uv_base == null ? null : new Vector2[uv_base.Length * instances_par_batch];
        Color[] colors = colors_base == null ? null : new Color[colors_base.Length * instances_par_batch];
        int[] indices = indices_base == null ? null : new int[indices_base.Length * instances_par_batch];

        for (int ii = 0; ii < instances_par_batch; ++ii)
        {
            for (int vi = 0; vi < vertices_base.Length; ++vi)
            {
                int i = ii * vertices_base.Length + vi;
                vertices[i] = vertices_base[vi];
                idata[i] = new Vector2((float)ii, (float)vi);
            }
            if (normals != null)
            {
                for (int vi = 0; vi < normals_base.Length; ++vi)
                {
                    int i = ii * normals_base.Length + vi;
                    normals[i] = normals_base[vi];
                }
            }
            if (tangents != null)
            {
                for (int vi = 0; vi < tangents_base.Length; ++vi)
                {
                    int i = ii * tangents_base.Length + vi;
                    tangents[i] = tangents_base[vi];
                }
            }
            if (uv != null)
            {
                for (int vi = 0; vi < uv_base.Length; ++vi)
                {
                    int i = ii * uv_base.Length + vi;
                    uv[i] = uv_base[vi];
                }
            }
            if (colors != null)
            {
                for (int vi = 0; vi < colors_base.Length; ++vi)
                {
                    int i = ii * colors_base.Length + vi;
                    colors[i] = colors_base[vi];
                }
            }
            if (indices != null)
            {
                for (int vi = 0; vi < indices_base.Length; ++vi)
                {
                    int i = ii * indices_base.Length + vi;
                    indices[i] = ii * vertices_base.Length + indices_base[vi];
                }
            }

        }
        Mesh ret = new Mesh();
        ret.vertices = vertices;
        ret.normals = normals;
        ret.tangents = tangents;
        ret.uv = uv;
        ret.colors = colors;
        ret.uv2 = idata;
        ret.triangles = indices;
        return ret;
    }

    #endregion


    void ForEachEveryMaterials(System.Action<Material> a)
    {
        m_actual_materials.ForEach((ma) => { ma.ForEach(v => { a(v); }); });
    }

    Material CloneMaterial(Material src, int nth)
    {
        Material m = new Material(src);
        m.SetInt("_BatchBegin", nth * m_instances_par_batch);
        m.SetTexture("_PositionBuffer", m_texPositions);
        m.SetTexture("_IDBuffer", m_texIDs);

        // fix rendering order for transparent objects
        if (m.renderQueue >= 3000)
        {
            m.renderQueue = m.renderQueue + (nth + 1);
        }
        return m;
    }

    public void RefleshMaterials()
    {
        m_actual_materials = null;
        Flush();
    }

    public void Flush()
    {
        if(m_mesh == null)
        {
            Debug.LogWarning("AlembicPointsRenderer: mesh is not assigned");
            return;
        }
        if (m_materials == null || m_materials.Length==0 || (m_materials.Length==1 && m_materials[0]==null))
        {
            Debug.LogWarning("AlembicPointsRenderer: material is not assigned");
            return;
        }



        var points = GetComponent<AlembicPoints>();
        var abcData = points.abcData;
        int max_instances = points.abcPeakVertexCount;
        int instance_count = abcData.count;
        m_bounds.center = mul(abcData.boundsCenter, m_trans_scale);
        m_bounds.extents = mul(abcData.boundsExtents, m_trans_scale);

        if (instance_count == 0) { return; } // nothing to draw

        // update data texture
        if (m_texPositions == null)
        {
            int height = ceildiv(max_instances, TextureWidth);
            m_texPositions = CreateDataTexture(TextureWidth, height, RenderTextureFormat.ARGBFloat);
            m_texIDs = CreateDataTexture(TextureWidth, height, RenderTextureFormat.RFloat);
        }
        AbcAPI.aiPointsCopyPositionsToTexture(ref abcData, m_texPositions.GetNativeTexturePtr(), m_texPositions.width, m_texPositions.height, AbcAPI.GetTextureFormat(m_texPositions));
        AbcAPI.aiPointsCopyIDsToTexture(ref abcData, m_texIDs.GetNativeTexturePtr(), m_texIDs.width, m_texIDs.height, AbcAPI.GetTextureFormat(m_texIDs));


        if (m_expanded_mesh == null)
        {
            m_expanded_mesh = CreateExpandedMesh(m_mesh, max_instances, out m_instances_par_batch);
            m_expanded_mesh.UploadMeshData(true);
            return;
        }

        if (m_actual_materials == null)
        {
            m_actual_materials = new List<List<Material>>();
            while (m_actual_materials.Count < m_materials.Length)
            {
                m_actual_materials.Add(new List<Material>());
            }
        }

        var trans = GetComponent<Transform>();
        m_expanded_mesh.bounds = m_bounds;
        m_count_rate = Mathf.Max(m_count_rate, 0.0f);
        instance_count = Mathf.Min((int)(instance_count * m_count_rate), (int)(max_instances * m_count_rate));
        int batch_count = ceildiv(instance_count, m_instances_par_batch);

        // clone materials if needed
        for (int i = 0; i < m_actual_materials.Count; ++i)
        {
            var a = m_actual_materials[i];
            while (a.Count < batch_count)
            {
                Material m = CloneMaterial(m_materials[i], a.Count);
                a.Add(m);
            }
        }

        // update materials
        var worldToLocalMatrix = trans.localToWorldMatrix;
        ForEachEveryMaterials((m) =>
        {
            m.SetInt("_NumInstances", instance_count);
            m.SetVector("_CountRate", new Vector4(m_count_rate, 1.0f / m_count_rate, 0.0f, 0.0f));
            m.SetVector("_ModelScale", m_model_scale);
            m.SetVector("_TransScale", m_trans_scale);
            m.SetMatrix("_Transform", worldToLocalMatrix);
        });

        // issue draw calls
        int layer = gameObject.layer;
        Matrix4x4 matrix = Matrix4x4.identity;
        m_actual_materials.ForEach(a =>
        {
            for (int i = 0; i < batch_count; ++i)
            {
                Graphics.DrawMesh(m_expanded_mesh, matrix, a[i], layer, null, 0, null, m_cast_shadow, m_receive_shadow);
            }
        });
    }

    void ReleaseGPUResoureces()
    {
        if (m_actual_materials != null)
        {
            m_actual_materials.ForEach(a => { a.Clear(); });
        }
        if (m_texPositions != null)
        {
            m_texPositions.Release();
            m_texPositions = null;
        }
        if (m_texIDs != null)
        {
            m_texIDs.Release();
            m_texIDs = null;
        }
        m_bounds = new Bounds();
    }


#if UNITY_EDITOR
    void Reset()
    {
        m_mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/AlembicImporter/Meshes/IcoSphere.asset");
        m_materials = new Material[1] { AssetDatabase.LoadAssetAtPath<Material>("Assets/AlembicImporter/Materials/AlembicPointsDefault.mat") };
        ReleaseGPUResoureces();
    }

    void OnValidate()
    {
        ReleaseGPUResoureces();
    }
#endif

    void OnDisable()
    {
        ReleaseGPUResoureces();
    }

    void LateUpdate()
    {
        Flush();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if(m_show_bounds)
        {
            Gizmos.matrix = GetComponent<Transform>().localToWorldMatrix;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(m_bounds.center, m_bounds.extents);
        }
    }
#endif
}
