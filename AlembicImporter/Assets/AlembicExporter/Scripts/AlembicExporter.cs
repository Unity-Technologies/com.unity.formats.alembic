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
[AddComponentMenu("Alembic/Exporter")]
public class AlembicExporter : MonoBehaviour
{
    #region impl

    public static IntPtr GetArrayPtr(Array v)
    {
        return Marshal.UnsafeAddrOfPinnedArrayElement(v, 0);
    }

    public static void CaptureTransform(AbcAPI.aeObject abc, Transform trans, bool inherits, bool invertForward)
    {
        AbcAPI.aeXFormData data;
        data.inherits = inherits;

        if (invertForward) { trans.forward = trans.forward * -1.0f; }

        if (inherits)
        {
            data.translation = trans.localPosition;
            data.rotation = trans.localRotation;
            data.scale = trans.localScale;
        }
        else
        {
            data.translation = trans.position;
            data.rotation = trans.rotation;
            data.scale = trans.lossyScale;
        }

        if (invertForward) { trans.forward = trans.forward * -1.0f; }
        AbcAPI.aeXFormWriteSample(abc, ref data);
    }

    public static void CaptureCamera(AbcAPI.aeObject abc, Camera cam, AlembicCameraParams cparams = null)
    {
        var data = AbcAPI.aeCameraData.default_value;
        data.nearClippingPlane = cam.nearClipPlane;
        data.farClippingPlane = cam.farClipPlane;
        data.fieldOfView = cam.fieldOfView;
        if(cparams != null)
        {
            data.focalLength = cparams.m_focalLength;
            data.focusDistance = cparams.m_focusDistance;
            data.aperture = cparams.m_aperture;
            data.aspectRatio = cparams.GetAspectRatio();
        }
        AbcAPI.aeCameraWriteSample(abc, ref data);
    }

    public class MeshBuffer
    {
        public int[] indices;
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector2[] uvs;
    }

    public static void CaptureMesh(AbcAPI.aeObject abc, Mesh mesh, MeshBuffer buf)
    {
        buf.indices = mesh.triangles;
        buf.vertices = mesh.vertices;
        buf.normals = mesh.normals;
        buf.uvs = mesh.uv;

        var data = new AbcAPI.aePolyMeshData();
        data.indices = GetArrayPtr(buf.indices);
        data.positions = GetArrayPtr(buf.vertices);
        if(buf.normals != null) { data.normals = GetArrayPtr(buf.normals); }
        if(buf.uvs != null)     { data.uvs = GetArrayPtr(buf.uvs); }
        data.positionCount = buf.vertices.Length;
        data.indexCount = buf.indices.Length;

        AbcAPI.aePolyMeshWriteSample(abc, ref data);
    }



    public abstract class ComponentCapturer
    {
        protected GameObject m_obj;
        protected AbcAPI.aeObject m_abc;

        public GameObject obj { get { return m_obj; } }
        public AbcAPI.aeObject abc { get { return m_abc; } }
        public abstract void Capture();
    }

    public class TransformCapturer : ComponentCapturer
    {
        Transform m_target;
        bool m_inherits = false;
        bool m_invertForward = false;

        public bool inherits {
            get { return m_inherits; }
            set { m_inherits = value; }
        }
        public bool invertForward
        {
            get { return m_invertForward; }
            set { m_invertForward = value; }
        }

        public TransformCapturer(Transform target, AbcAPI.aeObject abc)
        {
            m_obj = target.gameObject;
            m_abc = abc;
            m_target = target;
            m_inherits = inherits;
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            CaptureTransform(m_abc, m_target, m_inherits, m_invertForward);
        }
    }

    public class CameraCapturer : ComponentCapturer
    {
        Camera m_target;
        AlembicCameraParams m_params;

        public CameraCapturer(Camera target, AbcAPI.aeObject abc)
        {
            m_obj = target.gameObject;
            m_abc = abc;
            m_target = target;
            m_params = target.GetComponent<AlembicCameraParams>();
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            CaptureCamera(m_abc, m_target, m_params);
        }
    }

    public class MeshCapturer : ComponentCapturer
    {
        MeshRenderer m_target;
        MeshBuffer m_mesh_buffer;

        public MeshCapturer(MeshRenderer target, AbcAPI.aeObject abc)
        {
            m_obj = target.gameObject;
            m_abc = abc;
            m_target = target;
            m_mesh_buffer = new MeshBuffer();
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            CaptureMesh(m_abc, m_target.GetComponent<MeshFilter>().sharedMesh, m_mesh_buffer);
        }
    }

    public class SkinnedMeshCapturer : ComponentCapturer
    {
        SkinnedMeshRenderer m_target;
        Mesh m_mesh;
        MeshBuffer m_mesh_buffer;

        public SkinnedMeshCapturer(SkinnedMeshRenderer target, AbcAPI.aeObject abc)
        {
            m_obj = target.gameObject;
            m_abc = abc;
            m_target = target;
            m_mesh_buffer = new MeshBuffer();
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            if (m_mesh == null) { m_mesh = new Mesh(); }
            m_target.BakeMesh(m_mesh);
            CaptureMesh(m_abc, m_mesh, m_mesh_buffer);
        }
    }

    public class ParticleCapturer : ComponentCapturer
    {
        ParticleSystem m_target;
        AbcAPI.aeProperty m_prop_rotatrions;

        ParticleSystem.Particle[] m_buf_particles;
        Vector3[] m_buf_positions;
        Vector4[] m_buf_rotations;

        public ParticleCapturer(ParticleSystem target, AbcAPI.aeObject abc)
        {
            m_obj = target.gameObject;
            m_abc = abc;
            m_target = target;

            m_prop_rotatrions = AbcAPI.aeNewProperty(m_abc, "rotation", AbcAPI.aePropertyType.Float4Array);
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            // create buffer
            int count_max = m_target.maxParticles;
            if (m_buf_particles == null)
            {
                m_buf_particles = new ParticleSystem.Particle[count_max];
                m_buf_positions = new Vector3[count_max];
                m_buf_rotations = new Vector4[count_max];
            }
            else if (m_buf_particles.Length != count_max)
            {
                Array.Resize(ref m_buf_particles, count_max);
                Array.Resize(ref m_buf_positions, count_max);
                Array.Resize(ref m_buf_rotations, count_max);
            }

            // copy particle positions & rotations to buffer
            int count = m_target.GetParticles(m_buf_particles);
            for (int i = 0; i < count; ++i)
            {
                m_buf_positions[i] = m_buf_particles[i].position;
            }
            for (int i = 0; i < count; ++i)
            {
                m_buf_rotations[i] = m_buf_particles[i].axisOfRotation;
                m_buf_rotations[i].w = m_buf_particles[i].rotation;
            }

            // write!
            var data = new AbcAPI.aePointsData();
            data.positions = GetArrayPtr(m_buf_positions);
            data.count = count;
            AbcAPI.aePointsWriteSample(m_abc, ref data);
            AbcAPI.aePropertyWriteArraySample(m_prop_rotatrions, GetArrayPtr(m_buf_rotations), count);
        }
    }

    public class CustomCapturerHandler : ComponentCapturer
    {
        AlembicCustomComponentCapturer m_target;

        public CustomCapturerHandler(AlembicCustomComponentCapturer target)
        {
            m_obj = target.gameObject;
            m_target = target;
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            m_target.Capture();
        }
    }


#if UNITY_EDITOR
    void ForceDisableBatching()
    {
        var method = typeof(UnityEditor.PlayerSettings).GetMethod("SetBatchingForPlatform", BindingFlags.NonPublic | BindingFlags.Static);
        method.Invoke(null, new object[] { BuildTarget.StandaloneWindows, 0, 0 });
        method.Invoke(null, new object[] { BuildTarget.StandaloneWindows64, 0, 0 });
    }
#endif

    #endregion


    public enum Scope
    {
        EntireScene,
        CurrentBranch,
    }

    [Header("Abc")]

    public string m_outputPath;
    public AbcAPI.aeConfig m_conf = AbcAPI.aeConfig.default_value;

    [Header("Capture Components")]

    public Scope m_scope = Scope.EntireScene;
    public bool m_preserveTreeStructure = true;
    public bool m_ignoreDisabled = true;
    [Space(8)]
    public bool m_captureMeshRenderer = true;
    public bool m_captureSkinnedMeshRenderer = true;
    public bool m_captureParticleSystem = true;
    public bool m_captureCamera = true;
    public bool m_customCapturer = true;

    [Header("Capture Setting")]

    [Tooltip("Start capture on start.")]
    public bool m_captureOnStart = false;
    [Tooltip("Automatically end capture when reached Max Capture Frame. 0=Infinite")]
    public int m_maxCaptureFrame = 0;

    [Header("Misc")]

    public bool m_detailedLog;

    AbcAPI.aeContext m_ctx;
    List<ComponentCapturer> m_capturers = new List<ComponentCapturer>();
    bool m_recording;
    float m_time;
    float m_elapsed;
    int m_frameCount;


    public bool isRecording { get { return m_recording; } }
    public float time { get { return m_time; } }
    public float elapsed { get { return m_elapsed; } }
    public float frameCount { get { return m_frameCount; } }


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


    public TransformCapturer CreateComponentCapturer(Transform target, AbcAPI.aeObject parent)
    {
        if (m_detailedLog) { Debug.Log("AlembicExporter: new TransformCapturer(\"" + target.name + "\""); }

        var abc = AbcAPI.aeNewXForm(parent, target.name + " (" + target.GetInstanceID().ToString("X8") + ")");
        var cap = new TransformCapturer(target, abc);
        m_capturers.Add(cap);
        return cap;
    }

    public CameraCapturer CreateComponentCapturer(Camera target, AbcAPI.aeObject parent)
    {
        if (m_detailedLog) { Debug.Log("AlembicExporter: new CameraCapturer(\"" + target.name + "\""); }

        var abc = AbcAPI.aeNewCamera(parent, target.name);
        var cap = new CameraCapturer(target, abc);
        m_capturers.Add(cap);
        return cap;
    }

    public MeshCapturer CreateComponentCapturer(MeshRenderer target, AbcAPI.aeObject parent)
    {
        if (m_detailedLog) { Debug.Log("AlembicExporter: new MeshCapturer(\"" + target.name + "\""); }

        var abc = AbcAPI.aeNewPolyMesh(parent, target.name);
        var cap = new MeshCapturer(target, abc);
        m_capturers.Add(cap);
        return cap;
    }

    public SkinnedMeshCapturer CreateComponentCapturer(SkinnedMeshRenderer target, AbcAPI.aeObject parent)
    {
        if (m_detailedLog) { Debug.Log("AlembicExporter: new SkinnedMeshCapturer(\"" + target.name + "\""); }

        var abc = AbcAPI.aeNewPolyMesh(parent, target.name);
        var cap = new SkinnedMeshCapturer(target, abc);
        m_capturers.Add(cap);
        return cap;
    }

    public ParticleCapturer CreateComponentCapturer(ParticleSystem target, AbcAPI.aeObject parent)
    {
        if (m_detailedLog) { Debug.Log("AlembicExporter: new ParticleCapturer(\"" + target.name + "\""); }

        var abc = AbcAPI.aeNewPoints(parent, target.name);
        var cap = new ParticleCapturer(target, abc);
        m_capturers.Add(cap);
        return cap;
    }

    public CustomCapturerHandler CreateComponentCapturer(AlembicCustomComponentCapturer target, AbcAPI.aeObject parent)
    {
        if (m_detailedLog) { Debug.Log("AlembicExporter: new CustomCapturerHandler(\"" + target.name + "\""); }

        target.CreateAbcObject(parent);
        var cap = new CustomCapturerHandler(target);
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
        var mesh = target.GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null) { return true; }
        return false;
    }
    bool ShouldBeIgnored(SkinnedMeshRenderer target)
    {
        if (m_ignoreDisabled && (!target.gameObject.activeInHierarchy || !target.enabled)) { return true; }
        var mesh = target.sharedMesh;
        if (mesh == null) { return true; }
        return false;
    }


    #region impl_capture_tree

    // capture node tree for "Preserve Tree Structure" option.
    public class CaptureNode
    {
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

    void SetupComponentCapturer(CaptureNode node, AbcAPI.aeObject parent)
    {
        node.transform = CreateComponentCapturer(node.obj, parent);
        node.transform.inherits = true;

        if(node.componentType == null)
        {
            // do nothing
        }
        else if (node.componentType == typeof(Camera))
        {
            node.transform.invertForward = true;
            node.component = CreateComponentCapturer(node.obj.GetComponent<Camera>(), node.transform.abc);
        }
        else if (node.componentType == typeof(MeshRenderer))
        {
            node.component = CreateComponentCapturer(node.obj.GetComponent<MeshRenderer>(), node.transform.abc);
        }
        else if (node.componentType == typeof(SkinnedMeshRenderer))
        {
            node.component = CreateComponentCapturer(node.obj.GetComponent<SkinnedMeshRenderer>(), node.transform.abc);
        }
        else if (node.componentType == typeof(ParticleSystem))
        {
            node.component = CreateComponentCapturer(node.obj.GetComponent<ParticleSystem>(), node.transform.abc);
        }
        else if (node.componentType == typeof(AlembicCustomComponentCapturer))
        {
            node.component = CreateComponentCapturer(node.obj.GetComponent<AlembicCustomComponentCapturer>(), node.transform.abc);
        }

        foreach (var c in node.children)
        {
            SetupComponentCapturer(c, node.transform.abc);
        }
    }
    #endregion

    void CreateCapturers_Tree()
    {
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
        var abctop = AbcAPI.aeGetTopObject(m_ctx);
        foreach (var c in m_top_nodes)
        {
            SetupComponentCapturer(c, abctop);
        }


        m_top_nodes = null;
        m_capture_node = null;
    }

    void CreateCapturers_Flat()
    {
        var top = AbcAPI.aeGetTopObject(m_ctx);

        // Camera
        if (m_captureCamera)
        {
            foreach (var target in GetTargets<Camera>())
            {
                if (ShouldBeIgnored(target)) { continue; }
                var trans = CreateComponentCapturer(target.GetComponent<Transform>(), top);
                trans.inherits = false;
                trans.invertForward = true;
                CreateComponentCapturer(target, trans.abc);
            }
        }

        // MeshRenderer
        if (m_captureMeshRenderer)
        {
            foreach (var target in GetTargets<MeshRenderer>())
            {
                if (ShouldBeIgnored(target)) { continue; }
                var trans = CreateComponentCapturer(target.GetComponent<Transform>(), top);
                trans.inherits = false;
                CreateComponentCapturer(target, trans.abc);
            }
        }

        // SkinnedMeshRenderer
        if (m_captureSkinnedMeshRenderer)
        {
            foreach (var target in GetTargets<SkinnedMeshRenderer>())
            {
                if (ShouldBeIgnored(target)) { continue; }
                var trans = CreateComponentCapturer(target.GetComponent<Transform>(), top);
                trans.inherits = false;
                CreateComponentCapturer(target, trans.abc);
            }
        }

        // ParticleSystem
        if (m_captureParticleSystem)
        {
            foreach (var target in GetTargets<ParticleSystem>())
            {
                if (ShouldBeIgnored(target)) { continue; }
                var trans = CreateComponentCapturer(target.GetComponent<Transform>(), top);
                trans.inherits = false;
                CreateComponentCapturer(target, trans.abc);
            }
        }

        // handle custom capturers (AlembicCustomComponentCapturer)
        if (m_customCapturer)
        {
            foreach (var target in GetTargets<AlembicCustomComponentCapturer>())
            {
                if (ShouldBeIgnored(target)) { continue; }
                var trans = CreateComponentCapturer(target.GetComponent<Transform>(), top);
                trans.inherits = false;
                CreateComponentCapturer(target, trans.abc);
            }
        }
    }


    public bool BeginCapture()
    {
        if(m_recording) {
            Debug.Log("AlembicExporter: already started");
            return false;
        }

        // create context and open archive
        m_ctx = AbcAPI.aeCreateContext();
        if(m_ctx.ptr == IntPtr.Zero) {
            Debug.LogWarning("aeCreateContext() failed");
            return false;
        }

        AbcAPI.aeSetConfig(m_ctx, ref m_conf);
        if(!AbcAPI.aeOpenArchive(m_ctx, m_outputPath)) {
            Debug.LogWarning("aeOpenArchive() failed");
            AbcAPI.aeDestroyContext(m_ctx);
            m_ctx = new AbcAPI.aeContext();
            return false;
        }

        // create capturers
        if(m_preserveTreeStructure) {
            CreateCapturers_Tree();
        }
        else {
            CreateCapturers_Flat();
        }

        m_recording = true;
        m_time = m_conf.startTime;
        m_frameCount = 0;

        if (m_conf.timeSamplingType == AbcAPI.aeTypeSamplingType.Uniform)
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
        AbcAPI.aeDestroyContext(m_ctx); // flush archive
        m_ctx = new AbcAPI.aeContext();
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

    void ProcessCapture()
    {
        if (!m_recording) { return; }

        float begin_time = Time.realtimeSinceStartup;

        AbcAPI.aeAddTime(m_ctx, m_time);
        foreach (var recorder in m_capturers)
        {
            recorder.Capture();
        }
        m_time += Time.deltaTime;
        ++m_frameCount;

        m_elapsed = Time.realtimeSinceStartup - begin_time;
        if (m_detailedLog)
        {
            Debug.Log("AlembicExporter.ProcessCapture(): " + (m_elapsed * 1000.0f) + "ms");
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
        if (m_conf.timeSamplingType == AbcAPI.aeTypeSamplingType.Uniform)
        {
            AbcAPI.aeWaitMaxDeltaTime();
        }
    }

    void UpdateOutputPath()
    {
        if (m_outputPath == null || m_outputPath == "")
        {
            m_outputPath = gameObject.name + ".abc";
        }
    }



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
}
