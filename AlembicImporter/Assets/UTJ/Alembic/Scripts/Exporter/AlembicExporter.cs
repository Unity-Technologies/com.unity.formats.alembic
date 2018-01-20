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
    public class AlembicExporter : MonoBehaviour
    {
        #region impl

        public static void CaptureTransform(
            AbcAPI.aeObject abc, Transform trans,
            bool inherits, bool invertForward, bool scale)
        {
            AbcAPI.aeXFormData data;

            data.inherits = inherits;
            if (invertForward) { trans.forward = trans.forward * -1.0f; }
            if (inherits)
            {
                data.translation = trans.localPosition;
                data.rotation = trans.localRotation;
                data.scale = scale ? trans.localScale : Vector3.one;
            }
            else
            {
                data.translation = trans.position;
                data.rotation = trans.rotation;
                data.scale = scale ? trans.lossyScale : Vector3.one;
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
            public PinnedList<int> indices = new PinnedList<int>();
            public PinnedList<Vector3> vertices = new PinnedList<Vector3>();
            public PinnedList<Vector3> normals = new PinnedList<Vector3>();
            public PinnedList<Vector2> uvs = new PinnedList<Vector2>();

            public void Clear()
            {
                indices.Clear();
                vertices.Clear();
                normals.Clear();
                uvs.Clear();
            }

            public void Capture(Mesh mesh)
            {
                Clear();
                indices.Assign(mesh.triangles); // todo: this can be optimized
                vertices.LockList(ls => mesh.GetVertices(ls));
                normals.LockList(ls => mesh.GetNormals(ls));
                uvs.LockList(ls => mesh.GetUVs(0, ls));
            }
        }

        public class ClothBuffer
        {
            public PinnedList<int> remap = new PinnedList<int>();
            public PinnedList<Vector3> vertices = new PinnedList<Vector3>();
            public PinnedList<Vector3> normals = new PinnedList<Vector3>();

            public PinnedList<int> indicesFlattended = new PinnedList<int>();
            public PinnedList<Vector3> verticesFlattended = new PinnedList<Vector3>();
            public PinnedList<Vector3> normalsFlattended = new PinnedList<Vector3>();
            public PinnedList<Vector2> uvsFlattened = new PinnedList<Vector2>();

            public void Clear()
            {
                remap.Clear();
                vertices.Clear();
                normals.Clear();

                indicesFlattended.Clear();
                verticesFlattended.Clear();
                normalsFlattended.Clear();
                uvsFlattened.Clear();
            }

            [DllImport("abci")] public static extern void aeGenerateRemap(IntPtr dst, IntPtr points, int pointCount);
            public void GenerateRemap(MeshBuffer mbuf)
            {
                remap.Resize(mbuf.vertices.Count);
                aeGenerateRemap(remap, mbuf.vertices, mbuf.vertices.Count);

                indicesFlattended.ResizeDiscard(mbuf.indices.Count);
                for (int ii = 0; ii < indicesFlattended.Count; ++ii)
                    indicesFlattended[ii] = ii;
            }

            public void Capture(Cloth cloth, MeshBuffer mbuf)
            {
                vertices.Assign(cloth.vertices);
                normals.Assign(cloth.normals);

                // flatten
                verticesFlattended.ResizeDiscard(mbuf.indices.Count);
                normalsFlattended.ResizeDiscard(mbuf.indices.Count);
                for (int ii = 0; ii < mbuf.indices.Count; ++ii)
                {
                    int vi = remap[mbuf.indices[ii]];
                    verticesFlattended[ii] = vertices[vi];
                    normalsFlattended[ii] = normals[vi];
                }
                if (mbuf.uvs.Count > 0)
                {
                    uvsFlattened.ResizeDiscard(mbuf.indices.Count);
                    for (int ii = 0; ii < mbuf.indices.Count; ++ii)
                        uvsFlattened[ii] = mbuf.uvs[mbuf.indices[ii]];
                }
            }
        }

        public static void CaptureMesh(AbcAPI.aeObject abc, Mesh mesh, MeshBuffer mbuf)
        {
            mbuf.Capture(mesh);

            var data = new AbcAPI.aePolyMeshData();
            data.indices = mbuf.indices;
            data.positions = mbuf.vertices;
            data.normals = mbuf.normals;
            data.uvs = mbuf.uvs;
            data.positionCount = mbuf.vertices.Count;
            data.indexCount = mbuf.indices.Count;

            AbcAPI.aePolyMeshWriteSample(abc, ref data);
        }

        public static void CaptureCloth(AbcAPI.aeObject abc, Mesh mesh, Cloth cloth, MeshBuffer mbuf, ClothBuffer cbuf)
        {
            if (mbuf.indices.Count == 0)
            {
                mbuf.Capture(mesh);
                cbuf.GenerateRemap(mbuf);
            }
            cbuf.Capture(cloth, mbuf);

            var data = new AbcAPI.aePolyMeshData();
            data.indices = cbuf.indicesFlattended;
            data.positions = cbuf.verticesFlattended;
            data.normals = cbuf.normalsFlattended;
            data.uvs = cbuf.uvsFlattened;
            data.positionCount = cbuf.verticesFlattended.Count;
            data.indexCount = cbuf.indicesFlattended.Count;

            AbcAPI.aePolyMeshWriteSample(abc, ref data);
        }



        public abstract class ComponentCapturer
        {
            protected ComponentCapturer m_parent;
            protected GameObject m_obj;
            protected AbcAPI.aeObject m_abc;

            public ComponentCapturer parent { get { return m_parent; } }
            public GameObject obj { get { return m_obj; } }
            public AbcAPI.aeObject abc { get { return m_abc; } }
            public abstract void Capture();

            protected ComponentCapturer(ComponentCapturer p)
            {
                m_parent = p;
            }
        }

        public class RootCapturer : ComponentCapturer
        {
            public RootCapturer(AbcAPI.aeObject abc)
                : base(null)
            {
                m_abc = abc;
            }

            public override void Capture()
            {
                // do nothing
            }
        }

        public class TransformCapturer : ComponentCapturer
        {
            Transform m_target;
            bool m_inherits = false;
            bool m_invertForward = false;
            bool m_scale = true;

            public bool inherits {
                get { return m_inherits; }
                set { m_inherits = value; }
            }
            public bool invertForward
            {
                get { return m_invertForward; }
                set { m_invertForward = value; }
            }
            public bool scale
            {
                get { return m_scale; }
                set { m_scale = value; }
            }

            public TransformCapturer(ComponentCapturer parent, Transform target)
                : base(parent)
            {
                m_obj = target.gameObject;
                m_abc = AbcAPI.aeNewXForm(parent.abc, target.name + " (" + target.GetInstanceID().ToString("X8") + ")");
                m_target = target;
                m_inherits = inherits;
            }

            public override void Capture()
            {
                if (m_target == null) { return; }

                CaptureTransform(m_abc, m_target, m_inherits, m_invertForward, m_scale);
            }
        }

        public class CameraCapturer : ComponentCapturer
        {
            Camera m_target;
            AlembicCameraParams m_params;

            public CameraCapturer(ComponentCapturer parent, Camera target)
                : base(parent)
            {
                m_obj = target.gameObject;
                m_abc = AbcAPI.aeNewCamera(parent.abc, target.name);
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

            public MeshCapturer(ComponentCapturer parent, MeshRenderer target)
                : base(parent)
            {
                m_obj = target.gameObject;
                m_abc = AbcAPI.aeNewPolyMesh(parent.abc, target.name);
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
            Mesh m_meshSrc;
            Mesh m_meshBake;
            Cloth m_cloth;
            MeshBuffer m_mesh_buffer;
            ClothBuffer m_cloth_buffer;

            public SkinnedMeshCapturer(ComponentCapturer parent, SkinnedMeshRenderer target)
                : base(parent)
            {
                m_obj = target.gameObject;
                m_abc = AbcAPI.aeNewPolyMesh(parent.abc, target.name);
                m_target = target;

                if (m_target != null)
                {
                    m_mesh_buffer = new MeshBuffer();
                    m_meshSrc = target.sharedMesh;
                    m_cloth = m_target.GetComponent<Cloth>();
                    if (m_cloth != null)
                    {
                        m_cloth_buffer = new ClothBuffer();
                        var t = m_parent as TransformCapturer;
                        if (t != null)
                            t.scale = false;
                    }
                    else
                    {
                        m_meshBake = new Mesh();
                    }

                }
            }

            public override void Capture()
            {
                if (m_target == null) { return; }

                if (m_cloth != null)
                {
                    CaptureCloth(m_abc, m_meshSrc, m_cloth, m_mesh_buffer, m_cloth_buffer);
                }
                else
                {
                    m_target.BakeMesh(m_meshBake);
                    CaptureMesh(m_abc, m_meshBake, m_mesh_buffer);
                }
            }
        }

        public class ParticleCapturer : ComponentCapturer
        {
            ParticleSystem m_target;
            AbcAPI.aeProperty m_prop_rotatrions;

            ParticleSystem.Particle[] m_buf_particles;
            PinnedList<Vector3> m_buf_positions = new PinnedList<Vector3>();
            PinnedList<Vector4> m_buf_rotations = new PinnedList<Vector4>();

            public ParticleCapturer(ComponentCapturer parent, ParticleSystem target)
                : base(parent)
            {
                m_obj = target.gameObject;
                m_abc = AbcAPI.aeNewPoints(parent.abc, target.name);
                m_target = target;

                m_prop_rotatrions = AbcAPI.aeNewProperty(m_abc, "rotation", AbcAPI.aePropertyType.Float4Array);
            }

            public override void Capture()
            {
                if (m_target == null) { return; }

                // create buffer
#if UNITY_5_5_OR_NEWER
                int count_max = m_target.main.maxParticles;
#else
                int count_max = m_target.maxParticles;
#endif
                if (m_buf_particles == null)
                {
                    m_buf_particles = new ParticleSystem.Particle[count_max];
                    m_buf_positions.Resize(count_max);
                    m_buf_rotations.Resize(count_max);
                }
                else if (m_buf_particles.Length != count_max)
                {
                    Array.Resize(ref m_buf_particles, count_max);
                    m_buf_positions.Resize(count_max);
                    m_buf_rotations.Resize(count_max);
                }

                // copy particle positions & rotations to buffer
                int count = m_target.GetParticles(m_buf_particles);
                for (int i = 0; i < count; ++i)
                {
                    m_buf_positions[i] = m_buf_particles[i].position;
                }
                for (int i = 0; i < count; ++i)
                {
                    var a = m_buf_particles[i].axisOfRotation;
                    m_buf_rotations[i].Set(a.x, a.y, a.z, m_buf_particles[i].rotation);
                }

                // write!
                var data = new AbcAPI.aePointsData();
                data.positions = m_buf_positions;
                data.count = count;
                AbcAPI.aePointsWriteSample(m_abc, ref data);
                AbcAPI.aePropertyWriteArraySample(m_prop_rotatrions, m_buf_rotations, count);
            }
        }

        public class CustomCapturerHandler : ComponentCapturer
        {
            AlembicCustomComponentCapturer m_target;

            public CustomCapturerHandler(ComponentCapturer parent, AlembicCustomComponentCapturer target)
                : base(parent)
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

        public string m_outputPath;
        public AbcAPI.aeConfig m_conf = AbcAPI.aeConfig.default_value;
        public Scope m_scope = Scope.EntireScene;
        public bool m_preserveTreeStructure = true;
        public bool m_ignoreDisabled = true;
        public bool m_captureMeshRenderer = true;
        public bool m_captureSkinnedMeshRenderer = true;
        public bool m_captureParticleSystem = true;
        public bool m_captureCamera = true;
        public bool m_customCapturer = true;
        public bool m_captureOnStart = false;
        public int m_maxCaptureFrame = 0;
        public bool m_detailedLog;

        AbcAPI.aeContext m_ctx;
        ComponentCapturer m_root;
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


        public TransformCapturer CreateComponentCapturer(ComponentCapturer parent, Transform target)
        {
            if (m_detailedLog) { Debug.Log("AlembicExporter: new TransformCapturer(\"" + target.name + "\""); }

            var cap = new TransformCapturer(parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        public CameraCapturer CreateComponentCapturer(ComponentCapturer parent, Camera target)
        {
            if (m_detailedLog) { Debug.Log("AlembicExporter: new CameraCapturer(\"" + target.name + "\""); }

            var cap = new CameraCapturer(parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        public MeshCapturer CreateComponentCapturer(ComponentCapturer parent, MeshRenderer target)
        {
            if (m_detailedLog) { Debug.Log("AlembicExporter: new MeshCapturer(\"" + target.name + "\""); }

            var cap = new MeshCapturer(parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        public SkinnedMeshCapturer CreateComponentCapturer(ComponentCapturer parent, SkinnedMeshRenderer target)
        {
            if (m_detailedLog) { Debug.Log("AlembicExporter: new SkinnedMeshCapturer(\"" + target.name + "\""); }

            var cap = new SkinnedMeshCapturer(parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        public ParticleCapturer CreateComponentCapturer(ComponentCapturer parent, ParticleSystem target)
        {
            if (m_detailedLog) { Debug.Log("AlembicExporter: new ParticleCapturer(\"" + target.name + "\""); }

            var cap = new ParticleCapturer(parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        public CustomCapturerHandler CreateComponentCapturer(ComponentCapturer parent, AlembicCustomComponentCapturer target)
        {
            if (m_detailedLog) { Debug.Log("AlembicExporter: new CustomCapturerHandler(\"" + target.name + "\""); }

            target.CreateAbcObject(parent.abc);
            var cap = new CustomCapturerHandler(parent, target);
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
        #endregion

        void CreateCapturers_Tree()
        {
            m_root = new RootCapturer(AbcAPI.aeGetTopObject(m_ctx));
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

        void CreateCapturers_Flat()
        {
            m_root = new RootCapturer(AbcAPI.aeGetTopObject(m_ctx));

            // Camera
            if (m_captureCamera)
            {
                foreach (var target in GetTargets<Camera>())
                {
                    if (ShouldBeIgnored(target)) { continue; }
                    var trans = CreateComponentCapturer(m_root, target.GetComponent<Transform>());
                    trans.inherits = false;
                    trans.invertForward = true;
                    CreateComponentCapturer(trans, target);
                }
            }

            // MeshRenderer
            if (m_captureMeshRenderer)
            {
                foreach (var target in GetTargets<MeshRenderer>())
                {
                    if (ShouldBeIgnored(target)) { continue; }
                    var trans = CreateComponentCapturer(m_root, target.GetComponent<Transform>());
                    trans.inherits = false;
                    CreateComponentCapturer(trans, target);
                }
            }

            // SkinnedMeshRenderer
            if (m_captureSkinnedMeshRenderer)
            {
                foreach (var target in GetTargets<SkinnedMeshRenderer>())
                {
                    if (ShouldBeIgnored(target)) { continue; }
                    var trans = CreateComponentCapturer(m_root, target.GetComponent<Transform>());
                    trans.inherits = false;
                    CreateComponentCapturer(trans, target);
                }
            }

            // ParticleSystem
            if (m_captureParticleSystem)
            {
                foreach (var target in GetTargets<ParticleSystem>())
                {
                    if (ShouldBeIgnored(target)) { continue; }
                    var trans = CreateComponentCapturer(m_root, target.GetComponent<Transform>());
                    trans.inherits = false;
                    CreateComponentCapturer(trans, target);
                }
            }

            // handle custom capturers (AlembicCustomComponentCapturer)
            if (m_customCapturer)
            {
                foreach (var target in GetTargets<AlembicCustomComponentCapturer>())
                {
                    if (ShouldBeIgnored(target)) { continue; }
                    var trans = CreateComponentCapturer(m_root, target.GetComponent<Transform>());
                    trans.inherits = false;
                    CreateComponentCapturer(trans, target);
                }
            }
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

            if (m_conf.timeSamplingType == AbcAPI.aeTimeSamplingType.Uniform)
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
            if (m_conf.timeSamplingType == AbcAPI.aeTimeSamplingType.Uniform)
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
}
