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


namespace UTJ
{
    [Serializable]
    public struct PointsRandomizerConfig
    {
        public float    countRate;
        public uint     randomSeed;
        public Vector3  randomDiffuse;
        public int      repulseIteration;
        public float    repulseParticleSize;
        public float    repulseTimestep;

        public static PointsRandomizerConfig default_value
        {
            get
            {
                return new PointsRandomizerConfig
                {
                    countRate = 1.0f,
                    randomSeed = 0,
                    randomDiffuse = new Vector3(0.0f, 0.0f, 0.0f),
                    repulseIteration = 0,
                    repulseParticleSize = 0.2f,
                    repulseTimestep = 1.0f / 60.0f,
                };
            }
        }
    };


    [ExecuteInEditMode]
    [AddComponentMenu("UTJ/Alembic/Color Balls")]
    [RequireComponent(typeof(AlembicPointsRenderer))]
    public class AlembicColorBalls : MonoBehaviour
    {
        [Header("Color")]
        public Color[] m_colors = new Color[] { Color.red, Color.green, Color.blue };
        public bool m_interpolation = false;
        Texture2D m_color_texture;

        [Header("Randomizer")]
        public PointsRandomizerConfig m_conf = PointsRandomizerConfig.default_value;
        public string m_outputPath;


        AlembicColorBalls()
        {
#if UNITY_EDITOR
            AddDLLSearchPath();
#endif // UNITY_EDITOR
        }

        void UpdateTexture()
        {
            m_color_texture = new Texture2D(m_colors.Length, 1, TextureFormat.RGBA32, false, false);
            m_color_texture.filterMode = m_interpolation ? FilterMode.Bilinear : FilterMode.Point;
            m_color_texture.SetPixels(m_colors);

            var apr = GetComponent<AlembicPointsRenderer>();
            foreach(var m in apr.m_materials)
            {
                m.SetTexture("_ColorBuffer", m_color_texture);
            }
            apr.RefleshMaterials();
        }


#if UNITY_EDITOR
        [DllImport("AddDLLSearchPath")]
        public static extern void AddDLLSearchPath(string path_to_add = null);

        [DllImport("PointsRandomizer")]
        public static extern bool tPointsRandomizer(string src_abc_path, string dst_abc_path, ref PointsRandomizerConfig conf);

        public void DoExport()
        {
            var abc_stream = GetComponentInParent<AlembicStream>();
            if(abc_stream != null)
            {
                tPointsRandomizer(abc_stream.m_pathToAbc, m_outputPath, ref m_conf);
            }
        }

        void UpdateOutputPath()
        {
            if (m_outputPath == null || m_outputPath == "")
            {
                var root = GetComponent<Transform>().root;
                m_outputPath = root.gameObject.name + "_mod.abc";
            }
        }
#endif // UNITY_EDITOR



#if UNITY_EDITOR
        void OnValidate()
        {
            UpdateTexture();
        }

        void Reset()
        {
            UpdateOutputPath();
        }
#endif

        void OnEnable()
        {
            UpdateOutputPath();
        }

        void Awake()
        {
            UpdateTexture();
        }
    }
}
