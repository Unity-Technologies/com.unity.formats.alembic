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

//[ExecuteInEditMode]
[AddComponentMenu("Alembic/PointsRenderer")]
[RequireComponent(typeof(AlembicPoints))]
public class AlembicPointsRenderer : Ist.BatchRendererBase
{
    const int TextureWidth = 2048;
    RenderTexture m_texPositions;
    RenderTexture m_texIDs;


    public static RenderTexture CreateDataTexture(int w, int h, RenderTextureFormat f)
    {
        RenderTexture r = new RenderTexture(w, h, 0, f);
        r.filterMode = FilterMode.Point;
        r.useMipMap = false;
        r.generateMips = false;
        r.Create();
        return r;
    }


    public override Material CloneMaterial(Material src, int nth)
    {
        Material m = new Material(src);
        m.SetInt("g_batch_begin", nth * m_instances_par_batch);
        m.SetTexture("g_instance_texture_t", m_texPositions);
        m.SetTexture("g_instance_texture_id", m_texIDs);

        Vector4 ts = new Vector4(
            1.0f / m_texPositions.width,
            1.0f / m_texPositions.height,
            m_texPositions.width,
            m_texPositions.height);
        m.SetVector("g_texel_size", ts);

        // fix rendering order for transparent objects
        if (m.renderQueue >= 3000)
        {
            m.renderQueue = m.renderQueue + (nth + 1);
        }
        return m;
    }


    public virtual void ReleaseGPUResources()
    {
        ClearMaterials();
    }

    public virtual void ResetGPUResoures()
    {
        ReleaseGPUResources();
        UpdateGPUResources();
    }

    public override void UpdateGPUResources()
    {
        ForEachEveryMaterials((v) =>
        {
            v.SetInt("g_num_max_instances", m_max_instances);
            v.SetInt("g_num_instances", m_instance_count);
            v.SetVector("g_model_scale", m_model_scale);
            v.SetVector("g_trans_scale", m_trans_scale);
        });
    }


#if UNITY_EDITOR
    void Reset()
    {
        m_mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Ist/Foundation/Meshes/Cube.asset");
        m_material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Ist/MassParticle/CPUParticle/Materials/MPStandard.mat");
        m_bounds_size = Vector3.one * 2.0f;
    }
#endif

    public override void OnEnable()
    {
        var points = GetComponent<AlembicPoints>();
        int abcPeakVertexCount = points.abcPeakVertexCount;

        if (m_texPositions == null)
        {
            int height = abcPeakVertexCount / TextureWidth + 1;
            m_texPositions = CreateDataTexture(TextureWidth, height, RenderTextureFormat.ARGBFloat);
            m_texIDs = CreateDataTexture(TextureWidth, height, RenderTextureFormat.RFloat);
        }

        m_max_instances = abcPeakVertexCount;

        base.OnEnable();
        ResetGPUResoures();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        ReleaseGPUResources();
    }

    public override void LateUpdate()
    {
        m_bounds_size = Vector3.one * 2.0f;
        var points = GetComponent<AlembicPoints>();
        var abcData = points.abcData;
        if (abcData.count > 0) {
            AbcAPI.aiPointsCopyPositionsToTexture(ref abcData, m_texPositions.GetNativeTexturePtr(), m_texPositions.width, m_texPositions.height, m_texPositions.format);
            AbcAPI.aiPointsCopyIDsToTexture(ref abcData, m_texIDs.GetNativeTexturePtr(), m_texIDs.width, m_texIDs.height, m_texIDs.format);

            m_instance_count = points.abcPeakVertexCount;
            base.LateUpdate();
        }
    }

    public override void OnDrawGizmos()
    {
    }
}
