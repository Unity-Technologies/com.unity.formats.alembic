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
        public const int MaxInstancesParDraw = 1023;

        public Mesh m_mesh;
        public Material m_material;
        public ShadowCastingMode m_shadow = ShadowCastingMode.Off;
        public bool m_receiveShadows = false;
        public LayerSelector m_layer = 0;
        public float m_pointSize = 0.1f;

        Matrix4x4[] m_matrices;
        float[] m_ids;
        List<MaterialPropertyBlock> m_mpb;


        public void Flush()
        {
            if(m_mesh == null || m_material == null) { return; }

            var apc = GetComponent<AlembicPointsCloud>();
            var positions = apc.abcPositions;
            if(positions == null) { return; }

            int num_instances = positions.Length;
            if(num_instances == 0) { return; }

            int ids_pid = Shader.PropertyToID("_AlembicID");

#if UNITY_5_5_OR_NEWER
            if(SystemInfo.supportsInstancing)
            {
                // current Graphics.DrawMeshInstanced() can draw only up to 1023 instances.
                // multiple drawcalls maybe required.
                int num_batches = (num_instances + MaxInstancesParDraw - 1) / MaxInstancesParDraw;

                if (m_matrices == null)
                {
                    m_matrices = new Matrix4x4[MaxInstancesParDraw];
                    for (int i = 0; i < MaxInstancesParDraw; ++i) { m_matrices[i] = Matrix4x4.identity; }
                }

                // if material require alembic points ID, pass it via MaterialPropertyBlock
                ulong[] ids = null;
                bool alembicIDsRequired = m_material.HasProperty(ids_pid);
                if (alembicIDsRequired)
                {
                    ids = apc.abcIDs;
                    if (ids == null && ids.Length != num_instances)
                    {
                        alembicIDsRequired = false;
                    }
                    else
                    {
                        if (m_ids == null)
                        {
                            m_ids = new float[MaxInstancesParDraw];
                        }
                        if (m_mpb == null)
                        {
                            m_mpb = new List<MaterialPropertyBlock>();
                        }
                        while (m_mpb.Count < num_batches)
                        {
                            m_mpb.Add(new MaterialPropertyBlock());
                        }
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
                        m_matrices[i].m03 = positions[ibegin + i].x;
                        m_matrices[i].m13 = positions[ibegin + i].y;
                        m_matrices[i].m23 = positions[ibegin + i].z;
                    }

                    // send alembic points IDs to shader if needed
                    MaterialPropertyBlock mpb = null;
                    if (alembicIDsRequired)
                    {
                        for (int i = 0; i < n; ++i)
                        {
                            m_ids[i] = ids[ibegin + i];
                        }
                        mpb = m_mpb[ib];
                        mpb.SetFloatArray(ids_pid, m_ids);
                    }

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
                    matrix.m03 = positions[i].x;
                    matrix.m13 = positions[i].y;
                    matrix.m23 = positions[i].z;
                    Graphics.DrawMesh(m_mesh, matrix, m_material, m_layer);
                }
            }
        }

        void Reset()
        {
#if UNITY_EDITOR
            if(m_material == null)
            {
                m_material = UnityEngine.Object.Instantiate(AbcUtils.GetDefaultMaterial());
                m_material.name = "Material_0";
            }
            if(m_mesh == null)
            {
                // todo
            }
#endif
        }

        void LateUpdate()
        {
            Flush();
        }
    }
}
