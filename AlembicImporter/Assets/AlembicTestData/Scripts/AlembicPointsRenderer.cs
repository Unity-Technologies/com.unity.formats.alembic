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
    public RenderTexture m_texPositions;
    public RenderTexture m_texIDs;


    public static RenderTexture CreateDataTexture(int w, int h, RenderTextureFormat f)
    {
        RenderTexture r = new RenderTexture(w, h, 0, f);
        r.filterMode = FilterMode.Point;
        r.useMipMap = false;
        r.generateMips = false;
        r.Create();
        return r;
    }

    public void Update()
    {
        var points = GetComponent<AlembicPoints>();
        var abcData = points.abcData;
        if (abcData.count == 0) { return; }

        if(m_texPositions == null)
        {
            int height = points.abcPeakVertexCount / TextureWidth + 1;
            m_texPositions = CreateDataTexture(TextureWidth, height, RenderTextureFormat.ARGBFloat);
            m_texIDs = CreateDataTexture(TextureWidth, height, RenderTextureFormat.RFloat);
        }

        AbcAPI.aiPointsCopyPositionsToTexture(ref abcData, m_texPositions.GetNativeTexturePtr(), m_texPositions.width, m_texPositions.height, m_texPositions.format);
        AbcAPI.aiPointsCopyIDsToTexture(ref abcData, m_texIDs.GetNativeTexturePtr(), m_texIDs.width, m_texIDs.height, m_texIDs.format);
    }
}
