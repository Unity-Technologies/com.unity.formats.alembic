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
        [SerializeField] Mesh m_mesh;
        [SerializeField] Material[] m_materials;
        [SerializeField] ShadowCastingMode m_castShadows = ShadowCastingMode.Off;
        [SerializeField] bool m_receiveShadows = false;
        [SerializeField] float m_pointSize = 0.2f;
        [Tooltip("Use Alembic Points IDs as shader input")]
        [SerializeField] bool m_useAlembicIDs = false;

        ComputeBuffer m_cbPoints;
        ComputeBuffer m_cbVelocities;
        ComputeBuffer m_cbIDs;
        ComputeBuffer[] m_cbArgs;
        int[] m_args = new int[5] { 0, 0, 0, 0, 0 };
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
            else if (m_materialsInternal == null || m_materialsInternal.Length != m_materials.Length)
            {
                m_materialsInternal = new Material[m_materials.Length];
                for (int i = 0; i < m_materials.Length; ++i)
                {
                    if (m_materials[i] != null)
                        m_materialsInternal[i] = new Material(m_materials[i]);
                }
            }
            return m_materialsInternal;
        }

        public void Flush()
        {
            var apc = GetComponent<AlembicPointsCloud>();

            var points = apc.points;
            int numInstances = points.Count;
            if(numInstances == 0) { return; }

            var materials = SetupMaterials();
            var mesh = m_mesh;
            if (mesh == null || materials == null) { return; }

            int submeshCount = System.Math.Min(mesh.subMeshCount, materials.Length);
            int layer = gameObject.layer;

            var trans = GetComponent<Transform>();
            var pos = trans.position;
            var rot = trans.rotation;
            var scale = trans.lossyScale;

            bool supportsInstancing = SystemInfo.supportsInstancing && SystemInfo.supportsComputeShaders;
            if (!supportsInstancing)
            {
                Debug.LogWarning("AlembicPointsRenderer: Instancing is not supported on this system.");
                return;
            }

            for (int si = 0; si < submeshCount; ++si)
            {
                var material = materials[si];
                if (material == null)
                    continue;
                material.SetVector("_Translate", pos);
                material.SetVector("_Rotate", new Vector4(rot.x, rot.y, rot.z, rot.w));
                material.SetVector("_Scale", scale);
                material.SetFloat("_PointSize", m_pointSize);
            }

            {
                // update argument buffer
                if (m_cbArgs == null || m_cbArgs.Length != submeshCount)
                {
                    Release();
                    m_cbArgs = new ComputeBuffer[submeshCount];
                    for (int i = 0; i < submeshCount; ++i)
                    {
                        m_cbArgs[i] = new ComputeBuffer(1, m_args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                    }
                }

                bool abcHasVelocities = false;
                bool abcHasIDs = false;

                // update points buffer
                if (m_cbPoints != null && m_cbPoints.count < numInstances)
                {
                    m_cbPoints.Release();
                    m_cbPoints = null;
                }
                if (m_cbPoints == null)
                {
                    m_cbPoints = new ComputeBuffer(numInstances, 12);
                }
                m_cbPoints.SetData(points.List);

                // update velocity buffer
                if (m_useAlembicIDs)
                {
                    if (m_cbVelocities != null && m_cbVelocities.count < numInstances)
                    {
                        m_cbVelocities.Release();
                        m_cbVelocities = null;
                    }
                    if (m_cbVelocities == null)
                    {
                        m_cbVelocities = new ComputeBuffer(numInstances, 12);
                    }
                    var ids = apc.ids;
                    if (ids != null && ids.Count == numInstances)
                    {
                        m_cbVelocities.SetData(apc.velocities.List);
                        abcHasVelocities = true;
                    }
                }

                // update ID buffer
                if (m_useAlembicIDs)
                {
                    if (m_cbIDs != null && m_cbIDs.count < numInstances)
                    {
                        m_cbIDs.Release();
                        m_cbIDs = null;
                    }
                    if (m_cbIDs == null)
                    {
                        m_cbIDs = new ComputeBuffer(numInstances, 4);
                    }
                    var ids = apc.ids;
                    if (ids != null && ids.Count == numInstances)
                    {
                        m_cbIDs.SetData(apc.ids.List);
                        abcHasIDs = true;
                    }
                }

                // build bounds
                var bounds = new Bounds(apc.m_boundsCenter, apc.m_boundsExtents + mesh.bounds.extents);

                // issue drawcalls
                for (int si = 0; si < submeshCount; ++si)
                {
                    var args = m_cbArgs[si];
                    m_args[0] = (int)mesh.GetIndexCount(0);
                    m_args[1] = numInstances;
                    args.SetData(m_args);

                    var material = materials[si];
                    if (material == null)
                        continue;
                    material.SetBuffer("_AlembicPoints", m_cbPoints);
                    if (abcHasVelocities)
                    {
                        material.SetInt("_AlembicHasVelocities", 1);
                        material.SetBuffer("_AlembicVelocities", m_cbVelocities);
                    }
                    if (abcHasIDs)
                    {
                        material.SetInt("_AlembicHasIDs", 1);
                        material.SetBuffer("_AlembicIDs", m_cbIDs);
                    }
                    Graphics.DrawMeshInstancedIndirect(mesh, si, material,
                        bounds, args, 0, null, m_castShadows, m_receiveShadows, layer);
                }
            }
        }

        public void Release()
        {
            if (m_cbArgs != null) {
                foreach(var cb in m_cbArgs) { cb.Release(); }
                m_cbArgs = null;
            }
            if (m_cbPoints != null) { m_cbPoints.Release(); m_cbPoints = null; }
            if (m_cbVelocities != null) { m_cbVelocities.Release(); m_cbVelocities = null; }
            if (m_cbIDs != null) { m_cbIDs.Release(); m_cbIDs = null; }
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

        private void Start()
        {
#if UNITY_EDITOR
            if (m_mesh == null)
            {
                var cubeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m_mesh = cubeGO.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(cubeGO);
            }
            if (m_materials != null)
            {
                bool allNull = true;
                foreach (var m in m_materials)
                    if (m_materials != null)
                        allNull = false;
                if (allNull)
                    m_materials = null;
            }
            if (m_materials == null)
            {
                var mat = new Material(AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("a002496809b1b604c8a724108e6add6e")));
                mat.name = "Default Alembic Points";
                m_materials = new Material[] { mat };
            }
#endif
        }
    }
}
