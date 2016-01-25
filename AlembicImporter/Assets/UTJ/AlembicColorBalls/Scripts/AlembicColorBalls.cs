using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Threading;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UTJ
{
    public delegate void tLogCallback(IntPtr message);

    [Serializable]
    public struct PointsRandomizerConfig
    {
        [Header("Inc/Decrease")]
        public float    countRate;

        [Header("Randomness")]
        public uint     seed;
        public Vector3  diffusion;

        [Header("Repulsion")]
        public int      iteration;
        public float    particleRadius;
        public float    timestep;

        public tLogCallback logCallback;

        public static PointsRandomizerConfig default_value
        {
            get
            {
                return new PointsRandomizerConfig
                {
                    countRate = 1.0f,
                    seed = 0,
                    diffusion = new Vector3(0.0f, 0.0f, 0.0f),
                    iteration = 0,
                    particleRadius = 0.3f,
                    timestep = 1.0f / 60.0f,
                    logCallback = null,
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

#if UNITY_EDITOR
        [Header("Randomizer")]
        public PointsRandomizerConfig m_conf = PointsRandomizerConfig.default_value;
        public string m_outputPath;
        public bool m_logging = true;

        List<string> m_log = new List<string>();
        Thread m_exportThread;
#endif // UNITY_EDITOR


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
        [DllImport("PointsRandomizer")]
        public static extern bool tPointsRandomizer(string src_abc_path, string dst_abc_path, ref PointsRandomizerConfig conf);

        public void DoExport()
        {
            if(m_exportThread != null && m_exportThread.IsAlive)
            {
                Debug.Log("another export process is running");
            }

            var abc_stream = GetComponentInParent<AlembicStream>();
            if(abc_stream != null)
            {
                string path_to_src_abc = Application.streamingAssetsPath + "/" + abc_stream.m_pathToAbc;
                string path_to_dst_abc = Application.streamingAssetsPath + "/" + m_outputPath;

                if(m_logging) { m_conf.logCallback = LogCallback; }
                else { m_conf.logCallback = null; }

                Debug.Log("export started: " + path_to_dst_abc);
                m_exportThread = new Thread(()=> {
                    tPointsRandomizer(path_to_src_abc, path_to_dst_abc, ref m_conf);
                });
                m_exportThread.Start();
                EditorApplication.update += UpdateExport;
            }
        }

        // log callback. called from worker thread
        void LogCallback(IntPtr message)
        {
            lock (m_log)
            {
                m_log.Add(Marshal.PtrToStringAnsi(message));
            }
        }

        void UpdateExport()
        {
            if(m_exportThread != null)
            {
                // flush log messages
                lock (m_log)
                {
                    foreach(var m in m_log) { Debug.Log(m); }
                    m_log.Clear();
                }

                if (!m_exportThread.IsAlive)
                {
                    Debug.Log("export finished");
                    m_exportThread = null;
                    EditorApplication.update -= UpdateExport;
                }
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
