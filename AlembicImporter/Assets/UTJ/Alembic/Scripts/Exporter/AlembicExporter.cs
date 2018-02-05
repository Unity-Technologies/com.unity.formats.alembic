using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif



namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    [AddComponentMenu("UTJ/Alembic/Exporter")]
    public partial class AlembicExporter : MonoBehaviour
    {
        #region fields
        public string m_outputPath;
        public aeConfig m_conf = aeConfig.default_value;
        public Scope m_scope = Scope.EntireScene;
        public bool m_fixDeltaTime = true;

        public bool m_ignoreDisabled = true;
        public bool m_assumeNonSkinnedMeshesAreConstant = true;

        public bool m_captureMeshRenderer = true;
        public bool m_captureSkinnedMeshRenderer = true;
        public bool m_captureParticleSystem = true;
        public bool m_captureCamera = true;
        public bool m_customCapturer = true;

        public bool m_meshNormals = true;
        public bool m_meshUV0 = true;
        public bool m_meshUV1 = true;
        public bool m_meshColors = true;

        public bool m_captureOnStart = false;
        public bool m_ignoreFirstFrame = true;
        public int m_maxCaptureFrame = 0;

        public bool m_detailedLog = true;
        public bool m_debugLog = false;

        aeContext m_ctx;
        ComponentCapturer m_root;
        List<ComponentCapturer> m_capturers = new List<ComponentCapturer>();
        bool m_recording;
        float m_time;
        float m_elapsed;
        int m_frameCount;
        #endregion


        #region properties
        public bool isRecording { get { return m_recording; } }
        public float time { get { return m_time; } }
        public float elapsed { get { return m_elapsed; } }
        public float frameCount { get { return m_frameCount; } }
        #endregion


        #region impl
        T[] GetTargets<T>() where T : Component
        {
            if(m_scope == Scope.CurrentBranch)
            {
                return GetComponentsInChildren<T>();
            }
            else
            {
                return FindObjectsOfType<T>();
            }
        }


        public TransformCapturer CreateComponentCapturer(ComponentCapturer parent, Transform target)
        {
            if (m_debugLog) { Debug.Log("AlembicExporter: new TransformCapturer(\"" + target.name + "\")"); }

            var cap = new TransformCapturer(this, parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        public CameraCapturer CreateComponentCapturer(ComponentCapturer parent, Camera target)
        {
            if (m_debugLog) { Debug.Log("AlembicExporter: new CameraCapturer(\"" + target.name + "\")"); }

            var cap = new CameraCapturer(this, parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        public MeshCapturer CreateComponentCapturer(ComponentCapturer parent, MeshRenderer target)
        {
            if (m_debugLog) { Debug.Log("AlembicExporter: new MeshCapturer(\"" + target.name + "\")"); }

            var cap = new MeshCapturer(this, parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        public SkinnedMeshCapturer CreateComponentCapturer(ComponentCapturer parent, SkinnedMeshRenderer target)
        {
            if (m_debugLog) { Debug.Log("AlembicExporter: new SkinnedMeshCapturer(\"" + target.name + "\")"); }

            var cap = new SkinnedMeshCapturer(this, parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        public ParticleCapturer CreateComponentCapturer(ComponentCapturer parent, ParticleSystem target)
        {
            if (m_debugLog) { Debug.Log("AlembicExporter: new ParticleCapturer(\"" + target.name + "\")"); }

            var cap = new ParticleCapturer(this, parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        public CustomCapturerHandler CreateComponentCapturer(ComponentCapturer parent, AlembicCustomComponentCapturer target)
        {
            if (m_debugLog) { Debug.Log("AlembicExporter: new CustomCapturerHandler(\"" + target.name + "\")"); }

            target.CreateAbcObject(parent.abc);
            var cap = new CustomCapturerHandler(this, parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        bool ShouldBeIgnored(Behaviour target)
        {
            return m_ignoreDisabled && (!target.gameObject.activeInHierarchy || !target.enabled);
        }
        bool ShouldBeIgnored(ParticleSystem target)
        {
            return m_ignoreDisabled && (!target.gameObject.activeInHierarchy);
        }
        bool ShouldBeIgnored(MeshRenderer target)
        {
            if (m_ignoreDisabled && (!target.gameObject.activeInHierarchy || !target.enabled)) { return true; }
            if (target.GetComponent<MeshFilter>().sharedMesh == null) { return true; }
            return false;
        }
        bool ShouldBeIgnored(SkinnedMeshRenderer target)
        {
            if (m_ignoreDisabled && (!target.gameObject.activeInHierarchy || !target.enabled)) { return true; }
            if (target.sharedMesh == null) { return true; }
            return false;
        }


        // capture node tree for "Preserve Tree Structure" option.
        public class CaptureNode
        {
            public CaptureNode parent;
            public List<CaptureNode> children = new List<CaptureNode>();
            public Type componentType;

            public Transform obj;
            public TransformCapturer transform;
            public ComponentCapturer component;
        }

        Dictionary<Transform, CaptureNode> m_capture_node;
        List<CaptureNode> m_top_nodes;

        CaptureNode ConstructTree(Transform node)
        {
            if(node == null) { return null; }

            CaptureNode cn;
            if (m_capture_node.TryGetValue(node, out cn)) { return cn; }

            cn = new CaptureNode();
            cn.obj = node;
            m_capture_node.Add(node, cn);

            var parent = ConstructTree(node.parent);
            if (parent != null)
            {
                parent.children.Add(cn);
            }
            else
            {
                m_top_nodes.Add(cn);
            }

            return cn;
        }

        void SetupComponentCapturer(CaptureNode parent, CaptureNode node)
        {
            node.parent = parent;
            node.transform = CreateComponentCapturer(parent == null ? m_root : parent.transform, node.obj);
            node.transform.inherits = true;

            if(node.componentType == null)
            {
                // do nothing
            }
            else if (node.componentType == typeof(Camera))
            {
                node.transform.invertForward = true;
                node.component = CreateComponentCapturer(node.transform, node.obj.GetComponent<Camera>());
            }
            else if (node.componentType == typeof(MeshRenderer))
            {
                node.component = CreateComponentCapturer(node.transform, node.obj.GetComponent<MeshRenderer>());
            }
            else if (node.componentType == typeof(SkinnedMeshRenderer))
            {
                node.component = CreateComponentCapturer(node.transform, node.obj.GetComponent<SkinnedMeshRenderer>());
            }
            else if (node.componentType == typeof(ParticleSystem))
            {
                node.component = CreateComponentCapturer(node.transform, node.obj.GetComponent<ParticleSystem>());
            }
            else if (node.componentType == typeof(AlembicCustomComponentCapturer))
            {
                node.component = CreateComponentCapturer(node.transform, node.obj.GetComponent<AlembicCustomComponentCapturer>());
            }

            foreach (var c in node.children)
            {
                SetupComponentCapturer(node, c);
            }
        }

        void CreateCapturers()
        {
            m_root = new RootCapturer(this, m_ctx.topObject);
            m_capture_node = new Dictionary<Transform, CaptureNode>();
            m_top_nodes = new List<CaptureNode>();

            // construct tree
            // (bottom-up)
            if (m_captureCamera)
            {
                foreach (var t in GetTargets<Camera>())
                {
                    if (ShouldBeIgnored(t)) { continue; }
                    var node = ConstructTree(t.GetComponent<Transform>());
                    node.componentType = t.GetType();
                }
            }
            if (m_captureMeshRenderer)
            {
                foreach (var t in GetTargets<MeshRenderer>())
                {
                    if (ShouldBeIgnored(t)) { continue; }
                    var node = ConstructTree(t.GetComponent<Transform>());
                    node.componentType = t.GetType();
                }
            }
            if (m_captureSkinnedMeshRenderer)
            {
                foreach (var t in GetTargets<SkinnedMeshRenderer>())
                {
                    if (ShouldBeIgnored(t)) { continue; }
                    var node = ConstructTree(t.GetComponent<Transform>());
                    node.componentType = t.GetType();
                }
            }
            if (m_captureParticleSystem)
            {
                foreach (var t in GetTargets<ParticleSystem>())
                {
                    if (ShouldBeIgnored(t)) { continue; }
                    var node = ConstructTree(t.GetComponent<Transform>());
                    node.componentType = t.GetType();
                }
            }
            if (m_customCapturer)
            {
                foreach (var t in GetTargets<AlembicCustomComponentCapturer>())
                {
                    if (ShouldBeIgnored(t)) { continue; }
                    var node = ConstructTree(t.GetComponent<Transform>());
                    node.componentType = typeof(AlembicCustomComponentCapturer);
                }
            }

            // make component capturers (top-down)
            foreach (var c in m_top_nodes)
            {
                SetupComponentCapturer(null, c);
            }


            m_top_nodes = null;
            m_capture_node = null;
        }


        public bool BeginCapture()
        {
            if(m_recording) {
                Debug.Log("AlembicExporter: already started");
                return false;
            }

            {
                var dir = Path.GetDirectoryName(m_outputPath);
                if (!Directory.Exists(dir))
                {
                    try
                    {
                        Directory.CreateDirectory(dir);
                    }
                    catch(Exception)
                    {
                        Debug.LogWarning("Failed to create directory " + dir);
                        return false;
                    }
                }
            }

            // create context and open archive
            m_ctx = aeContext.Create();
            if(m_ctx.self == IntPtr.Zero) {
                Debug.LogWarning("aeCreateContext() failed");
                return false;
            }

            m_ctx.SetConfig(ref m_conf);
            if(!m_ctx.OpenArchive( m_outputPath)) {
                Debug.LogWarning("aeOpenArchive() failed");
                m_ctx.Destroy();
                return false;
            }

            // create capturers
            CreateCapturers();

            m_recording = true;
            m_time = m_conf.startTime;
            m_frameCount = 0;

            if (m_conf.timeSamplingType == aeTimeSamplingType.Uniform && m_fixDeltaTime)
            {
                Time.maximumDeltaTime = (1.0f / m_conf.frameRate);
            }

            Debug.Log("AlembicExporter: start " + m_outputPath);
            return true;
        }

        public void EndCapture()
        {
            if (!m_recording) { return; }

            m_capturers.Clear();
            m_ctx.Destroy(); // flush archive
            m_recording = false;
            m_time = 0.0f;
            m_frameCount = 0;

            Debug.Log("AlembicExporter: end: " + m_outputPath);
        }

        public void OneShot()
        {
            if (BeginCapture())
            {
                ProcessCapture();
                EndCapture();
            }
        }

        int m_prevFrame = 0;
        bool m_firstFrame = true;
        void ProcessCapture()
        {
            if (!m_recording || Time.frameCount == m_prevFrame) { return; }
            m_prevFrame = Time.frameCount;
            if (m_captureOnStart && m_ignoreFirstFrame && m_firstFrame)
            {
                m_firstFrame = false;
                return;
            }

            float begin_time = Time.realtimeSinceStartup;

            m_ctx.MarkFrameBegin();
            m_ctx.AddTime(m_time);
            foreach (var recorder in m_capturers)
            {
                recorder.Capture();
            }
            m_ctx.MarkFrameEnd();
            m_time += Time.deltaTime;
            ++m_frameCount;

            m_elapsed = Time.realtimeSinceStartup - begin_time;
            if (m_detailedLog)
            {
                Debug.Log("AlembicExporter: frame " + m_frameCount + " (" + (m_elapsed * 1000.0f) + " ms)");
            }

            if(m_maxCaptureFrame > 0 && m_frameCount >= m_maxCaptureFrame)
            {
                EndCapture();
            }
        }

        IEnumerator ProcessRecording()
        {
            yield return new WaitForEndOfFrame();
            if(!m_recording) { yield break; }

            ProcessCapture();

            // wait maximumDeltaTime if timeSamplingType is uniform
            if (m_conf.timeSamplingType == aeTimeSamplingType.Uniform && m_fixDeltaTime)
            {
                AbcAPI.aeWaitMaxDeltaTime();
            }
        }

        void UpdateOutputPath()
        {
            if (m_outputPath == null || m_outputPath == "")
            {
                m_outputPath = "Output/" + gameObject.name + ".abc";
            }
        }
        #endregion


        #region messages
#if UNITY_EDITOR
        void Reset()
        {
            ForceDisableBatching();
            UpdateOutputPath();
        }
    #endif

        void OnEnable()
        {
            UpdateOutputPath();
        }

        void Start()
        {
    #if UNITY_EDITOR
            if(m_captureOnStart && EditorApplication.isPlaying)
    #else
            if(m_captureOnStart)
    #endif
            {
                BeginCapture();
            }
        }

        void Update()
        {
            if(m_recording)
            {
                StartCoroutine(ProcessRecording());
            }
        }

        void OnDisable()
        {
            EndCapture();
        }
        #endregion
    }
}
