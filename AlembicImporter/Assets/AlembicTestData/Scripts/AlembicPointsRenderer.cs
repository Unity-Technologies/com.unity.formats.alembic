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
    RenderTexture m_texPositions;
    RenderTexture m_texIDs;

    public void Update()
    {
        var points = GetComponent<AlembicPoints>();
        var abcData = points.abcData;
        if (abcData.count == 0) { return; }

        if(m_texPositions == null)
        {
            int height = points.abcPeakVertexCount / TextureWidth + 1;
            m_texPositions = new RenderTexture(TextureWidth, height, 0, RenderTextureFormat.ARGBFloat);
            m_texIDs = new RenderTexture(TextureWidth, height, 0, RenderTextureFormat.RInt);
        }

        AbcAPI.aiPointsCopyPositionsToTexture(ref abcData, m_texPositions.GetNativeTexturePtr(), m_texPositions.width, m_texPositions.height, m_texPositions.format);
        AbcAPI.aiPointsCopyIDsToTexture(ref abcData, m_texIDs.GetNativeTexturePtr(), m_texIDs.width, m_texIDs.height, m_texIDs.format);
    }
}
