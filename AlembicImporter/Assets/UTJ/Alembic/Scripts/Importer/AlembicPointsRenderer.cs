using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(AlembicPointsCloud))]
    public class AlembicPointsRenderer : MonoBehaviour
    {
        [SerializeField] Mesh m_mesh;
        [SerializeField] Material[] m_materials;
        [SerializeField] Material m_motionVectorMaterial;
        [SerializeField] ShadowCastingMode m_castShadows = ShadowCastingMode.Off;
        [SerializeField] bool m_receiveShadows = true;
        [SerializeField] bool m_generateMotionVector = true;
        [SerializeField] float m_pointSize = 0.2f;

        Mesh m_prevMesh;
        ComputeBuffer m_cbPoints;
        ComputeBuffer m_cbVelocities;
        ComputeBuffer m_cbIDs;
        ComputeBuffer[] m_cbArgs;
        CommandBuffer m_cmdMotionVector;
        int[] m_args = new int[5] { 0, 0, 0, 0, 0 };
        Bounds m_bounds;
        Material[] m_materialsInternal;
        Material m_motionVectorMaterialsInternal;


        public Mesh sharedMesh
        {
            get { return m_mesh; }
            set { m_mesh = value; }
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

        Material SetupMotionVectorMaterial()
        {
            if (m_motionVectorMaterial == null)
                m_motionVectorMaterialsInternal = null;
            else if(m_motionVectorMaterialsInternal == null)
                m_motionVectorMaterialsInternal = new Material(m_motionVectorMaterial);
            return m_motionVectorMaterialsInternal;
        }


        public void Flush()
        {
            var apc = GetComponent<AlembicPointsCloud>();

            var points = apc.points;
            int numInstances = points.Count;
            if (numInstances == 0) { return; }
            var velocities = apc.velocities;
            var ids = apc.ids;

            var materials = SetupMaterials();
            var mvmaterial = SetupMotionVectorMaterial();
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

            // check if mesh changed
            if (m_prevMesh != mesh)
            {
                m_prevMesh = mesh;
                if (m_cbArgs != null)
                {
                    foreach (var cb in m_cbArgs) { cb.Release(); }
                    m_cbArgs = null;
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
            if (velocities.Count == numInstances)
            {
                abcHasVelocities = true;
                if (m_cbVelocities != null && m_cbVelocities.count < numInstances)
                {
                    m_cbVelocities.Release();
                    m_cbVelocities = null;
                }
                if (m_cbVelocities == null)
                {
                    m_cbVelocities = new ComputeBuffer(numInstances, 12);
                }
                m_cbVelocities.SetData(velocities.List);
            }

            // update ID buffer
            if (ids.Count == numInstances)
            {
                abcHasIDs = true;
                if (m_cbIDs != null && m_cbIDs.count < numInstances)
                {
                    m_cbIDs.Release();
                    m_cbIDs = null;
                }
                if (m_cbIDs == null)
                {
                    m_cbIDs = new ComputeBuffer(numInstances, 4);
                }
                m_cbIDs.SetData(ids.List);
            }

            // build bounds
            m_bounds = new Bounds(apc.m_boundsCenter, apc.m_boundsExtents + mesh.bounds.extents);


            // update materials
            Action<Material> updateMaterial = (mat) => {
                if (mat == null)
                    return;

                mat.SetVector("_Translate", pos);
                mat.SetVector("_Rotate", new Vector4(rot.x, rot.y, rot.z, rot.w));
                mat.SetVector("_Scale", scale);
                mat.SetFloat("_PointSize", m_pointSize);
                mat.SetBuffer("_AlembicPoints", m_cbPoints);
                if (abcHasIDs)
                {
                    mat.SetInt("_AlembicHasIDs", 1);
                    mat.SetBuffer("_AlembicIDs", m_cbIDs);
                }
                if (abcHasVelocities)
                {
                    mat.SetInt("_AlembicHasVelocities", 1);
                    mat.SetBuffer("_AlembicVelocities", m_cbVelocities);
                }
            };

            foreach (var material in materials)
                updateMaterial.Invoke(material);
            updateMaterial.Invoke(mvmaterial);


            // update argument buffer
            if (m_cbArgs == null || m_cbArgs.Length != submeshCount)
            {
                m_cbArgs = new ComputeBuffer[submeshCount];
                for (int si = 0; si < submeshCount; ++si)
                {
                    var cbArgs = new ComputeBuffer(1, m_args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                    m_cbArgs[si] = cbArgs;
                    m_args[0] = (int)mesh.GetIndexCount(si);
                    m_args[1] = numInstances;
                    cbArgs.SetData(m_args);
                }
            }

            // issue drawcalls
            for (int si = 0; si < submeshCount; ++si)
            {
                var args = m_cbArgs[si];
                var material = materials[si];
                if (material == null)
                    continue;
                Graphics.DrawMeshInstancedIndirect(mesh, si, material,
                    m_bounds, args, 0, null, m_castShadows, m_receiveShadows, layer);
            }
        }

        void FlushMotionVector()
        {
            if (!m_generateMotionVector || Camera.current == null || (Camera.current.depthTextureMode & DepthTextureMode.MotionVectors) == 0)
                return;

            // assume setup is already done in Flush()

            var material = SetupMotionVectorMaterial();
            var mesh = m_mesh;
            var apc = GetComponent<AlembicPointsCloud>();
            if (mesh == null || material == null || apc.velocities.Count == 0)
                return;

            int layer = gameObject.layer;

            if (m_cmdMotionVector == null)
            {
                m_cmdMotionVector = new CommandBuffer();
                m_cmdMotionVector.name = "AlembicPointsRenderer";
            }
            m_cmdMotionVector.Clear();
            m_cmdMotionVector.SetRenderTarget(BuiltinRenderTextureType.MotionVectors, BuiltinRenderTextureType.CameraTarget);
            for (int si = 0; si < mesh.subMeshCount; ++si)
                m_cmdMotionVector.DrawMeshInstancedIndirect(mesh, si, material, 0, m_cbArgs[si], 0);
            Graphics.ExecuteCommandBuffer(m_cmdMotionVector);
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
            if (m_cmdMotionVector != null) { m_cmdMotionVector.Release(); m_cmdMotionVector = null; }
        }


        void OnDisable()
        {
            Release();
        }

        void LateUpdate()
        {
            Flush();
        }

        void OnRenderObject()
        {
            FlushMotionVector();
        }

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
            if(m_motionVectorMaterial == null)
            {
                var mat = new Material(AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("05cc257315ad20240b491c6a72f29db6")));
                mat.name = "Default Alembic Points Motion Vector";
                m_motionVectorMaterial = mat;
            }
#endif
        }
    }
}
