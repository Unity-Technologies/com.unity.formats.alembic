using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    public class AlembicPointsRenderer : MonoBehaviour
    {
        public enum InstancingMode
        {
            NoInstancing,
#if UNITY_5_5_OR_NEWER
            Instancing,
#endif
#if UNITY_5_6_OR_NEWER
            Procedural,
#endif
        }

        public const int MaxInstancesParDraw = 1023;

        public Mesh m_mesh;
        public Material m_material;
        public ShadowCastingMode m_shadow = ShadowCastingMode.Off;
        public bool m_receiveShadows = false;
        public LayerSelector m_layer = 0;
        public float m_pointSize = 0.1f;
        public InstancingMode m_instancingMode =
#if UNITY_5_5_OR_NEWER
            InstancingMode.Instancing;
#else
            InstancingMode.NoInstancing;
#endif

        Matrix4x4[] m_matrices;
        float[] m_ids;
        List<MaterialPropertyBlock> m_mpbs;
#if UNITY_5_6_OR_NEWER
        ComputeBuffer m_cbPoints;
        ComputeBuffer m_cbIDs;
        ComputeBuffer m_cbArgs;
        const string m_kwAlembicProceduralInstancing = "ALEMBIC_PROCEDURAL_INSTANCING_ENABLED";
        private int[] m_args = new int[5] { 0, 0, 0, 0, 0 };
#endif
#if UNITY_EDITOR
        bool m_dirty = false;
#endif

        public void Flush()
        {
            if(m_mesh == null || m_material == null) { return; }

            var apc = GetComponent<AlembicPointsCloud>();
            var points = apc.abcPositions;
            if(points == null) { return; }

            int num_instances = points.Length;
            if(num_instances == 0) { return; }

            bool supportsInstancing = SystemInfo.supportsInstancing;
            int pidPointSize = Shader.PropertyToID("_PointSize");
            int pidAlembicPoints = Shader.PropertyToID("_AlembicPoints");
            int pidAlembicIDs = Shader.PropertyToID("_AlembicIDs");
            int pidAlembicID = Shader.PropertyToID("_AlembicID");

#if UNITY_5_6_OR_NEWER
            if (supportsInstancing && m_instancingMode == InstancingMode.Procedural)
            {
                m_material.EnableKeyword(m_kwAlembicProceduralInstancing);
                m_material.SetFloat(pidPointSize, m_pointSize);

                // update argument buffer
                if(m_cbArgs == null)
                {
                    m_cbArgs = new ComputeBuffer(1, m_args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                }
                m_args[0] = (int)m_mesh.GetIndexCount(0);
                m_args[1] = num_instances;
                m_cbArgs.SetData(m_args);

                // update points buffer
                if (m_cbPoints != null && m_cbPoints.count != num_instances)
                {
                    m_cbPoints.Release();
                    m_cbPoints = null;
                }
                if (m_cbPoints == null)
                {
                    m_cbPoints = new ComputeBuffer(num_instances, 12);
                }
                m_cbPoints.SetData(points);
                m_material.SetBuffer(pidAlembicPoints, m_cbPoints);

                // update ID buffer
                if (m_cbIDs != null && m_cbIDs.count != num_instances)
                {
                    m_cbIDs.Release();
                    m_cbIDs = null;
                }
                if (m_cbIDs == null)
                {
                    m_cbIDs = new ComputeBuffer(num_instances, 4);
                }
                ulong[] ids = apc.abcIDs;
                if (ids != null && ids.Length == num_instances)
                {
                    if (m_ids == null || m_ids.Length != num_instances)
                    {
                        m_ids = new float[num_instances];
                    }
                    for (int i = 0; i < num_instances; ++i)
                    {
                        m_ids[i] = ids[i];
                    }
                    m_cbIDs.SetData(m_ids);
                    m_material.SetBuffer(pidAlembicIDs, m_cbIDs);
                }

                // issue drawcall
                var bounds = new Bounds(apc.m_boundsCenter, apc.m_boundsExtents + m_mesh.bounds.extents);
                Graphics.DrawMeshInstancedIndirect(m_mesh, 0, m_material,
                    bounds, m_cbArgs, 0, null, m_shadow, m_receiveShadows, m_layer);
            }
            else
#endif
#if UNITY_5_5_OR_NEWER
            if (supportsInstancing && m_instancingMode == InstancingMode.Instancing)
            {
                if(m_material.IsKeywordEnabled(m_kwAlembicProceduralInstancing))
                {
                    m_material.DisableKeyword(m_kwAlembicProceduralInstancing);
                }

                // current Graphics.DrawMeshInstanced() can draw only up to 1023 instances.
                // multiple drawcalls maybe required.
                int num_batches = (num_instances + MaxInstancesParDraw - 1) / MaxInstancesParDraw;

                if (m_matrices == null || m_matrices.Length != MaxInstancesParDraw)
                {
                    m_matrices = new Matrix4x4[MaxInstancesParDraw];
                    for (int i = 0; i < MaxInstancesParDraw; ++i) { m_matrices[i] = Matrix4x4.identity; }
                }

                // setup alembic point IDs
                ulong[] ids = apc.abcIDs;
                bool alembicIDsAvailable = ids != null && ids.Length == num_instances;
                if (alembicIDsAvailable)
                {
                    if (m_ids == null || m_ids.Length != MaxInstancesParDraw)
                    {
                        m_ids = new float[MaxInstancesParDraw];
                    }
                    if (m_mpbs == null)
                    {
                        m_mpbs = new List<MaterialPropertyBlock>();
                    }
                    while (m_mpbs.Count < num_batches)
                    {
                        m_mpbs.Add(new MaterialPropertyBlock());
                    }
                }

                for (int ib = 0; ib < num_batches; ++ib)
                {
                    int ibegin = ib * MaxInstancesParDraw;
                    int iend = System.Math.Min(ibegin + MaxInstancesParDraw, num_instances);
                    int n = iend - ibegin;

                    // build matrices
                    for (int i = 0; i < n; ++i)
                    {
                        m_matrices[i].m00 = m_matrices[i].m11 = m_matrices[i].m22 = m_pointSize;
                        m_matrices[i].m03 = points[ibegin + i].x;
                        m_matrices[i].m13 = points[ibegin + i].y;
                        m_matrices[i].m23 = points[ibegin + i].z;
                    }

                    // send alembic points IDs to shader if needed
                    MaterialPropertyBlock mpb = null;
                    if (alembicIDsAvailable)
                    {
                        for (int i = 0; i < n; ++i)
                        {
                            m_ids[i] = ids[ibegin + i];
                        }
                        mpb = m_mpbs[ib];
                        mpb.SetFloatArray(pidAlembicIDs, m_ids);
                    }

                    // issue drawcall
                    Graphics.DrawMeshInstanced(m_mesh, 0, m_material, m_matrices, n, mpb, m_shadow, m_receiveShadows, m_layer);
                }
            }
            else
#endif
            {
                var matrix = Matrix4x4.identity;
                matrix.m00 = matrix.m11 = matrix.m22 = m_pointSize;
                for (int i = 0; i < num_instances; ++i)
                {
                    matrix.m03 = points[i].x;
                    matrix.m13 = points[i].y;
                    matrix.m23 = points[i].z;
                    Graphics.DrawMesh(m_mesh, matrix, m_material, m_layer, null, 0, null, m_shadow, m_receiveShadows);
                }
            }
        }

        public void Release()
        {
#if UNITY_5_6_OR_NEWER
            if (m_cbArgs != null) { m_cbArgs.Release(); m_cbArgs = null; }
            if (m_cbPoints != null) { m_cbPoints.Release(); m_cbPoints = null; }
            if (m_cbIDs != null) { m_cbIDs.Release(); m_cbIDs = null; }
#endif
        }


        void OnDisable()
        {
            Release();
        }

        void LateUpdate()
        {
            Flush();
#if UNITY_EDITOR
            m_dirty = true;
#endif
        }

#if UNITY_EDITOR
        void Reset()
        {
            if (m_material == null)
            {
                m_material = UnityEngine.Object.Instantiate(AbcUtils.GetDefaultMaterial());
                m_material.name = "Material_0";
            }
            if (m_mesh == null)
            {
                // todo
            }
        }

        void OnDrawGizmos()
        {
            // force draw particles while paused.
            // using OnDrawGizmos() is dirty workaround but I couldn't find better way...
            if (EditorApplication.isPaused && m_dirty)
            {
                Flush();
                m_dirty = false;
            }
        }
#endif
    }
}
