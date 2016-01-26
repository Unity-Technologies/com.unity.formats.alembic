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
    public struct PointsExpanderConfig
    {
        public float    count_rate;
        public uint     random_seed;
        public Vector3  random_diffuse;
        public int      repulse_iteration;
        public float    repulse_timestep;
        public float    repulse_damping;
        public float    repulse_advection;
        public float    repulse_particle_size;
        public float    repulse_stiffness;

        public tLogCallback log_callback;

        public static PointsExpanderConfig default_value
        {
            get
            {
                return new PointsExpanderConfig
                {
                    count_rate = 1.0f,
                    random_seed = 0,
                    random_diffuse = new Vector3(0.0f, 0.0f, 0.0f),
                    repulse_iteration = 0,
                    repulse_timestep = 1.0f / 60.0f,
                    repulse_damping = 0.1f,
                    repulse_advection = 0.1f,
                    repulse_particle_size = 0.3f,
                    repulse_stiffness = 500.0f,
                    log_callback = null,
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
        [Header("Export Settings")]
        public string m_outputPath;
        public bool m_logging = true;

        [Header("- Inc/Decrease Points")]
        public float m_countRate = 1.0f;

        [Header("- Random Diffuse")]
        public bool m_enableRandom = false;
        public uint m_randomSeed = 0;
        public Vector3 m_diffusion = new Vector3(0.2f, 0.2f, 0.2f);

        [Header("- Repulsion")]
        public bool m_enableRepulsion = false;
        public int m_iteration = 2;
        public float m_particleRadius = 0.3f;
        public float m_moveAmount = 1.0f;


        PointsExpanderConfig m_conf = PointsExpanderConfig.default_value;

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
        [DllImport("PointsExpander")]
        public static extern bool tPointsExpander(string src_abc_path, string dst_abc_path, ref PointsExpanderConfig conf);

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

                m_conf.count_rate = m_countRate;
                m_conf.random_seed = m_randomSeed;
                m_conf.random_diffuse = m_enableRandom ? m_diffusion : Vector3.zero;
                m_conf.repulse_iteration = m_enableRepulsion ? m_iteration : 0;
                m_conf.repulse_timestep = 0.01f; // 10ms
                m_conf.repulse_particle_size = m_particleRadius;
                m_conf.repulse_stiffness = 100.0f * m_moveAmount;

                if (m_logging) { m_conf.log_callback = LogCallback; }
                else { m_conf.log_callback = null; }

                Debug.Log("export started: " + path_to_dst_abc);
                m_exportThread = new Thread(()=> {
                    tPointsExpander(path_to_src_abc, path_to_dst_abc, ref m_conf);
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
