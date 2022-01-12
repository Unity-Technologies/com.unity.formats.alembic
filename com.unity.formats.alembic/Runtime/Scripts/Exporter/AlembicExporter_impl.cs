using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Util
{
    /// <summary>
    /// The scope of the Alembic export.
    /// </summary>
    public enum ExportScope
    {
        /// <summary>
        /// Export the entire Scene.
        /// </summary>
        EntireScene,
        /// <summary>
        /// Export only a branch (or hierarchy) of the Scene. Requires a TargetBranch value to be set.
        /// </summary>
        TargetBranch,
    }

    /// <summary>
    /// Settings controlling various aspects of the recording in Alembic exporter.
    /// </summary>
    [Serializable]
    public class AlembicRecorderSettings
    {
        [SerializeField]
        string outputPath = "Output/Output.abc";
        /// <summary>
        /// Get or set the location to save the exported Alembic file to.
        /// </summary>
        public string OutputPath
        {
            get { return outputPath; }
            set { outputPath = value; }
        }

        [SerializeField]
        AlembicExportOptions conf = new AlembicExportOptions();

        /// <summary>
        /// Alembic file options (archive type, transform format, etc.)
        /// </summary>
        public AlembicExportOptions ExportOptions => conf;

        [SerializeField]
        ExportScope scope = ExportScope.EntireScene;
        /// <summary>
        /// Get or set the scope of the export (entire Scene or selected branch).
        /// </summary>
        public ExportScope Scope
        {
            get { return scope; }
            set { scope = value; }
        }

        [SerializeField]
        GameObject targetBranch;
        /// <summary>
        /// Get or set the branch (or hierarchy) of the Scene that is exported. Use this option only if ExportScope is set to TargetBranch.
        /// </summary>
        public GameObject TargetBranch
        {
            get
            {
                return getTargetBranch != null ? getTargetBranch() : targetBranch;
            }
            set
            {
                if (setTargetBranch != null)
                {
                    setTargetBranch(value);
                }
                else
                {
                    targetBranch = value;
                }
            }
        }

        // Allow the GameObject target to be (de)serialized in a different way (the UnityRecorder saves asset and have a different mechanism to serialize scene references)
        internal SetTargetBranch setTargetBranch;
        internal GetTargetBranch getTargetBranch;

        internal delegate void SetTargetBranch(GameObject go);
        internal delegate GameObject GetTargetBranch();

        [SerializeField, HideInInspector]
        bool fixDeltaTime = true;
        /// <summary>
        /// Enable to set Time.maximumDeltaTime using the frame rate to ensure fixed delta time. Only available when TimeSamplingType is set to Uniform.
        /// </summary>
        public bool FixDeltaTime
        {
            get { return fixDeltaTime; }
            set { fixDeltaTime = value; }
        }

        [SerializeField]
        [Tooltip("Assume only GameObjects with a SkinnedMeshRenderer component change over time.")]
        bool assumeNonSkinnedMeshesAreConstant = true;
        /// <summary>
        /// Enable to skip capturing animation on static Meshes.
        /// </summary>
        public bool AssumeNonSkinnedMeshesAreConstant
        {
            get { return assumeNonSkinnedMeshesAreConstant; }
            set { assumeNonSkinnedMeshesAreConstant = value; }
        }

        [SerializeField]
        bool captureMeshRenderer = true;
        /// <summary>
        /// Enable to capture Mesh assets.
        /// </summary>
        public bool CaptureMeshRenderer
        {
            get { return captureMeshRenderer; }
            set { captureMeshRenderer = value; }
        }

        [SerializeField]
        bool captureSkinnedMeshRenderer = true;
        /// <summary>
        /// Enable to capture Skinned Mesh assets.
        /// </summary>
        public bool CaptureSkinnedMeshRenderer
        {
            get { return captureSkinnedMeshRenderer; }
            set { captureSkinnedMeshRenderer = value; }
        }

        [SerializeField]
        bool captureParticleSystem = false;
        internal bool CaptureParticleSystem // Need to confirm is working.
        {
            get { return captureParticleSystem; }
            set { captureParticleSystem = value; }
        }

        [SerializeField]
        bool captureCamera = true;
        /// <summary>
        /// Enable to capture Camera components.
        /// </summary>
        public bool CaptureCamera
        {
            get { return captureCamera; }
            set { captureCamera = value; }
        }

        [SerializeField]
        bool meshNormals = true;
        /// <summary>
        /// Enable to export Mesh normals.
        /// </summary>
        public bool MeshNormals
        {
            get { return meshNormals; }
            set { meshNormals = value; }
        }

        [SerializeField]
        bool meshUV0 = true;
        /// <summary>
        /// Enable to export the base texture coordinate set of the Mesh.
        /// </summary>
        public bool MeshUV0
        {
            get { return meshUV0; }
            set { meshUV0 = value; }
        }

        [SerializeField]
        bool meshUV1 = true;
        /// <summary>
        /// Enable to export the second texture coordinate set of the Mesh.
        /// </summary>
        public bool MeshUV1
        {
            get { return meshUV1; }
            set { meshUV1 = value; }
        }

        [SerializeField]
        bool meshColors = true;
        /// <summary>
        /// Enable to export Mesh vertex colors.
        /// </summary>
        public bool MeshColors
        {
            get { return meshColors; }
            set { meshColors = value; }
        }

        [SerializeField]
        bool meshSubmeshes = true;
        /// <summary>
        /// Enable to export sub-Meshes.
        /// </summary>
        public bool MeshSubmeshes
        {
            get { return meshSubmeshes; }
            set { meshSubmeshes = value; }
        }

        [SerializeField]
        bool detailedLog = false;
        /// <summary>
        /// Enable to provide Debug logging for each captured frame.
        /// </summary>
        public bool DetailedLog
        {
            get { return detailedLog; }
            set { detailedLog = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    sealed class CaptureTarget : Attribute
    {
        public Type componentType { get; set; }

        public CaptureTarget(Type t) { componentType = t; }
    }

    abstract class ComponentCapturer
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


    /// <summary>
    /// Class that implements the recording of Unity Scene elements in the Alembic format.
    /// </summary>
    [Serializable]
    public sealed class AlembicRecorder : IDisposable
    {
        #region internal types
        class MeshBuffer : IDisposable
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

            public void SetupSubmeshes(aeObject abc, Mesh mesh)
            {
                for (int smi = 0; smi < mesh.subMeshCount; ++smi)
                    abc.AddFaceSet(string.Format("submesh[{0}]", smi));
            }

            public void Capture(Mesh mesh, Matrix4x4 world2local,
                bool captureNormals, bool captureUV0, bool captureUV1, bool captureColors)
            {
                if (mesh == null)
                {
                    Clear();
                    return;
                }

                if (world2local != Matrix4x4.identity)
                {
                    var verts = new List<Vector3>();
                    mesh.GetVertices(verts);
                    for (var i = 0; i < verts.Count; ++i)
                    {
                        var v = verts[i];
                        verts[i] = world2local.MultiplyPoint(v);
                    }

                    points.Assign(verts);
                }
                else
                {
                    points.LockList(ls => mesh.GetVertices(ls));
                }


                if (captureNormals)
                {
                    if (world2local != Matrix4x4.identity)
                    {
                        var meshNormals = new List<Vector3>();
                        mesh.GetNormals(meshNormals);
                        for (var i = 0; i < meshNormals.Count; ++i)
                        {
                            var n = meshNormals[i];
                            meshNormals[i] = world2local.MultiplyVector(n);
                        }
                        normals.Assign(meshNormals);
                    }
                    else
                    {
                        normals.LockList(ls => mesh.GetNormals(ls));
                    }
                }
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

                    aeSubmeshData smd = new aeSubmeshData();
                    switch (mesh.GetTopology(smi))
                    {
                        case MeshTopology.Triangles: smd.topology = aeTopology.Triangles; break;
                        case MeshTopology.Lines: smd.topology = aeTopology.Lines; break;
                        case MeshTopology.Quads: smd.topology = aeTopology.Quads; break;
                        default: smd.topology = aeTopology.Points; break;
                    }
                    smd.indexes = indices;
                    smd.indexCount = indices.Count;
                    submeshData[smi] = smd;
                }
            }

            public void Capture(Mesh mesh, AlembicRecorderSettings settings)
            {
                Capture(mesh, Matrix4x4.identity, settings.MeshNormals, settings.MeshUV0, settings.MeshUV1, settings.MeshColors);
            }

            public void Capture(Mesh mesh, Matrix4x4 world2local, AlembicRecorderSettings settings)
            {
                Capture(mesh, world2local, settings.MeshNormals, settings.MeshUV0, settings.MeshUV1, settings.MeshColors);
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

            public void Dispose()
            {
                if (points != null) points.Dispose();
                if (normals != null) normals.Dispose();
                if (uv0 != null) uv0.Dispose();
                if (uv1 != null) uv1.Dispose();
                if (colors != null) colors.Dispose();
                if (submeshData != null) submeshData.Dispose();
                if (submeshIndices != null) submeshIndices.ForEach(i => { if (i != null) i.Dispose(); });
            }
        }

        class ClothBuffer : IDisposable
        {
            public PinnedList<int> remap = new PinnedList<int>();
            public PinnedList<Vector3> vertices = new PinnedList<Vector3>();
            public PinnedList<Vector3> normals = new PinnedList<Vector3>();
            public Transform rootBone;
            public int numRemappedVertices;

            void GenerateRemapIndices(Mesh mesh, MeshBuffer mbuf)
            {
                mbuf.Capture(mesh, Matrix4x4.identity, false, false, false, false);
                var weights4 = new PinnedList<BoneWeight>();
                weights4.LockList(l => { mesh.GetBoneWeights(l); });

                remap.Resize(mbuf.points.Count);
                numRemappedVertices = NativeMethods.aeGenerateRemapIndices(remap, mbuf.points, weights4, mbuf.points.Count);
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

                if (settings.MeshNormals)
                    normals.Assign(cloth.normals);
                else
                    normals.Clear();

                // apply root bone transform
                if (rootBone != null)
                {
                    var mat = Matrix4x4.TRS(rootBone.position, rootBone.rotation, Vector3.one);
                    // The Cloth disregards the world scale (Similar to the SkinnedMesh).
                    if (rootBone.parent != null)
                    {
                        mat = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, rootBone.parent.lossyScale).inverse *
                            mat;
                    }

                    NativeMethods.aeApplyMatrixP(vertices, vertices.Count, ref mat);
                    NativeMethods.aeApplyMatrixV(normals, normals.Count, ref mat);
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
                if (settings.MeshUV0)
                    mbuf.uv0.LockList(ls => mesh.GetUVs(0, ls));
                else
                    mbuf.uv0.Clear();

                if (settings.MeshUV1)
                    mbuf.uv1.LockList(ls => mesh.GetUVs(1, ls));
                else
                    mbuf.uv1.Clear();

                if (settings.MeshColors)
                    mbuf.colors.LockList(ls => mesh.GetColors(ls));
                else
                    mbuf.colors.Clear();
            }

            public void Dispose()
            {
                if (remap != null) remap.Dispose();
                if (vertices != null) vertices.Dispose();
                if (normals != null) normals.Dispose();
            }
        }

        class RootCapturer : ComponentCapturer
        {
            public RootCapturer(AlembicRecorder rec, aeObject abc)
            {
                recorder = rec;
                abcObject = abc;
            }

            public override void Setup(Component c) { }
            public override void Capture() { }
        }

        [CaptureTarget(typeof(Transform))]
        class TransformCapturer : ComponentCapturer
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
                if (parent == null || target == null)
                {
                    if (parent == null)
                        Debug.LogWarning("Parent was null");
                    else
                        Debug.LogWarning("Target was null");
                    m_target = null;
                    return;
                }
                abcObject = parent.abcObject.NewXform(target.name + " (" + target.GetInstanceID().ToString("X8") + ")", timeSamplingIndex);
                m_target = target;
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

            void Capture(ref aeXformData dst)
            {
                var src = m_target;

                dst.visibility = src.gameObject.activeSelf;
                dst.inherits = m_inherits;
                if (m_invertForward)
                {
                    src.rotation = Quaternion.LookRotation(-1 * src.forward, src.up);  // rotate around Y 180deg: z => -z
                }
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

                if (m_invertForward)
                {
                    src.rotation = Quaternion.LookRotation(-1 * src.forward, src.up);
                }
            }
        }

        [CaptureTarget(typeof(Camera))]
        class CameraCapturer : ComponentCapturer
        {
            Camera m_target;
            CameraData m_data;

            public override void Setup(Component c)
            {
                var target = c as Camera;
                abcObject = parent.abcObject.NewCamera(target.name, timeSamplingIndex);
                m_target = target;

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

            void Capture(ref CameraData dst)
            {
                var src = m_target;
                dst.visibility = src.gameObject.activeSelf;
                dst.nearClipPlane = src.nearClipPlane;
                dst.farClipPlane = src.farClipPlane;

                if (src.usePhysicalProperties)
                {
                    dst.focalLength = src.focalLength;
                    dst.lensShift = src.lensShift;
                    dst.sensorSize = src.sensorSize;
                }
                else
                {
                    const float deg2rad = Mathf.PI / 180;
                    dst.focalLength = (float)(Screen.height / 2 / Math.Tan(deg2rad * src.fieldOfView / 2));
                    dst.sensorSize = new Vector2(Screen.width, Screen.height);
                }
            }
        }

        [CaptureTarget(typeof(MeshRenderer))]
        class MeshCapturer : ComponentCapturer, IDisposable
        {
            MeshRenderer m_target;
            MeshBuffer m_mbuf = new MeshBuffer();

            public override void Setup(Component c)
            {
                m_target = c as MeshRenderer;
                MeshFilter meshFilter = m_target.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    m_target = null;
                    return;
                }

                Mesh mesh = meshFilter.mesh;
                if (mesh == null)
                    return;

                abcObject = parent.abcObject.NewPolyMesh(m_target.name, timeSamplingIndex);
                if (recorder.Settings.MeshSubmeshes)
                    m_mbuf.SetupSubmeshes(abcObject, mesh);
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
                    if (!recorder.m_settings.AssumeNonSkinnedMeshesAreConstant || m_mbuf.points.Capacity == 0)
                        m_mbuf.Capture(mesh, recorder.m_settings);
                }
                m_mbuf.WriteSample(abcObject);
            }

            public void Dispose()
            {
                if (m_mbuf != null) m_mbuf.Dispose();
            }
        }

        [CaptureTarget(typeof(SkinnedMeshRenderer))]
        class SkinnedMeshCapturer : ComponentCapturer, IDisposable
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
                if (recorder.Settings.MeshSubmeshes)
                    m_mbuf.SetupSubmeshes(abcObject, mesh);

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
                        {
                            m_meshBake = new Mesh { name = m_target.name };
                        }

                        m_meshBake.Clear();
#if UNITY_2020_2_OR_NEWER
                        m_target.BakeMesh(m_meshBake, true);
                        m_mbuf.Capture(m_meshBake, Matrix4x4.identity, recorder.m_settings);
#else
                        m_target.BakeMesh(m_meshBake);

                        var withScale = m_target.transform.worldToLocalMatrix;
                        var noScale = m_target.transform.WorldNoScale();
                        // The Skinned mesh baker disregards the world scale.
                        // This matrix transform inverts the wrong Unity transforms are reapplies the full world scale.
                        m_mbuf.Capture(m_meshBake, withScale * noScale.inverse, recorder.m_settings);
#endif
                    }
                }
                m_mbuf.WriteSample(abcObject);
            }

            public void Dispose()
            {
                if (m_mbuf != null) m_mbuf.Dispose();
                if (m_cbuf != null) m_cbuf.Dispose();
            }
        }

        [CaptureTarget(typeof(ParticleSystem))]
        class ParticleCapturer : ComponentCapturer, IDisposable
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

            public void Dispose()
            {
                if (m_bufPoints != null) m_bufPoints.Dispose();
                if (m_bufRotations != null) m_bufRotations.Dispose();
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
        /// <summary>
        /// Get or set the Recorder settings.
        /// </summary>
        public AlembicRecorderSettings Settings
        {
            get { return m_settings; }
            set { m_settings = value; }
        }

        /// <summary>
        /// Get or set the recording target branch. Ignored if Scope is set to EntireScene.
        /// </summary>
        public GameObject TargetBranch
        {
            get { return m_settings.TargetBranch; }
            set { m_settings.TargetBranch = value; }
        }

        /// <summary>
        /// Get the recording status.
        /// </summary>
        /// <returns>True if a recording session is active.</returns>
        public bool Recording { get { return m_recording; } }

        /// <summary>
        /// Get or set the frame number the capture stops at.
        /// </summary>
        public int FrameCount
        {
            get { return m_frameCount; }
            set { m_frameCount = value; }
        }
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
// Deprecated in 2019.2
#if !UNITY_2019_2_OR_NEWER
                method.Invoke(null, new object[] { BuildTarget.StandaloneLinux, 0, 0 });
#endif
                method.Invoke(null, new object[] { BuildTarget.StandaloneLinux64, 0, 0 });
            }
        }

#endif


        Component[] GetTargets(Type type)
        {
            if (m_settings.Scope == ExportScope.TargetBranch && TargetBranch != null)
                return TargetBranch.GetComponentsInChildren(type);
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

            if (!m_settings.CaptureCamera)
                m_capturerTable[typeof(Camera)].enabled = false;
            if (!m_settings.CaptureMeshRenderer)
                m_capturerTable[typeof(MeshRenderer)].enabled = false;
            if (!m_settings.CaptureSkinnedMeshRenderer)
                m_capturerTable[typeof(SkinnedMeshRenderer)].enabled = false;
            if (!m_settings.CaptureParticleSystem)
                m_capturerTable[typeof(ParticleSystem)].enabled = false;
        }

        void SetupComponentCapturer(CaptureNode node)
        {
            if (node.setup)
                return;

            int timeSamplingIndex = GetCurrentTimeSamplingIndex();
            var parent = node.parent;
            if (parent != null && parent.transformCapturer == null)
            {
                SetupComponentCapturer(parent);
                if (!m_nodes.ContainsKey(parent.instanceID) || !m_newNodes.Contains(parent))
                {
                    m_nodes.Add(parent.instanceID, parent);
                    m_newNodes.Add(parent);
                }
            }

            node.transformCapturer = new TransformCapturer();
            node.transformCapturer.recorder = this;
            node.transformCapturer.parent = parent == null ? m_root : parent.transformCapturer;
            node.transformCapturer.timeSamplingIndex = timeSamplingIndex;
            node.transformCapturer.inherits = true;
            node.transformCapturer.Setup(node.transform);

            if (node.componentType != null && node.componentType != typeof(Transform)) // previous chunk already sets up transforms
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
        /// <summary>
        /// Deallocate the native resources.
        /// </summary>
        public void Dispose()
        {
            m_ctx.Destroy();
        }

        /// <summary>
        /// Starts a recording session.
        /// </summary>
        /// <returns>True if succeeded, false otherwise.</returns>
        public bool BeginRecording()
        {
            if (m_recording)
            {
                Debug.LogWarning("AlembicRecorder: already recording");
                return false;
            }
            if (m_settings.Scope == ExportScope.TargetBranch && TargetBranch == null)
            {
                Debug.LogWarning("AlembicRecorder: target object is not set");
                return false;
            }


            {
                var dir = Path.GetDirectoryName(m_settings.OutputPath);
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

            m_ctx.SetConfig(m_settings.ExportOptions);
            if (!m_ctx.OpenArchive(m_settings.OutputPath))
            {
                Debug.LogWarning("AlembicRecorder: failed to open file " + m_settings.OutputPath);
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

            if (m_settings.ExportOptions.TimeSamplingType == TimeSamplingType.Uniform && m_settings.FixDeltaTime)
                Time.maximumDeltaTime = (1.0f / m_settings.ExportOptions.FrameRate);

            Debug.Log("AlembicRecorder: start " + m_settings.OutputPath);
            return true;
        }

        /// <summary>
        /// Ends the recording session.
        /// </summary>
        public void EndRecording()
        {
            if (!m_recording) { return; }

            m_iidToRemove = null;
            m_newNodes = null;
            m_nodes = null;
            m_root = null;
            m_ctx.Destroy(); // flush archive
            m_recording = false;

            Debug.Log("AlembicRecorder: end: " + m_settings.OutputPath);
        }

        /// <summary>
        /// Writes the current frame to the Alembic archive. Recording should have been previously started.
        /// </summary>

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
            switch (m_settings.ExportOptions.TimeSamplingType)
            {
                case TimeSamplingType.Uniform:
                    m_time = (1.0f / m_settings.ExportOptions.FrameRate) * m_frameCount;
                    break;
                case TimeSamplingType.Acyclic:
                    m_time += Time.deltaTime;
                    break;
            }
            m_elapsed = Time.realtimeSinceStartup - begin_time;

            // wait maximumDeltaTime if timeSamplingType is uniform
            if (m_settings.ExportOptions.TimeSamplingType == TimeSamplingType.Uniform && m_settings.FixDeltaTime)
                AbcAPI.aeWaitMaxDeltaTime();

            if (m_settings.DetailedLog)
            {
                Debug.Log("AlembicRecorder: frame " + m_frameCount + " (" + (m_elapsed * 1000.0f) + " ms)");
            }
        }

        #endregion
    }
}
