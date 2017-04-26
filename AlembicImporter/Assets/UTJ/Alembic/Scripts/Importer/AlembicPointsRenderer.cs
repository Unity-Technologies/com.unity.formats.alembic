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

        [SerializeField] Mesh m_mesh;
        [SerializeField] Material[] m_materials;
        [SerializeField] ShadowCastingMode m_castShadows = ShadowCastingMode.Off;
        [SerializeField] bool m_receiveShadows = false;
        [SerializeField] float m_pointSize = 0.2f;
        [SerializeField] InstancingMode m_instancingMode =
#if UNITY_5_5_OR_NEWER
            InstancingMode.Instancing;
#else
            InstancingMode.NoInstancing;
#endif
        [Tooltip("Use Alembic Points IDs as shader input")]
        [SerializeField] bool m_useAlembicIDs = false;

#if UNITY_5_5_OR_NEWER
        const string kAlembicProceduralInstancing = "ALEMBIC_PROCEDURAL_INSTANCING_ENABLED";
        Matrix4x4[] m_matrices;
        float[] m_ids;
        List<MaterialPropertyBlock> m_mpbs;
#endif
#if UNITY_5_6_OR_NEWER
        ComputeBuffer m_cbPoints;
        ComputeBuffer m_cbIDs;
        ComputeBuffer[] m_cbArgs;
        int[] m_args = new int[5] { 0, 0, 0, 0, 0 };
#endif
#if UNITY_EDITOR
        bool m_dirty = false;
#endif
        Material[] m_materialsInternal;


        public Mesh sharedMesh
        {
            get { return m_mesh; }
            set { m_mesh = value; }
        }

        public Material material
        {
            get {
                return m_materialsInternal != null && m_materialsInternal.Length > 0 ? m_materialsInternal[0] : null;
            }
            set {
                m_materialsInternal = null;
                m_materials = new Material[] { value };
            }
        }
        public Material[] materials
        {
            get { return m_materialsInternal; }
            set {
                m_materialsInternal = null;
                m_materials = value;
            }
        }

        public Material sharedMaterial
        {
            get
            {
                return m_materials != null && m_materials.Length > 0 ? m_materials[0] : null;
            }
            set
            {
                m_materialsInternal = m_materials = new Material[] { value };
            }
        }
        public Material[] sharedMaterials
        {
            get { return m_materials; }
            set
            {
                m_materialsInternal = m_materials = value;
            }
        }


        Material[] SetupMaterials()
        {
            if (m_materials == null || m_materials.Length == 0)
            {
                m_materialsInternal = null;
            }
            else if (m_materialsInternal == null || m_materialsInternal.Length == 0)
            {
                m_materialsInternal = new Material[m_materials.Length];
                for (int i = 0; i < m_materials.Length; ++i)
                {
                    m_materialsInternal[i] = new Material(m_materials[i]);
                }
            }
            return m_materialsInternal;
        }

        public void Flush()
        {
            var apc = GetComponent<AlembicPointsCloud>();
            var points = apc.abcPositions;
            if(points == null) { return; }

            int num_instances = points.Length;
            if(num_instances == 0) { return; }

            var materials = SetupMaterials();
            var mesh = m_mesh;
            if (mesh == null || materials == null) { return; }

            int num_submeshes = System.Math.Min(mesh.subMeshCount, materials.Length);
            int layer = gameObject.layer;

            var trans = GetComponent<Transform>();
            var pos = trans.position;
            var rot = trans.rotation;
            var scale = trans.lossyScale;
            var pscale = scale * m_pointSize;

            bool supportsInstancing = SystemInfo.supportsInstancing;
#if UNITY_5_6_OR_NEWER
            int pidPointSize = Shader.PropertyToID("_PointSize");
            int pidTranslate = Shader.PropertyToID("_Translate");
            int pidRotate = Shader.PropertyToID("_Rotate");
            int pidScale = Shader.PropertyToID("_Scale");
            int pidAlembicPoints = Shader.PropertyToID("_AlembicPoints");
#endif
#if UNITY_5_5_OR_NEWER
            int pidAlembicIDs = Shader.PropertyToID("_AlembicIDs");
#endif

            if (!supportsInstancing && m_instancingMode != InstancingMode.NoInstancing)
            {
                Debug.LogWarning("AlembicPointsRenderer: Instancing is not supported on this system. fallback to InstancingMode.NoInstancing.");
                m_instancingMode = InstancingMode.NoInstancing;
            }

#if UNITY_5_6_OR_NEWER
            if (m_instancingMode == InstancingMode.Procedural && !SystemInfo.supportsComputeShaders)
            {
                Debug.LogWarning("AlembicPointsRenderer: InstancingMode.Procedural is not supported on this system. fallback to InstancingMode.Instancing.");
                m_instancingMode = InstancingMode.Instancing;
            }

            if (supportsInstancing && m_instancingMode == InstancingMode.Procedural)
            {
                // Graphics.DrawMeshInstancedIndirect() route

                // update argument buffer
                if (m_cbArgs == null || m_cbArgs.Length != num_submeshes)
                {
                    Release();
                    m_cbArgs = new ComputeBuffer[num_submeshes];
                    for (int i = 0; i < num_submeshes; ++i)
                    {
                        m_cbArgs[i] = new ComputeBuffer(1, m_args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                    }
                }

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

                // update ID buffer
                bool alembicIDsAvailable = false;
                if (m_useAlembicIDs)
                {
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
                        alembicIDsAvailable = true;
                    }
                }

                // build bounds
                var bounds = new Bounds(apc.m_boundsCenter, apc.m_boundsExtents + mesh.bounds.extents);

                // issue drawcalls
                for (int si = 0; si < num_submeshes; ++si)
                {
                    var args = m_cbArgs[si];
                    m_args[0] = (int)mesh.GetIndexCount(0);
                    m_args[1] = num_instances;
                    args.SetData(m_args);

                    var material = materials[si];
                    material.EnableKeyword(kAlembicProceduralInstancing);
                    material.SetFloat(pidPointSize, m_pointSize);
                    material.SetBuffer(pidAlembicPoints, m_cbPoints);
                    if (alembicIDsAvailable) { material.SetBuffer(pidAlembicIDs, m_cbIDs); }
                    Graphics.DrawMeshInstancedIndirect(mesh, si, material,
                        bounds, args, 0, null, m_castShadows, m_receiveShadows, layer);
                }
            }
            else
#endif
#if UNITY_5_5_OR_NEWER
            if (supportsInstancing && m_instancingMode == InstancingMode.Instancing)
            {
                // Graphics.DrawMeshInstanced() route
                // Graphics.DrawMeshInstanced() can draw only up to 1023 instances.
                // multiple drawcalls maybe required.

                int num_batches = (num_instances + MaxInstancesParDraw - 1) / MaxInstancesParDraw;

                if (m_matrices == null || m_matrices.Length != MaxInstancesParDraw)
                {
                    m_matrices = new Matrix4x4[MaxInstancesParDraw];
                    for (int i = 0; i < MaxInstancesParDraw; ++i) { m_matrices[i] = Matrix4x4.identity; }
                }

                for (int si = 0; si < num_submeshes; ++si)
                {
                    var material = materials[si];
                    if (material.IsKeywordEnabled(kAlembicProceduralInstancing))
                    {
                        material.DisableKeyword(kAlembicProceduralInstancing);
                    }
                }

                // setup alembic point IDs
                bool alembicIDsAvailable = false;
                ulong[] ids = null;
                if (m_useAlembicIDs)
                {
                    ids = apc.abcIDs;
                    alembicIDsAvailable = ids != null && ids.Length == num_instances;
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
                }

                for (int ib = 0; ib < num_batches; ++ib)
                {
                    int ibegin = ib * MaxInstancesParDraw;
                    int iend = System.Math.Min(ibegin + MaxInstancesParDraw, num_instances);
                    int n = iend - ibegin;

                    // build matrices
                    for (int ii = 0; ii < n; ++ii)
                    {
                        var ppos = points[ibegin + ii];
                        ppos.x *= scale.x;
                        ppos.y *= scale.y;
                        ppos.z *= scale.z;
                        ppos = (rot * ppos) + pos;
                        m_matrices[ii].SetTRS(ppos, rot, pscale);
                    }

                    MaterialPropertyBlock mpb = null;
                    if (alembicIDsAvailable)
                    {
                        for (int ii = 0; ii < n; ++ii)
                        {
                            m_ids[ii] = ids[ibegin + ii];
                        }
                        mpb = m_mpbs[ib];
                        mpb.SetFloatArray(pidAlembicIDs, m_ids);
                    }

                    // issue drawcalls
                    for (int si = 0; si < num_submeshes; ++si)
                    {
                        var material = materials[si];
                        Graphics.DrawMeshInstanced(mesh, si, material, m_matrices, n, mpb, m_castShadows, m_receiveShadows, layer);
                    }
                }
            }
            else
#endif
            {
                // Graphics.DrawMesh() route
                // not use IDs in this case because it's too expensive...

                var matrix = Matrix4x4.identity;
                for (int ii = 0; ii < num_instances; ++ii)
                {
                    var ppos = points[ii];
                    ppos.x *= scale.x;
                    ppos.y *= scale.y;
                    ppos.z *= scale.z;
                    ppos = (rot * ppos) + pos;
                    matrix.SetTRS(ppos, rot, pscale);

                    // issue drawcalls
                    for (int si = 0; si < num_submeshes; ++si)
                    {
                        var material = materials[si];
                        Graphics.DrawMesh(mesh, matrix, material, layer, null, si, null, m_castShadows, m_receiveShadows);
                    }
                }
            }
        }

        public void Release()
        {
#if UNITY_5_6_OR_NEWER
            if (m_cbArgs != null) {
                foreach(var cb in m_cbArgs) { cb.Release(); }
                m_cbArgs = null;
            }
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
        void OnDrawGizmos()
        {
            // force draw particles while paused.
            // using OnDrawGizmos() is dirty workaround but I couldn't find better way...
            if (EditorApplication.isPaused && m_dirty)
            {
                Flush();
                m_dirty = false;
            }

            if(!EditorApplication.isPlaying || EditorApplication.isPaused)
            {
                // force update internal materials to apply material parameter changes immediately
                m_materialsInternal = null;
            }
        }
#endif
    }
}
