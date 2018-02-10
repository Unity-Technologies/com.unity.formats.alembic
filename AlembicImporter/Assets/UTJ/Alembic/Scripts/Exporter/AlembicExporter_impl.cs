using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif



namespace UTJ.Alembic
{
    public enum ExportScope
    {
        EntireScene,
        CurrentBranch,
    }

    [Serializable]
    public class AlembicRecorderSettings
    {
        public string outputPath;
        public aeConfig conf = aeConfig.defaultValue;
        public ExportScope scope = ExportScope.EntireScene;
        public bool fixDeltaTime = true;

        public bool ignoreDisabled = true;
        public bool assumeNonSkinnedMeshesAreConstant = true;

        public bool captureMeshRenderer = true;
        public bool captureSkinnedMeshRenderer = true;
        public bool captureParticleSystem = true;
        public bool captureCamera = true;
        public bool customCapturer = true;

        public bool meshNormals = true;
        public bool meshUV0 = true;
        public bool meshUV1 = true;
        public bool meshColors = true;

        public bool detailedLog = true;
        public bool debugLog = false;
    }

    [Serializable]
    public class AlembicRecorder : IDisposable
    {
        #region internal types
        public class MeshBuffer
        {
            public PinnedList<int> indices = new PinnedList<int>();
            public PinnedList<Vector3> points = new PinnedList<Vector3>();
            public PinnedList<Vector3> normals = new PinnedList<Vector3>();
            public PinnedList<Vector2> uv0 = new PinnedList<Vector2>();
            public PinnedList<Vector2> uv1 = new PinnedList<Vector2>();
            public PinnedList<Color> colors = new PinnedList<Color>();
            public List<PinnedList<int>> facesets = new List<PinnedList<int>>();
            PinnedList<int> tmpIndices = new PinnedList<int>();

            public void SetupSubmeshes(aeObject abc, Mesh mesh, Material[] materials)
            {
                if (mesh.subMeshCount > 1)
                {
                    for (int smi = 0; smi < mesh.subMeshCount; ++smi)
                    {
                        string name;
                        if (smi < materials.Length && materials[smi] != null)
                            name = materials[smi].name;
                        else
                            name = string.Format("submesh[{0}]", smi);
                        abc.AddFaceSet(name);
                    }
                }
            }

            public void Capture(Mesh mesh,
                bool captureNormals, bool captureUV0, bool captureUV1, bool captureColors)
            {
                points.LockList(ls => mesh.GetVertices(ls));

                if (captureNormals)
                    normals.LockList(ls => mesh.GetNormals(ls));
                else
                    normals.Clear();

                if (captureUV0)
                    uv0.LockList(ls => mesh.GetUVs(0, ls));
                else
                    uv0.Clear();

                if (captureUV1)
                    uv1.LockList(ls => mesh.GetUVs(1, ls));
                else
                    uv1.Clear();

                if (captureColors)
                    colors.LockList(ls => mesh.GetColors(ls));
                else
                    colors.Clear();

                {
                    int submeshCount = mesh.subMeshCount;
                    if (submeshCount == 1)
                    {
                        indices.LockList(ls => mesh.GetTriangles(ls, 0));
                    }
                    else
                    {
                        indices.Assign(mesh.triangles);

                        while (facesets.Count < submeshCount)
                            facesets.Add(new PinnedList<int>());

                        int offsetTriangle = 0;
                        for (int smi = 0; smi < submeshCount; ++smi)
                        {
                            tmpIndices.LockList(ls => { mesh.GetTriangles(ls, smi); });
                            int numTriangles = tmpIndices.Count / 3;
                            facesets[smi].ResizeDiscard(numTriangles);
                            for (int ti = 0; ti < numTriangles; ++ti)
                                facesets[smi][ti] = ti + offsetTriangle;
                            offsetTriangle += numTriangles;
                        }
                    }
                }
            }

            public void Capture(Mesh mesh, AlembicRecorderSettings settings)
            {
                Capture(mesh, settings.meshNormals, settings.meshUV0, settings.meshUV1, settings.meshColors);
            }


            public void WriteSample(aeObject abc)
            {
                {
                    var data = default(aePolyMeshData);
                    data.indices = indices;
                    data.indexCount = indices.Count;
                    data.points = points;
                    data.pointCount = points.Count;
                    data.normals = normals;
                    data.uv0 = uv0;
                    data.uv1 = uv1;
                    data.colors = colors;
                    abc.WriteSample(ref data);
                }
                for (int fsi = 0; fsi < facesets.Count; ++fsi)
                {
                    var data = default(aeFaceSetData);
                    data.faces = facesets[fsi];
                    data.faceCount = facesets[fsi].Count;
                    abc.WriteFaceSetSample(fsi, ref data);
                }
            }
        }

        public class ClothBuffer
        {
            public PinnedList<int> remap = new PinnedList<int>();
            public PinnedList<Vector3> vertices = new PinnedList<Vector3>();
            public PinnedList<Vector3> normals = new PinnedList<Vector3>();
            public Transform rootBone;
            public int numRemappedVertices;

            [DllImport("abci")] static extern int aeGenerateRemapIndices(IntPtr dstIndices, IntPtr points, IntPtr weights4, int numPoints);
            [DllImport("abci")] static extern void aeApplyMatrixP(IntPtr dstPoints, int num, ref Matrix4x4 mat);
            [DllImport("abci")] static extern void aeApplyMatrixV(IntPtr dstVectors, int num, ref Matrix4x4 mat);
            void GenerateRemapIndices(Mesh mesh, MeshBuffer mbuf)
            {
                mbuf.Capture(mesh, false, false, false, false);
                var weights4 = new PinnedList<BoneWeight>();
                weights4.LockList(l => { mesh.GetBoneWeights(l); });

                remap.Resize(mbuf.points.Count);
                numRemappedVertices = aeGenerateRemapIndices(remap, mbuf.points, weights4, mbuf.points.Count);
            }

            public void Capture(Mesh mesh, Cloth cloth, MeshBuffer mbuf, AlembicRecorderSettings settings)
            {
                if (mesh == null || cloth == null)
                    return;
                if (remap.Count != mesh.vertexCount)
                    GenerateRemapIndices(mesh, mbuf);

                // capture cloth points and normals
                vertices.Assign(cloth.vertices);
                if (numRemappedVertices != vertices.Count)
                {
                    Debug.LogWarning("numRemappedVertices != vertices.Count");
                    return;
                }

                if (settings.meshNormals)
                    normals.Assign(cloth.normals);
                else
                    normals.Clear();

                // apply root bone transform
                if (rootBone != null)
                {
                    var mat = Matrix4x4.TRS(rootBone.localPosition, rootBone.localRotation, Vector3.one);
                    aeApplyMatrixP(vertices, vertices.Count, ref mat);
                    aeApplyMatrixV(normals, normals.Count, ref mat);
                }

                // remap vertices and normals
                for (int vi = 0; vi < remap.Count; ++vi)
                    mbuf.points[vi] = vertices[remap[vi]];
                if (normals.Count > 0)
                {
                    mbuf.normals.ResizeDiscard(remap.Count);
                    for (int vi = 0; vi < remap.Count; ++vi)
                        mbuf.normals[vi] = normals[remap[vi]];
                }

                // capture other components
                if (settings.meshUV0)
                    mbuf.uv0.LockList(ls => mesh.GetUVs(0, ls));
                else
                    mbuf.uv0.Clear();

                if (settings.meshUV1)
                    mbuf.uv1.LockList(ls => mesh.GetUVs(1, ls));
                else
                    mbuf.uv1.Clear();

                if (settings.meshColors)
                    mbuf.colors.LockList(ls => mesh.GetColors(ls));
                else
                    mbuf.colors.Clear();

            }
        }


        public abstract class ComponentCapturer
        {
            protected AlembicRecorder m_recorder;
            protected ComponentCapturer m_parent;
            protected GameObject m_obj;
            protected aeObject m_abc;

            public ComponentCapturer parent { get { return m_parent; } }
            public GameObject obj { get { return m_obj; } }
            public aeObject abc { get { return m_abc; } }
            public abstract void Capture();

            protected ComponentCapturer(AlembicRecorder exp, ComponentCapturer p, Component c)
            {
                m_recorder = exp;
                m_parent = p;
                m_obj = c != null ? c.gameObject : null;
            }
        }

        public class RootCapturer : ComponentCapturer
        {
            public RootCapturer(AlembicRecorder exp, aeObject abc)
                : base(exp, null, null)
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
            bool m_capturePosition = true;
            bool m_captureRotation = true;
            bool m_captureScale = true;

            public bool inherits { set { m_inherits = value; } }
            public bool invertForward { set { m_invertForward = value; } }
            public bool capturePosition { set { m_capturePosition = value; } }
            public bool captureRotation { set { m_captureRotation = value; } }
            public bool captureScale { set { m_captureScale = value; } }

            public TransformCapturer(AlembicRecorder exp, ComponentCapturer parent, Transform target)
                : base(exp, parent, target)
            {
                m_abc = parent.abc.NewXform(target.name + " (" + target.GetInstanceID().ToString("X8") + ")");
                m_target = target;
            }

            public override void Capture()
            {
                if (m_target == null) { return; }
                var trans = m_target;

                aeXformData data;
                if (m_invertForward) { trans.forward = trans.forward * -1.0f; }
                data.inherits = m_inherits;
                if (m_inherits)
                {
                    data.translation = m_capturePosition ? trans.localPosition : Vector3.zero;
                    data.rotation = m_captureRotation ? trans.localRotation : Quaternion.identity;
                    data.scale = m_captureScale ? trans.localScale : Vector3.one;
                }
                else
                {
                    data.translation = m_capturePosition ? trans.position : Vector3.zero;
                    data.rotation = m_captureRotation ? trans.rotation : Quaternion.identity;
                    data.scale = m_captureScale ? trans.lossyScale : Vector3.one;
                }
                if (m_invertForward) { trans.forward = trans.forward * -1.0f; }
                abc.WriteSample(ref data);
            }
        }

        public class CameraCapturer : ComponentCapturer
        {
            Camera m_target;
            AlembicCameraParams m_params;

            public CameraCapturer(AlembicRecorder exp, ComponentCapturer parent, Camera target)
                : base(exp, parent, target)
            {
                m_abc = parent.abc.NewCamera(target.name);
                m_target = target;
                m_params = target.GetComponent<AlembicCameraParams>();
            }

            public override void Capture()
            {
                if (m_target == null) { return; }
                var cam = m_target;

                var data = aeCameraData.defaultValue;
                data.nearClippingPlane = cam.nearClipPlane;
                data.farClippingPlane = cam.farClipPlane;
                data.fieldOfView = cam.fieldOfView;
                if (m_params != null)
                {
                    data.focalLength = m_params.m_focalLength;
                    data.focusDistance = m_params.m_focusDistance;
                    data.aperture = m_params.m_aperture;
                    data.aspectRatio = m_params.GetAspectRatio();
                }
                abc.WriteSample(ref data);
            }
        }

        public class MeshCapturer : ComponentCapturer
        {
            MeshRenderer m_target;
            MeshBuffer m_mbuf;

            public MeshCapturer(AlembicRecorder exp, ComponentCapturer parent, MeshRenderer target)
                : base(exp, parent, target)
            {
                m_target = target;
                var mesh = m_target.GetComponent<MeshFilter>().sharedMesh;
                if (mesh == null)
                    return;

                m_abc = parent.abc.NewPolyMesh(target.name);
                m_mbuf = new MeshBuffer();
                m_mbuf.SetupSubmeshes(m_abc, mesh, m_target.sharedMaterials);
            }

            public override void Capture()
            {
                if (m_target == null) { return; }

                var mesh = m_target.GetComponent<MeshFilter>().sharedMesh;
                if (mesh == null || (m_recorder.m_settings.assumeNonSkinnedMeshesAreConstant && m_mbuf.points.Capacity != 0))
                    return;

                m_mbuf.Capture(mesh, m_recorder.m_settings);
                m_mbuf.WriteSample(abc);
            }
        }

        public class SkinnedMeshCapturer : ComponentCapturer
        {
            SkinnedMeshRenderer m_target;
            Mesh m_meshSrc;
            Mesh m_meshBake;
            Cloth m_cloth;
            MeshBuffer m_mbuf;
            ClothBuffer m_cbuf;

            public SkinnedMeshCapturer(AlembicRecorder exp, ComponentCapturer parent, SkinnedMeshRenderer target)
                : base(exp, parent, target)
            {
                m_target = target;
                var mesh = target.sharedMesh;
                if (mesh == null)
                    return;

                m_abc = parent.abc.NewPolyMesh(target.name);
                m_mbuf = new MeshBuffer();
                m_mbuf.SetupSubmeshes(m_abc, mesh, m_target.sharedMaterials);

                m_meshSrc = target.sharedMesh;
                m_cloth = m_target.GetComponent<Cloth>();
                if (m_cloth != null)
                {
                    m_cbuf = new ClothBuffer();
                    m_cbuf.rootBone = m_target.rootBone != null ? m_target.rootBone : m_target.GetComponent<Transform>();

                    var tc = m_parent as TransformCapturer;
                    if (tc != null)
                    {
                        tc.capturePosition = false;
                        tc.captureRotation = false;
                        tc.captureScale = false;
                    }
                }
            }

            public override void Capture()
            {
                if (m_target == null) { return; }

                if (m_cloth != null)
                {
                    m_cbuf.Capture(m_meshSrc, m_cloth, m_mbuf, m_recorder.m_settings);
                    m_mbuf.WriteSample(m_abc);
                }
                else
                {
                    if (m_meshBake == null)
                        m_meshBake = new Mesh();
                    m_meshBake.Clear();
                    m_target.BakeMesh(m_meshBake);

                    m_mbuf.Capture(m_meshBake, m_recorder.m_settings);
                    m_mbuf.WriteSample(m_abc);
                }
            }
        }

        public class ParticleCapturer : ComponentCapturer
        {
            ParticleSystem m_target;
            aeProperty m_prop_rotatrions;

            ParticleSystem.Particle[] m_buf_particles;
            PinnedList<Vector3> m_buf_positions = new PinnedList<Vector3>();
            PinnedList<Vector4> m_buf_rotations = new PinnedList<Vector4>();

            public ParticleCapturer(AlembicRecorder exp, ComponentCapturer parent, ParticleSystem target)
                : base(exp, parent, target)
            {
                m_abc = parent.abc.NewPoints(target.name);
                m_target = target;

                m_prop_rotatrions = m_abc.NewProperty("rotation", aePropertyType.Float4Array);
            }

            public override void Capture()
            {
                if (m_target == null) { return; }

                // create buffer
                int count_max = m_target.main.maxParticles;
                if (m_buf_particles == null || m_buf_particles.Length != count_max)
                {
                    m_buf_particles = new ParticleSystem.Particle[count_max];
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
                var data = new aePointsData();
                data.positions = m_buf_positions;
                data.count = count;
                m_abc.WriteSample(ref data);
                m_prop_rotatrions.WriteArraySample(m_buf_rotations, count);
            }
        }

        public class CustomCapturerHandler : ComponentCapturer
        {
            AlembicCustomComponentCapturer m_target;

            public CustomCapturerHandler(AlembicRecorder exp, ComponentCapturer parent, AlembicCustomComponentCapturer target)
                : base(exp, parent, target)
            {
                m_target = target;
            }

            public override void Capture()
            {
                if (m_target == null) { return; }

                m_target.Capture();
            }
        }
        #endregion


        #region fields
        [SerializeField] AlembicRecorderSettings m_settings = new AlembicRecorderSettings();
        [SerializeField] GameObject m_targetBranch;

        aeContext m_ctx;
        ComponentCapturer m_root;
        List<ComponentCapturer> m_capturers = new List<ComponentCapturer>();
        bool m_recording;
        float m_time;
        float m_elapsed;
        int m_frameCount;
        #endregion


        #region properties
        public AlembicRecorderSettings settings { get { return m_settings; } }
        public GameObject targetBranch { get { return m_targetBranch; } set { m_targetBranch = value; } }
        public bool recording { get { return m_recording; } }
        public int frameCount { get { return m_frameCount; } }
        #endregion


        #region private methods
#if UNITY_EDITOR
        public static void ForceDisableBatching()
        {
            var method = typeof(UnityEditor.PlayerSettings).GetMethod("SetBatchingForPlatform", BindingFlags.NonPublic | BindingFlags.Static);
            if (method != null)
            {
                method.Invoke(null, new object[] { BuildTarget.StandaloneWindows, 0, 0 });
                method.Invoke(null, new object[] { BuildTarget.StandaloneWindows64, 0, 0 });
            }
        }
#endif

        T[] GetTargets<T>() where T : Component
        {
            if (m_settings.scope == ExportScope.CurrentBranch && m_targetBranch != null)
            {
                return m_targetBranch.GetComponentsInChildren<T>();
            }
            else
            {
                return GameObject.FindObjectsOfType<T>();
            }
        }


        TransformCapturer CreateComponentCapturer(ComponentCapturer parent, Transform target)
        {
            if (m_settings.debugLog) { Debug.Log("AlembicRecorder: new TransformCapturer(\"" + target.name + "\")"); }

            var cap = new TransformCapturer(this, parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        CameraCapturer CreateComponentCapturer(ComponentCapturer parent, Camera target)
        {
            if (m_settings.debugLog) { Debug.Log("AlembicRecorder: new CameraCapturer(\"" + target.name + "\")"); }

            var cap = new CameraCapturer(this, parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        MeshCapturer CreateComponentCapturer(ComponentCapturer parent, MeshRenderer target)
        {
            if (m_settings.debugLog) { Debug.Log("AlembicRecorder: new MeshCapturer(\"" + target.name + "\")"); }

            var cap = new MeshCapturer(this, parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        SkinnedMeshCapturer CreateComponentCapturer(ComponentCapturer parent, SkinnedMeshRenderer target)
        {
            if (m_settings.debugLog) { Debug.Log("AlembicRecorder: new SkinnedMeshCapturer(\"" + target.name + "\")"); }

            var cap = new SkinnedMeshCapturer(this, parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        ParticleCapturer CreateComponentCapturer(ComponentCapturer parent, ParticleSystem target)
        {
            if (m_settings.debugLog) { Debug.Log("AlembicRecorder: new ParticleCapturer(\"" + target.name + "\")"); }

            var cap = new ParticleCapturer(this, parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        CustomCapturerHandler CreateComponentCapturer(ComponentCapturer parent, AlembicCustomComponentCapturer target)
        {
            if (m_settings.debugLog) { Debug.Log("AlembicRecorder: new CustomCapturerHandler(\"" + target.name + "\")"); }

            target.CreateAbcObject(parent.abc);
            var cap = new CustomCapturerHandler(this, parent, target);
            m_capturers.Add(cap);
            return cap;
        }

        bool ShouldBeIgnored(Behaviour target)
        {
            return m_settings.ignoreDisabled && (!target.gameObject.activeInHierarchy || !target.enabled);
        }
        bool ShouldBeIgnored(ParticleSystem target)
        {
            return m_settings.ignoreDisabled && (!target.gameObject.activeInHierarchy);
        }
        bool ShouldBeIgnored(MeshRenderer target)
        {
            if (m_settings.ignoreDisabled && (!target.gameObject.activeInHierarchy || !target.enabled)) { return true; }
            if (target.GetComponent<MeshFilter>().sharedMesh == null) { return true; }
            return false;
        }
        bool ShouldBeIgnored(SkinnedMeshRenderer target)
        {
            if (m_settings.ignoreDisabled && (!target.gameObject.activeInHierarchy || !target.enabled)) { return true; }
            if (target.sharedMesh == null) { return true; }
            return false;
        }


        // capture node tree for "Preserve Tree Structure" option.
        class CaptureNode
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
            if (node == null) { return null; }

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

            if (node.componentType == null)
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
            if (m_settings.captureCamera)
            {
                foreach (var t in GetTargets<Camera>())
                {
                    if (ShouldBeIgnored(t)) { continue; }
                    var node = ConstructTree(t.GetComponent<Transform>());
                    node.componentType = t.GetType();
                }
            }
            if (m_settings.captureMeshRenderer)
            {
                foreach (var t in GetTargets<MeshRenderer>())
                {
                    if (ShouldBeIgnored(t)) { continue; }
                    var node = ConstructTree(t.GetComponent<Transform>());
                    node.componentType = t.GetType();
                }
            }
            if (m_settings.captureSkinnedMeshRenderer)
            {
                foreach (var t in GetTargets<SkinnedMeshRenderer>())
                {
                    if (ShouldBeIgnored(t)) { continue; }
                    var node = ConstructTree(t.GetComponent<Transform>());
                    node.componentType = t.GetType();
                }
            }
            if (m_settings.captureParticleSystem)
            {
                foreach (var t in GetTargets<ParticleSystem>())
                {
                    if (ShouldBeIgnored(t)) { continue; }
                    var node = ConstructTree(t.GetComponent<Transform>());
                    node.componentType = t.GetType();
                }
            }
            if (m_settings.customCapturer)
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
        #endregion


        #region public methods
        public void Dispose()
        {
            m_ctx.Destroy();
        }

        public bool BeginRecording()
        {
            if (m_recording)
            {
                Debug.Log("AlembicRecorder: already recording");
                return false;
            }

            {
                var dir = Path.GetDirectoryName(m_settings.outputPath);
                if (!Directory.Exists(dir))
                {
                    try
                    {
                        Directory.CreateDirectory(dir);
                    }
                    catch (Exception)
                    {
                        Debug.LogWarning("AlembicRecorder: Failed to create directory " + dir);
                        return false;
                    }
                }
            }

            // create context and open archive
            m_ctx = aeContext.Create();
            if (m_ctx.self == IntPtr.Zero)
            {
                Debug.LogWarning("AlembicRecorder: failed to create context");
                return false;
            }

            m_ctx.SetConfig(ref m_settings.conf);
            if (!m_ctx.OpenArchive(m_settings.outputPath))
            {
                Debug.LogWarning("AlembicRecorder: failed to open file " + m_settings.outputPath);
                m_ctx.Destroy();
                return false;
            }

            // create capturers
            CreateCapturers();

            m_recording = true;
            m_time = m_settings.conf.startTime;
            m_frameCount = 0;

            Debug.Log("AlembicRecorder: start " + m_settings.outputPath);
            return true;
        }

        public void EndRecording()
        {
            if (!m_recording) { return; }

            m_capturers.Clear();
            m_ctx.Destroy(); // flush archive
            m_recording = false;

            Debug.Log("AlembicRecorder: end: " + m_settings.outputPath);
        }

        public void ProcessRecording()
        {
            if (!m_recording) { return; }

            float begin_time = Time.realtimeSinceStartup;

            m_ctx.MarkFrameBegin();
            m_ctx.AddTime(m_time);
            foreach (var recorder in m_capturers)
                recorder.Capture();
            m_ctx.MarkFrameEnd();

            m_time += Time.deltaTime;
            ++m_frameCount;

            m_elapsed = Time.realtimeSinceStartup - begin_time;
            if (m_settings.detailedLog)
            {
                Debug.Log("AlembicRecorder: frame " + m_frameCount + " (" + (m_elapsed * 1000.0f) + " ms)");
            }
        }
        #endregion
    }
}
