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
        TargetBranch,
    }


    [Serializable]
    public class AlembicRecorderSettings
    {
        public string outputPath  = "Output/Output.abc";
        public aeConfig conf = aeConfig.defaultValue;
        public ExportScope scope = ExportScope.EntireScene;
        public GameObject targetBranch;
        public bool fixDeltaTime = true;

        public bool assumeNonSkinnedMeshesAreConstant = true;

        public bool captureMeshRenderer = true;
        public bool captureSkinnedMeshRenderer = true;
        public bool captureParticleSystem = true;
        public bool captureCamera = true;

        public bool meshNormals = true;
        public bool meshUV0 = true;
        public bool meshUV1 = true;
        public bool meshColors = true;
        public bool meshSubmeshes = true;

        public bool detailedLog = true;
        public bool debugLog = false;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CaptureTarget : Attribute
    {
        public Type componentType;

        public CaptureTarget(Type t) { componentType = t; }
    }

    public abstract class ComponentCapturer
    {
        public AlembicRecorder recorder;
        public ComponentCapturer parent;
        public aeObject abcObject;
        public int timeSamplingIndex;

        public abstract void Setup(Component c);
        public abstract void Capture();

        public void MarkForceInvisible()
        {
            abcObject.MarkForceInvisible();
        }
    }


    [Serializable]
    public class AlembicRecorder : IDisposable
    {
        #region internal types
        public class MeshBuffer
        {
            public bool visibility = true;
            public PinnedList<Vector3> points = new PinnedList<Vector3>();
            public PinnedList<Vector3> normals = new PinnedList<Vector3>();
            public PinnedList<Vector2> uv0 = new PinnedList<Vector2>();
            public PinnedList<Vector2> uv1 = new PinnedList<Vector2>();
            public PinnedList<Color> colors = new PinnedList<Color>();

            public PinnedList<aeSubmeshData> submeshData = new PinnedList<aeSubmeshData>();
            public List<PinnedList<int>> submeshIndices = new List<PinnedList<int>>();

            public void Clear()
            {
                points.Clear();
                normals.Clear();
                uv0.Clear();
                uv1.Clear();
                colors.Clear();

                submeshData.Clear();
                submeshIndices.Clear();
            }

            public void SetupSubmeshes(aeObject abc, Mesh mesh, Material[] materials)
            {
                for (int smi = 0; smi < mesh.subMeshCount; ++smi)
                    abc.AddFaceSet(string.Format("submesh[{0}]", smi));
            }

            public void Capture(Mesh mesh,
                bool captureNormals, bool captureUV0, bool captureUV1, bool captureColors)
            {
                if (mesh == null)
                {
                    Clear();
                    return;
                }

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


                int submeshCount = mesh.subMeshCount;
                submeshData.Resize(submeshCount);
                if (submeshIndices.Count > submeshCount)
                    submeshIndices.RemoveRange(submeshCount, submeshIndices.Count - submeshCount);
                while (submeshIndices.Count < submeshCount)
                    submeshIndices.Add(new PinnedList<int>());
                for (int smi = 0; smi < submeshCount; ++smi)
                {
                    var indices = submeshIndices[smi];
                    indices.LockList(l => { mesh.GetIndices(l, smi); });

                    aeSubmeshData smd;
                    switch (mesh.GetTopology(smi)) {
                        case MeshTopology.Triangles: smd.topology = aeTopology.Triangles; break;
                        case MeshTopology.Lines: smd.topology = aeTopology.Lines; break;
                        case MeshTopology.Quads: smd.topology = aeTopology.Quads; break;
                        default: smd.topology = aeTopology.Points; break;
                    }
                    smd.indices = indices;
                    smd.indexCount = indices.Count;
                    submeshData[smi] = smd;
                }
            }

            public void Capture(Mesh mesh, AlembicRecorderSettings settings)
            {
                Capture(mesh, settings.meshNormals, settings.meshUV0, settings.meshUV1, settings.meshColors);
            }


            public void WriteSample(aeObject abc)
            {
                var data = default(aePolyMeshData);
                data.visibility = visibility;
                data.points = points;
                data.pointCount = points.Count;
                data.normals = normals;
                data.uv0 = uv0;
                data.uv1 = uv1;
                data.colors = colors;
                data.submeshes = submeshData;
                data.submeshCount = submeshData.Count;
                abc.WriteSample(ref data);
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
                {
                    mbuf.Clear();
                    return;
                }

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

        public class RootCapturer : ComponentCapturer
        {
            public RootCapturer(AlembicRecorder rec, aeObject abc)
            {
                recorder = rec;
                abcObject = abc;
            }

            public override void Setup(Component c) { }
            public override void Capture() { }
        }

        public class TransformCapturer : ComponentCapturer
        {
            Transform m_target;
            bool m_inherits = false;
            bool m_invertForward = false;
            bool m_capturePosition = true;
            bool m_captureRotation = true;
            bool m_captureScale = true;
            aeXformData m_data;

            public bool inherits { set { m_inherits = value; } }
            public bool invertForward { set { m_invertForward = value; } }
            public bool capturePosition { set { m_capturePosition = value; } }
            public bool captureRotation { set { m_captureRotation = value; } }
            public bool captureScale { set { m_captureScale = value; } }

            public override void Setup(Component c)
            {
                var target = c as Transform;
                abcObject = parent.abcObject.NewXform(target.name + " (" + target.GetInstanceID().ToString("X8") + ")", timeSamplingIndex);
                m_target = target;
            }

            public override void Capture()
            {
                if (m_target == null) {
                    m_data.visibility = false;
                }
                else
                {
                    Capture(ref m_data);
                }
                abcObject.WriteSample(ref m_data);
            }

            void Capture(ref aeXformData dst)
            {
                var src = m_target;

                dst.visibility = src.gameObject.activeSelf;
                dst.inherits = m_inherits;
                if (m_invertForward) { src.forward = src.forward * -1.0f; }
                if (m_inherits)
                {
                    dst.translation = m_capturePosition ? src.localPosition : Vector3.zero;
                    dst.rotation = m_captureRotation ? src.localRotation : Quaternion.identity;
                    dst.scale = m_captureScale ? src.localScale : Vector3.one;
                }
                else
                {
                    dst.translation = m_capturePosition ? src.position : Vector3.zero;
                    dst.rotation = m_captureRotation ? src.rotation : Quaternion.identity;
                    dst.scale = m_captureScale ? src.lossyScale : Vector3.one;
                }
                if (m_invertForward) { src.forward = src.forward * -1.0f; }
            }
        }

        [CaptureTarget(typeof(Camera))]
        public class CameraCapturer : ComponentCapturer
        {
            Camera m_target;
            AlembicCameraParams m_params;
            aeCameraData m_data = aeCameraData.defaultValue;

            public override void Setup(Component c)
            {
                var target = c as Camera;
                abcObject = parent.abcObject.NewCamera(target.name, timeSamplingIndex);
                m_target = target;
                m_params = target.GetComponent<AlembicCameraParams>();

                var trans = parent as TransformCapturer;
                if (trans != null)
                    trans.invertForward = true;
            }

            public override void Capture()
            {
                if (m_target == null)
                {
                    m_data.visibility = false;
                }
                else
                {
                    Capture(ref m_data);
                }
                abcObject.WriteSample(ref m_data);
            }

            void Capture(ref aeCameraData dst)
            {
                var src = m_target;
                dst.visibility = src.gameObject.activeSelf;
                dst.nearClippingPlane = src.nearClipPlane;
                dst.farClippingPlane = src.farClipPlane;
                dst.fieldOfView = src.fieldOfView;
                if (m_params != null)
                {
                    dst.focalLength = m_params.m_focalLength;
                    dst.focusDistance = m_params.m_focusDistance;
                    dst.aperture = m_params.m_aperture;
                    dst.aspectRatio = m_params.GetAspectRatio();
                }
            }
        }

        [CaptureTarget(typeof(MeshRenderer))]
        public class MeshCapturer : ComponentCapturer
        {
            MeshRenderer m_target;
            MeshBuffer m_mbuf = new MeshBuffer();

            public override void Setup(Component c)
            {
                m_target = c as MeshRenderer;
                var mesh = m_target.GetComponent<MeshFilter>().sharedMesh;
                if (mesh == null)
                    return;

                abcObject = parent.abcObject.NewPolyMesh(m_target.name, timeSamplingIndex);
                if (recorder.settings.meshSubmeshes)
                    m_mbuf.SetupSubmeshes(abcObject, mesh, m_target.sharedMaterials);
            }

            public override void Capture()
            {
                if (m_target == null)
                {
                    m_mbuf.visibility = false;
                }
                else
                {
                    m_mbuf.visibility = m_target.gameObject.activeSelf;
                    var mesh = m_target.GetComponent<MeshFilter>().sharedMesh;
                    if (!recorder.m_settings.assumeNonSkinnedMeshesAreConstant || m_mbuf.points.Capacity == 0)
                        m_mbuf.Capture(mesh, recorder.m_settings);
                }
                m_mbuf.WriteSample(abcObject);
            }
        }

        [CaptureTarget(typeof(SkinnedMeshRenderer))]
        public class SkinnedMeshCapturer : ComponentCapturer
        {
            SkinnedMeshRenderer m_target;
            Mesh m_meshSrc;
            Mesh m_meshBake;
            Cloth m_cloth;
            MeshBuffer m_mbuf = new MeshBuffer();
            ClothBuffer m_cbuf;

            public override void Setup(Component c)
            {
                var target = c as SkinnedMeshRenderer;
                m_target = target;
                var mesh = target.sharedMesh;
                if (mesh == null)
                    return;

                abcObject = parent.abcObject.NewPolyMesh(target.name, timeSamplingIndex);
                if (recorder.settings.meshSubmeshes)
                    m_mbuf.SetupSubmeshes(abcObject, mesh, m_target.sharedMaterials);

                m_meshSrc = target.sharedMesh;
                m_cloth = m_target.GetComponent<Cloth>();
                if (m_cloth != null)
                {
                    m_cbuf = new ClothBuffer();
                    m_cbuf.rootBone = m_target.rootBone != null ? m_target.rootBone : m_target.GetComponent<Transform>();

                    var tc = parent as TransformCapturer;
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
                if (m_target == null)
                {
                    m_mbuf.visibility = false;
                }
                else
                {
                    m_mbuf.visibility = m_target.gameObject.activeSelf;
                    if (m_cloth != null)
                    {
                        m_cbuf.Capture(m_meshSrc, m_cloth, m_mbuf, recorder.m_settings);
                    }
                    else
                    {
                        if (m_meshBake == null)
                            m_meshBake = new Mesh();

                        m_meshBake.Clear();
                        m_target.BakeMesh(m_meshBake);
                        m_mbuf.Capture(m_meshBake, recorder.m_settings);
                    }
                }
                m_mbuf.WriteSample(abcObject);
            }
        }

        [CaptureTarget(typeof(ParticleSystem))]
        public class ParticleCapturer : ComponentCapturer
        {
            ParticleSystem m_target;
            ParticleSystem.Particle[] m_bufParticles;
            PinnedList<Vector3> m_bufPoints = new PinnedList<Vector3>();
            PinnedList<Quaternion> m_bufRotations = new PinnedList<Quaternion>();
            aePointsData m_data;

            public override void Setup(Component c)
            {
                m_target = c as ParticleSystem;
                abcObject = parent.abcObject.NewPoints(m_target.name, timeSamplingIndex);
            }

            public override void Capture()
            {
                if (m_target == null)
                {
                    m_data.visibility = false;
                }
                else
                {
                    // create buffer
                    int count_max = m_target.main.maxParticles;
                    if (m_bufParticles == null || m_bufParticles.Length != count_max)
                    {
                        m_bufParticles = new ParticleSystem.Particle[count_max];
                        m_bufPoints.Resize(count_max);
                        m_bufRotations.Resize(count_max);
                    }

                    // copy particle positions & rotations to buffer
                    int count = m_target.GetParticles(m_bufParticles);
                    for (int i = 0; i < count; ++i)
                    {
                        m_bufPoints[i] = m_bufParticles[i].position;
                        m_bufRotations[i] = Quaternion.AngleAxis(m_bufParticles[i].rotation, m_bufParticles[i].axisOfRotation);
                    }

                    // write!
                    m_data.visibility = m_target.gameObject.activeSelf;
                    m_data.positions = m_bufPoints;
                    m_data.count = count;
                }
                abcObject.WriteSample(ref m_data);
            }
        }


        class CaptureNode
        {
            public int instanceID;
            public Type componentType;
            public CaptureNode parent;
            public Transform transform;
            public TransformCapturer transformCapturer;
            public ComponentCapturer componentCapturer;
            public bool setup = false;

            public void MarkForceInvisible()
            {
                if (transformCapturer != null)
                    transformCapturer.MarkForceInvisible();
                if (componentCapturer != null)
                    componentCapturer.MarkForceInvisible();
            }

            public void Capture()
            {
                if (transformCapturer != null)
                    transformCapturer.Capture();
                if (componentCapturer != null)
                    componentCapturer.Capture();
            }
        }

        class CapturerRecord
        {
            public bool enabled = true;
            public Type type;
        }
        #endregion


        #region fields
        [SerializeField] AlembicRecorderSettings m_settings = new AlembicRecorderSettings();

        aeContext m_ctx;
        ComponentCapturer m_root;
        Dictionary<int, CaptureNode> m_nodes;
        List<CaptureNode> m_newNodes;
        List<int> m_iidToRemove;
        int m_lastTimeSamplingIndex;
        int m_startFrameOfLastTimeSampling;

        bool m_recording;
        float m_time, m_timePrev;
        float m_elapsed;
        int m_frameCount;

        Dictionary<Type, CapturerRecord> m_capturerTable = new Dictionary<Type, CapturerRecord>();
        #endregion


        #region properties
        public AlembicRecorderSettings settings
        {
            get { return m_settings; }
            set { m_settings = value; }
        }
        public GameObject targetBranch { get { return m_settings.targetBranch; } set { m_settings.targetBranch = value; } }
        public bool recording { get { return m_recording; } }
        public int frameCount { get { return m_frameCount; } }
        #endregion


        #region impl
#if UNITY_EDITOR
        public static void ForceDisableBatching()
        {
            var method = typeof(UnityEditor.PlayerSettings).GetMethod("SetBatchingForPlatform", BindingFlags.NonPublic | BindingFlags.Static);
            if (method != null)
            {
                method.Invoke(null, new object[] { BuildTarget.StandaloneWindows, 0, 0 });
                method.Invoke(null, new object[] { BuildTarget.StandaloneWindows64, 0, 0 });
#if UNITY_2017_3_OR_NEWER
                method.Invoke(null, new object[] { BuildTarget.StandaloneOSX, 0, 0 });
#else
                method.Invoke(null, new object[] { BuildTarget.StandaloneOSXUniversal, 0, 0 });
#endif
                method.Invoke(null, new object[] { BuildTarget.StandaloneLinux, 0, 0 });
                method.Invoke(null, new object[] { BuildTarget.StandaloneLinux64, 0, 0 });
            }
        }
#endif

                T[] GetTargets<T>() where T : Component
        {
            if (m_settings.scope == ExportScope.TargetBranch && targetBranch != null)
            {
                return targetBranch.GetComponentsInChildren<T>();
            }
            else
            {
                return GameObject.FindObjectsOfType<T>();
            }
        }

        Component[] GetTargets(Type type)
        {
            if (m_settings.scope == ExportScope.TargetBranch && targetBranch != null)
                return targetBranch.GetComponentsInChildren(type);
            else
                return Array.ConvertAll<UnityEngine.Object, Component>(GameObject.FindObjectsOfType(type), e => (Component)e);
        }

        int GetCurrentTimeSamplingIndex()
        {
            if (m_frameCount != m_startFrameOfLastTimeSampling)
            {
                m_startFrameOfLastTimeSampling = m_frameCount;
                m_lastTimeSamplingIndex = m_ctx.AddTimeSampling(m_timePrev);
            }
            return m_lastTimeSamplingIndex;
        }

        CaptureNode ConstructTree(Transform node)
        {
            if (node == null) { return null; }

            int iid = node.gameObject.GetInstanceID();
            CaptureNode cn;
            if (m_nodes.TryGetValue(iid, out cn)) { return cn; }

            cn = new CaptureNode();
            cn.instanceID = iid;
            cn.transform = node;
            cn.parent = ConstructTree(node.parent);
            m_nodes.Add(iid, cn);
            m_newNodes.Add(cn);
            return cn;
        }


        void SetupCapturerTable()
        {
            if (m_capturerTable.Count != 0)
                return;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    var attr = type.GetCustomAttributes(typeof(CaptureTarget), true);
                    if (attr.Length > 0)
                    {
                        m_capturerTable[(attr[0] as CaptureTarget).componentType] = new CapturerRecord { type = type };
                    }
                }
            }

            if (!m_settings.captureCamera)
                m_capturerTable[typeof(Camera)].enabled = false;
            if (!m_settings.captureMeshRenderer)
                m_capturerTable[typeof(MeshRenderer)].enabled = false;
            if (!m_settings.captureSkinnedMeshRenderer)
                m_capturerTable[typeof(SkinnedMeshRenderer)].enabled = false;
            if (!m_settings.captureParticleSystem)
                m_capturerTable[typeof(ParticleSystem)].enabled = false;
        }


        void SetupComponentCapturer(CaptureNode node)
        {
            if (node.setup)
                return;

            int timeSamplingIndex = GetCurrentTimeSamplingIndex();
            var parent = node.parent;
            node.transformCapturer = new TransformCapturer();
            node.transformCapturer.recorder = this;
            node.transformCapturer.parent = parent == null ? m_root : parent.transformCapturer;
            node.transformCapturer.timeSamplingIndex = timeSamplingIndex;
            node.transformCapturer.inherits = true;
            node.transformCapturer.Setup(node.transform);

            if (node.componentType != null)
            {
                var component = node.transform.GetComponent(node.componentType);
                if (component != null)
                {
                    var cr = m_capturerTable[node.componentType];
                    node.componentCapturer = Activator.CreateInstance(cr.type) as ComponentCapturer;
                    node.componentCapturer.recorder = this;
                    node.componentCapturer.parent = node.transformCapturer; ;
                    node.componentCapturer.timeSamplingIndex = timeSamplingIndex;
                    node.componentCapturer.Setup(component);
                }
            }

            node.setup = true;
        }

        void UpdateCaptureNodes()
        {
            // construct tree
            // (bottom-up)
            foreach (var kvp in m_capturerTable)
            {
                if (!kvp.Value.enabled)
                    continue;

                foreach (var t in GetTargets(kvp.Key))
                {
                    var node = ConstructTree(t.GetComponent<Transform>());
                    node.componentType = t.GetType();
                }
            }

            // make component capturers
            foreach (var c in m_nodes)
                SetupComponentCapturer(c.Value);
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
                Debug.LogWarning("AlembicRecorder: already recording");
                return false;
            }
            if (m_settings.scope == ExportScope.TargetBranch && targetBranch == null)
            {
                Debug.LogWarning("AlembicRecorder: target object is not set");
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

            m_root = new RootCapturer(this, m_ctx.topObject);
            m_nodes = new Dictionary<int, CaptureNode>();
            m_newNodes = new List<CaptureNode>();
            m_iidToRemove = new List<int>();
            m_lastTimeSamplingIndex = 1;
            m_startFrameOfLastTimeSampling = 0;

            // create capturers
            SetupCapturerTable();

            m_recording = true;
            m_time = m_timePrev = 0.0f;
            m_frameCount = 0;

            if (m_settings.conf.timeSamplingType == aeTimeSamplingType.Uniform && m_settings.fixDeltaTime)
                Time.maximumDeltaTime = (1.0f / m_settings.conf.frameRate);

            Debug.Log("AlembicRecorder: start " + m_settings.outputPath);
            return true;
        }

        public void EndRecording()
        {
            if (!m_recording) { return; }

            m_iidToRemove = null;
            m_newNodes = null;
            m_nodes = null;
            m_root = null;
            m_ctx.Destroy(); // flush archive
            m_recording = false;

            Debug.Log("AlembicRecorder: end: " + m_settings.outputPath);
        }

        public void ProcessRecording()
        {
            if (!m_recording) { return; }

            float begin_time = Time.realtimeSinceStartup;

            // check if there are new GameObjects to capture
            UpdateCaptureNodes();
            if (m_frameCount > 0 && m_newNodes.Count > 0)
            {
                // add invisible sample
                m_ctx.MarkFrameBegin();
                foreach (var node in m_newNodes)
                {
                    node.MarkForceInvisible();
                    node.Capture();
                }
                m_ctx.MarkFrameEnd();
            }
            m_newNodes.Clear();

            // do capture
            m_ctx.MarkFrameBegin();
            m_ctx.AddTime(m_time);
            foreach (var kvp in m_nodes)
            {
                var node = kvp.Value;
                node.Capture();
                if (node.transform == null)
                    m_iidToRemove.Add(node.instanceID);
            }
            m_ctx.MarkFrameEnd();

            // remove deleted GameObjects
            foreach (int iid in m_iidToRemove)
                m_nodes.Remove(iid);
            m_iidToRemove.Clear();

            // advance time
            ++m_frameCount;
            m_timePrev = m_time;
            switch(m_settings.conf.timeSamplingType)
            {
                case aeTimeSamplingType.Uniform:
                    m_time = (1.0f / m_settings.conf.frameRate) * m_frameCount;
                    break;
                case aeTimeSamplingType.Acyclic:
                    m_time += Time.deltaTime;
                    break;
            }
            m_elapsed = Time.realtimeSinceStartup - begin_time;

            // wait maximumDeltaTime if timeSamplingType is uniform
            if (m_settings.conf.timeSamplingType == aeTimeSamplingType.Uniform && m_settings.fixDeltaTime)
                AbcAPI.aeWaitMaxDeltaTime();

            if (m_settings.detailedLog)
            {
                Debug.Log("AlembicRecorder: frame " + m_frameCount + " (" + (m_elapsed * 1000.0f) + " ms)");
            }
        }
        #endregion
    }
}
