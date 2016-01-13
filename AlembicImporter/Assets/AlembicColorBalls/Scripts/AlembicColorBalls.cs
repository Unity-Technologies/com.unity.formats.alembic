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
[RequireComponent(typeof(AlembicPointsRenderer))]
public class AlembicColorBalls : MonoBehaviour
{
    public Color[] m_colors = new Color[] { Color.red, Color.green, Color.blue };
    Texture2D m_color_texture;

    void UpdateTexture()
    {
        m_color_texture = new Texture2D(m_colors.Length, 1, TextureFormat.RGBA32, false, false);
        m_color_texture.SetPixels(m_colors);

        var apr = GetComponent<AlembicPointsRenderer>();
        foreach(var m in apr.m_materials)
        {
            m.SetTexture("_ColorBuffer", m_color_texture);
        }
        apr.RefleshMaterials();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        UpdateTexture();
    }
#endif // UNITY_EDITOR

    void Awake()
    {
        UpdateTexture();
    }
}
