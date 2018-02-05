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
    public partial class AlembicExporter : MonoBehaviour
    {
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

            public void Capture(Mesh mesh, AlembicExporter exp)
            {
                Capture(mesh, exp.m_meshNormals, exp.m_meshUV0, exp.m_meshUV1, exp.m_meshColors);
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

            public void Capture(Mesh mesh, Cloth cloth, MeshBuffer mbuf, AlembicExporter exp)
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

                if (exp.m_meshNormals)
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
                if (exp.m_meshUV0)
                    mbuf.uv0.LockList(ls => mesh.GetUVs(0, ls));
                else
                    mbuf.uv0.Clear();

                if (exp.m_meshUV1)
                    mbuf.uv1.LockList(ls => mesh.GetUVs(1, ls));
                else
                    mbuf.uv1.Clear();

                if (exp.m_meshColors)
                    mbuf.colors.LockList(ls => mesh.GetColors(ls));
                else
                    mbuf.colors.Clear();

            }
        }


        public abstract class ComponentCapturer
        {
            protected AlembicExporter m_exporter;
            protected ComponentCapturer m_parent;
            protected GameObject m_obj;
            protected aeObject m_abc;

            public ComponentCapturer parent { get { return m_parent; } }
            public GameObject obj { get { return m_obj; } }
            public aeObject abc { get { return m_abc; } }
            public abstract void Capture();

            protected ComponentCapturer(AlembicExporter exp, ComponentCapturer p, Component c)
            {
                m_exporter = exp;
                m_parent = p;
                m_obj = c != null ? c.gameObject : null;
            }
        }

        public class RootCapturer : ComponentCapturer
        {
            public RootCapturer(AlembicExporter exp, aeObject abc)
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

            public TransformCapturer(AlembicExporter exp, ComponentCapturer parent, Transform target)
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

            public CameraCapturer(AlembicExporter exp, ComponentCapturer parent, Camera target)
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

            public MeshCapturer(AlembicExporter exp, ComponentCapturer parent, MeshRenderer target)
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
                if (mesh == null || (m_exporter.m_assumeNonSkinnedMeshesAreConstant && m_mbuf.points.Capacity != 0))
                    return;

                m_mbuf.Capture(mesh, m_exporter);
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

            public SkinnedMeshCapturer(AlembicExporter exp, ComponentCapturer parent, SkinnedMeshRenderer target)
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
                    m_cbuf.Capture(m_meshSrc, m_cloth, m_mbuf, m_exporter);
                    m_mbuf.WriteSample(m_abc);
                }
                else
                {
                    if (m_meshBake == null)
                        m_meshBake = new Mesh();
                    m_meshBake.Clear();
                    m_target.BakeMesh(m_meshBake);

                    m_mbuf.Capture(m_meshBake, m_exporter);
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

            public ParticleCapturer(AlembicExporter exp, ComponentCapturer parent, ParticleSystem target)
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

            public CustomCapturerHandler(AlembicExporter exp, ComponentCapturer parent, AlembicCustomComponentCapturer target)
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


#if UNITY_EDITOR
        void ForceDisableBatching()
        {
            var method = typeof(UnityEditor.PlayerSettings).GetMethod("SetBatchingForPlatform", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { BuildTarget.StandaloneWindows, 0, 0 });
            method.Invoke(null, new object[] { BuildTarget.StandaloneWindows64, 0, 0 });
        }
#endif

        public enum Scope
        {
            EntireScene,
            CurrentBranch,
        }
    }
}
