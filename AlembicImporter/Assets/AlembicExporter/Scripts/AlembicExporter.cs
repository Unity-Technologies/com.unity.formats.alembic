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

    public static void CaptureTransform(aeAPI.aeObject abc, Transform trans, bool inherits)
    {
        aeAPI.aeXFormSampleData data;
        data.inherits = inherits;
        if(inherits)
        {
            data.translation = trans.localPosition;
            data.scale = trans.localScale;
            trans.localRotation.ToAngleAxis(out data.rotationAngle, out data.rotationAxis);
        }
        else
        {
            data.translation = trans.position;
            data.scale = trans.lossyScale;
            trans.rotation.ToAngleAxis(out data.rotationAngle, out data.rotationAxis);
        }
        aeAPI.aeXFormWriteSample(abc, ref data);
    }

    public static void CaptureCamera(aeAPI.aeObject abc, Camera cam)
    {
        var data = new aeAPI.aeCameraSampleData();
        data.nearClippingPlane = cam.nearClipPlane;
        data.farClippingPlane = cam.farClipPlane;
        data.fieldOfView = cam.fieldOfView;
        aeAPI.aeCameraWriteSample(abc, ref data);
    }

    public static void CaptureMesh(aeAPI.aeObject abc, Mesh mesh)
    {
        var data = new aeAPI.aePolyMeshSampleData();
        var indices = mesh.triangles;
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var uvs = mesh.uv;

        data.indices = GetArrayPtr(indices);
        data.positions = GetArrayPtr(vertices);
        if(normals != null) { data.normals = GetArrayPtr(normals); }
        if(uvs != null)     { data.uvs = GetArrayPtr(uvs); }
        data.vertexCount = vertices.Length;
        data.indexCount = indices.Length;

        aeAPI.aePolyMeshWriteSample(abc, ref data);
    }



    public abstract class ComponentCapturer
    {
        protected GameObject m_obj;
        protected aeAPI.aeObject m_abc;

        public GameObject obj { get { return m_obj; } }
        public aeAPI.aeObject abc { get { return m_abc; } }
        public abstract void Capture();
    }

    public class TransformCapturer : ComponentCapturer
    {
        Transform m_target;
        bool m_inherits;

        public TransformCapturer(Transform target, aeAPI.aeObject abc, bool inherits)
        {
            m_obj = target.gameObject;
            m_abc = abc;
            m_target = target;
            m_inherits = inherits;
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            CaptureTransform(m_abc, m_target, m_inherits);
        }
    }

    public class CameraCapturer : ComponentCapturer
    {
        Camera m_target;

        public CameraCapturer(Camera target, aeAPI.aeObject abc)
        {
            m_obj = target.gameObject;
            m_abc = abc;
            m_target = target;
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            CaptureCamera(m_abc, m_target);
        }
    }

    public class MeshCapturer : ComponentCapturer
    {
        MeshRenderer m_target;

        public MeshCapturer(MeshRenderer target, aeAPI.aeObject abc)
        {
            m_obj = target.gameObject;
            m_abc = abc;
            m_target = target;
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            CaptureMesh(m_abc, m_target.GetComponent<MeshFilter>().sharedMesh);
        }
    }

    public class SkinnedMeshCapturer : ComponentCapturer
    {
        SkinnedMeshRenderer m_target;
        Mesh m_mesh;

        public SkinnedMeshCapturer(SkinnedMeshRenderer target, aeAPI.aeObject abc)
        {
            m_obj = target.gameObject;
            m_abc = abc;
            m_target = target;
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            if (m_mesh == null) { m_mesh = new Mesh(); }
            m_target.BakeMesh(m_mesh);
            CaptureMesh(m_abc, m_mesh);
        }
    }

    public class ParticleCapturer : ComponentCapturer
    {
        ParticleSystem m_target;
        aeAPI.aeProperty m_prop_rotatrions;

        ParticleSystem.Particle[] m_buf_particles;
        Vector3[] m_buf_positions;
        Vector3[] m_buf_rotations;

        public ParticleCapturer(ParticleSystem target, aeAPI.aeObject abc)
        {
            m_obj = target.gameObject;
            m_abc = abc;
            m_target = target;

            m_prop_rotatrions = aeAPI.aeNewProperty(m_abc, "rotation", aeAPI.aePropertyType.Vec3Array);
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
                m_buf_rotations = new Vector3[count_max];
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
                m_buf_rotations[i] = m_buf_particles[i].rotation3D;
            }

            // write!
            var data = new aeAPI.aePointsSampleData();
            data.positions = GetArrayPtr(m_buf_positions);
            data.count = count;
            aeAPI.aePointsWriteSample(m_abc, ref data);
            aeAPI.aePropertyWriteArraySample(m_prop_rotatrions, GetArrayPtr(m_buf_rotations), count);
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

    #endregion


    public enum Scope
    {
        EntireScene,
        CurrentBranch,
    }

    public string m_output_path;
    public aeAPI.aeConfig m_conf = aeAPI.aeConfig.default_value;

    public Scope m_scope = Scope.EntireScene;
    public bool m_preserveTreeStructure = true;

    public bool m_captureMeshRenderer = true;
    public bool m_captureSkinnedMeshRenderer = true;
    public bool m_captureParticleSystem = true;
    public bool m_captureCamera = true;
    public bool m_enableCustomCapturer = true;
    public bool m_ignoreDisabled = true;

    aeAPI.aeContext m_ctx;
    List<ComponentCapturer> m_capturers = new List<ComponentCapturer>();
    bool m_recording;
    float m_time;
    int m_frameCount;


    public bool isRecording { get { return m_recording; } }
    public float time { get { return m_time; } set { m_time = value; } }
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


    public TransformCapturer CreateTransformCapturer(GameObject target, aeAPI.aeObject parent, bool inherits)
    {
        var abc = aeAPI.aeNewXForm(parent, target.name + " (" + target.GetHashCode().ToString("X8") + ")");
        var cap = new TransformCapturer(target.GetComponent<Transform>(), abc, inherits);
        m_capturers.Add(cap);
        return cap;
    }

    public CameraCapturer CreateCameraCapturer(Camera target, aeAPI.aeObject parent)
    {
        var abc = aeAPI.aeNewCamera(parent, target.name);
        var cap = new CameraCapturer(target, abc);
        m_capturers.Add(cap);
        return cap;
    }

    public MeshCapturer CreateMeshCapturer(MeshRenderer target, aeAPI.aeObject parent)
    {
        var abc = aeAPI.aeNewPolyMesh(parent, target.name);
        var cap = new MeshCapturer(target, abc);
        m_capturers.Add(cap);
        return cap;
    }

    public SkinnedMeshCapturer CreateSkinnedMeshCapturer(SkinnedMeshRenderer target, aeAPI.aeObject parent)
    {
        var abc = aeAPI.aeNewPolyMesh(parent, target.name);
        var cap = new SkinnedMeshCapturer(target, abc);
        m_capturers.Add(cap);
        return cap;
    }

    public ParticleCapturer CreateParticleCapturer(ParticleSystem target, aeAPI.aeObject parent)
    {
        var abc = aeAPI.aeNewPoints(parent, target.name);
        var cap = new ParticleCapturer(target, abc);
        m_capturers.Add(cap);
        return cap;
    }

    public CustomCapturerHandler CreateCustomCapturerHandler(AlembicCustomComponentCapturer target, aeAPI.aeObject parent)
    {
        target.SetParent(parent);
        var cap = new CustomCapturerHandler(target);
        m_capturers.Add(cap);
        return cap;
    }

    bool ShouldBeIgnored(Behaviour target)
    {
        return m_ignoreDisabled && (!target.gameObject.activeInHierarchy || !target.enabled);
    }
    bool ShouldBeIgnored(Renderer target)
    {
        return m_ignoreDisabled && (!target.gameObject.activeInHierarchy || !target.enabled);
    }
    bool ShouldBeIgnored(ParticleSystem target)
    {
        return m_ignoreDisabled && (!target.gameObject.activeInHierarchy);
    }


    #region impl_capture_tree

    // capture node tree for "Preserve Tree Structure" option.
    public class CaptureNode
    {
        public List<CaptureNode> children = new List<CaptureNode>();

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

    void ConstructTree_TransformCapturer()
    {
        var abctop = aeAPI.aeGetTopObject(m_ctx);
        foreach (var c in m_top_nodes)
        {
            ConstructTree_TransformCapturer(c, abctop);
        }
    }

    void ConstructTree_TransformCapturer(CaptureNode node, aeAPI.aeObject parent)
    {
        node.transform = CreateTransformCapturer(node.obj.gameObject, parent, true);
        foreach (var c in node.children)
        {
            ConstructTree_TransformCapturer(c, node.transform.abc);
        }
    }
    #endregion

    void CreateCapturers_Tree()
    {
        m_capture_node = new Dictionary<Transform, CaptureNode>();
        m_top_nodes = new List<CaptureNode>();

        Camera[]                cameras         = m_captureCamera ? GetTargets<Camera>() : null;
        MeshRenderer[]          meshes          = m_captureMeshRenderer ? GetTargets<MeshRenderer>() : null;
        SkinnedMeshRenderer[]   skinned_meshes  = m_captureSkinnedMeshRenderer ? GetTargets<SkinnedMeshRenderer>() : null;
        ParticleSystem[]        particles       = m_captureParticleSystem ? GetTargets<ParticleSystem>() : null;
        AlembicCustomComponentCapturer[] custum_capturers = m_enableCustomCapturer ? GetTargets<AlembicCustomComponentCapturer>() : null;

        // construct tree
        // (bottom-up)
        if (cameras != null)
        {
            foreach (var t in cameras)
            {
                if (ShouldBeIgnored(t)) { return; }
                ConstructTree(t.GetComponent<Transform>());
            }
        }
        if (meshes != null)
        {
            foreach (var t in meshes)
            {
                if (ShouldBeIgnored(t)) { return; }
                ConstructTree(t.GetComponent<Transform>());
            }
        }
        if (skinned_meshes != null)
        {
            foreach (var t in skinned_meshes)
            {
                if (ShouldBeIgnored(t)) { return; }
                ConstructTree(t.GetComponent<Transform>());
            }
        }
        if (particles != null)
        {
            foreach (var t in particles)
            {
                if (ShouldBeIgnored(t)) { return; }
                ConstructTree(t.GetComponent<Transform>());
            }
        }
        if (custum_capturers != null)
        {
            foreach (var t in custum_capturers)
            {
                if (ShouldBeIgnored(t)) { return; }
                ConstructTree(t.GetComponent<Transform>());
            }
        }

        // make transform capturers (top-down)
        ConstructTree_TransformCapturer();

        // make component capturers
        if (cameras != null)
        {
            foreach (var t in cameras)
            {
                CaptureNode cn;
                if (m_capture_node.TryGetValue(t.GetComponent<Transform>(), out cn))
                {
                    cn.component = CreateCameraCapturer(t, cn.transform.abc);
                }
            }
        }
        if (meshes != null)
        {
            foreach (var t in meshes)
            {
                CaptureNode cn;
                if (m_capture_node.TryGetValue(t.GetComponent<Transform>(), out cn))
                {
                    cn.component = CreateMeshCapturer(t, cn.transform.abc);
                }
            }
        }
        if (skinned_meshes != null)
        {
            foreach (var t in skinned_meshes)
            {
                CaptureNode cn;
                if (m_capture_node.TryGetValue(t.GetComponent<Transform>(), out cn))
                {
                    cn.component = CreateSkinnedMeshCapturer(t, cn.transform.abc);
                }
            }
        }
        if (particles != null)
        {
            foreach (var t in particles)
            {
                CaptureNode cn;
                if (m_capture_node.TryGetValue(t.GetComponent<Transform>(), out cn))
                {
                    cn.component = CreateParticleCapturer(t, cn.transform.abc);
                }
            }
        }
        if (custum_capturers != null)
        {
            foreach (var t in custum_capturers)
            {
                CaptureNode cn;
                if (m_capture_node.TryGetValue(t.GetComponent<Transform>(), out cn))
                {
                    cn.component = CreateCustomCapturerHandler(t, cn.transform.abc);
                }
            }
        }

        m_top_nodes = null;
        m_capture_node = null;
    }

    void CreateCapturers_Flat()
    {
        var top = aeAPI.aeGetTopObject(m_ctx);

        // Camera
        if (m_captureCamera)
        {
            foreach (var target in GetTargets<Camera>())
            {
                if (ShouldBeIgnored(target)) { continue; }
                CreateCameraCapturer(target, CreateTransformCapturer(target.gameObject, top, false).abc);
            }
        }

        // MeshRenderer
        if (m_captureMeshRenderer)
        {
            foreach (var target in GetTargets<MeshRenderer>())
            {
                if (ShouldBeIgnored(target)) { continue; }
                CreateMeshCapturer(target, CreateTransformCapturer(target.gameObject, top, false).abc);
            }
        }

        // SkinnedMeshRenderer
        if (m_captureSkinnedMeshRenderer)
        {
            foreach (var target in GetTargets<SkinnedMeshRenderer>())
            {
                if (ShouldBeIgnored(target)) { continue; }
                CreateSkinnedMeshCapturer(target, CreateTransformCapturer(target.gameObject, top, false).abc);
            }
        }

        // ParticleSystem
        if (m_captureParticleSystem)
        {
            foreach (var target in GetTargets<ParticleSystem>())
            {
                if (ShouldBeIgnored(target)) { continue; }
                CreateParticleCapturer(target, CreateTransformCapturer(target.gameObject, top, false).abc);
            }
        }

        // handle custom capturers (AlembicCustomComponentCapturer)
        if (m_enableCustomCapturer)
        {
            foreach (var target in GetTargets<AlembicCustomComponentCapturer>())
            {
                if (ShouldBeIgnored(target)) { continue; }
                CreateCustomCapturerHandler(target, CreateTransformCapturer(target.gameObject, top, false).abc);
            }
        }
    }


    public bool BeginCapture()
    {
        if(m_recording) {
            Debug.Log("already recording");
            return false;
        }

        // create context and open archive
        m_ctx = aeAPI.aeCreateContext(ref m_conf);
        if(m_ctx.ptr == IntPtr.Zero) {
            Debug.LogWarning("aeCreateContext() failed");
            return false;
        }
        if(!aeAPI.aeOpenArchive(m_ctx, m_output_path)) {
            Debug.LogWarning("aeOpenArchive() failed");
            aeAPI.aeDestroyContext(m_ctx);
            m_ctx = new aeAPI.aeContext();
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
        m_time = 0.0f;

        Debug.Log("AlembicExporter: start " + m_output_path);
        return true;
    }

    public void EndCapture()
    {
        if (!m_recording) { return; }

        m_capturers.Clear();
        aeAPI.aeDestroyContext(m_ctx); // flush archive
        m_ctx = new aeAPI.aeContext();
        m_recording = false;
        m_frameCount = 0;

        Debug.Log("AlembicExporter: end: " + m_output_path);
    }

    public void OneShot()
    {
        if (BeginCapture())
        {
            aeAPI.aeAddTime(m_ctx, 0.0f);
            foreach (var recorder in m_capturers)
            {
                recorder.Capture();
            }
            EndCapture();
        }
    }

    IEnumerator ProcessRecording()
    {
        yield return new WaitForEndOfFrame();
        if(!m_recording) { yield break; }

        aeAPI.aeAddTime(m_ctx, m_time);
        foreach(var recorder in m_capturers) {
            recorder.Capture();
        }
        ++m_frameCount;


        // wait maximumDeltaTime if timeSamplingType is uniform
        if (m_conf.timeSamplingType == aeAPI.aeTypeSamplingType.Uniform)
        {
            Time.maximumDeltaTime = m_conf.timePerSample;
            aeAPI.aeWaitMaxDeltaTime();
        }
    }


    void Reset()
    {
        if(m_output_path == null || m_output_path == "")
        {
            m_output_path = gameObject.name + ".abc";
        }
    }

    void Update()
    {
        if(m_recording)
        {
            m_time += Time.deltaTime;
            StartCoroutine(ProcessRecording());
        }
    }

    void OnDisable()
    {
        EndCapture();
    }
}
