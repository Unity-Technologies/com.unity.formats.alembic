using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

namespace UTJ
{
    [ExecuteInEditMode]
    public class AlembicMesh : AlembicElement
    {
        [Serializable]
        public class Split
        {
            public Vector3[] positionCache;
            public Vector3[] normalCache;
            public Vector2[] uvCache;
            public Vector4[] tangentCache;
            public Mesh mesh;
            public GameObject host;
    
            public bool clear;
            public int submeshCount;
            public bool active;
    
            public Vector3 center;
            public Vector3 size;
        }
    
        [Serializable]
        public class Submesh
        {
            public int[] indexCache;
            public int facesetIndex;
            public int splitIndex;
            public int index;
    
            public bool update;
        }
    
        public AbcAPI.aiFaceWindingOverride m_faceWinding = AbcAPI.aiFaceWindingOverride.InheritStreamSetting;
        public AbcAPI.aiNormalsModeOverride m_normalsMode = AbcAPI.aiNormalsModeOverride.InheritStreamSetting;
        public AbcAPI.aiTangentsModeOverride m_tangentsMode = AbcAPI.aiTangentsModeOverride.InheritStreamSetting;
        public bool m_cacheTangentsSplits = true;
        
        [HideInInspector] public bool hasFacesets = false;
        [HideInInspector] public List<Submesh> m_submeshes = new List<Submesh>();
        [HideInInspector] public List<Split> m_splits = new List<Split>();
    
        AbcAPI.aiMeshSummary m_summary;
        AbcAPI.aiMeshSampleSummary m_sampleSummary;
        bool m_freshSetup = false;
    
    
        public static IntPtr GetArrayPtr(Array a)
        {
            return Marshal.UnsafeAddrOfPinnedArrayElement(a, 0);
        }
    
        void UpdateSplits(int numSplits)
        {
            Split split = null;
    
            if (m_summary.topologyVariance == AbcAPI.aiTopologyVariance.Heterogeneous || numSplits > 1)
            {
                for (int i=0; i<numSplits; ++i)
                {
                    if (i >= m_splits.Count)
                    {
                        split = new Split
                        {
                            positionCache = new Vector3[0],
                            normalCache = new Vector3[0],
                            uvCache = new Vector2[0],
                            tangentCache = new Vector4[0],
                            mesh = null,
                            host = null,
                            clear = true,
                            submeshCount = 0,
                            active = true,
                            center = Vector3.zero,
                            size = Vector3.zero
                        };
    
                        m_splits.Add(split);
                    }
                    else
                    {
                        m_splits[i].active = true;
                    }
                }
            }
            else
            {
                if (m_splits.Count == 0)
                {
                    split = new Split
                    {
                        positionCache = new Vector3[0],
                        normalCache = new Vector3[0],
                        uvCache = new Vector2[0],
                        tangentCache = new Vector4[0],
                        mesh = null,
                        host = m_trans.gameObject,
                        clear = true,
                        submeshCount = 0,
                        active = true,
                        center = Vector3.zero,
                        size = Vector3.zero
                    };
    
                    m_splits.Add(split);
                }
                else
                {
                    m_splits[0].active = true;
                }
            }
    
            for (int i=numSplits; i<m_splits.Count; ++i)
            {
                m_splits[i].active = false;
            }
        }
    
        public override void AbcSetup(AlembicStream abcStream,
                                      AbcAPI.aiObject abcObj,
                                      AbcAPI.aiSchema abcSchema)
        {
            base.AbcSetup(abcStream, abcObj, abcSchema);
    
            AbcAPI.aiPolyMeshGetSummary(abcSchema, ref m_summary);
    
            m_freshSetup = true;
        }
        
        public override void AbcDestroy()
        {
        }
    
        public override void AbcGetConfig(ref AbcAPI.aiConfig config)
        {
            if (m_normalsMode != AbcAPI.aiNormalsModeOverride.InheritStreamSetting)
            {
                config.normalsMode = (AbcAPI.aiNormalsMode) m_normalsMode;
            }
    
            if (m_tangentsMode != AbcAPI.aiTangentsModeOverride.InheritStreamSetting)
            {
                config.tangentsMode = (AbcAPI.aiTangentsMode) m_tangentsMode;
            }
    
            if (m_faceWinding != AbcAPI.aiFaceWindingOverride.InheritStreamSetting)
            {
                config.swapFaceWinding = (m_faceWinding == AbcAPI.aiFaceWindingOverride.Swap);
            }
    
            config.cacheTangentsSplits = m_cacheTangentsSplits;
    
            // if 'forceUpdate' is set true, even if alembic sample data do not change at all
            // AbcSampleUpdated will still be called (topologyChanged will be false)
    
            AlembicMaterial abcMaterials = m_trans.GetComponent<AlembicMaterial>();
    
            config.forceUpdate = m_freshSetup || (abcMaterials != null ? abcMaterials.HasFacesetsChanged() : hasFacesets);
        }
    
        public override void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged)
        {
            AlembicMaterial abcMaterials = m_trans.GetComponent<AlembicMaterial>();
    
            if (abcMaterials != null)
            {
                if (abcMaterials.HasFacesetsChanged())
                {
                    AbcVerboseLog("AlembicMesh.AbcSampleUpdated: Facesets updated, force topology update");
                    topologyChanged = true;
                }
    
                hasFacesets = (abcMaterials.GetFacesetsCount() > 0);
            }
            else if (hasFacesets)
            {
                AbcVerboseLog("AlembicMesh.AbcSampleUpdated: Facesets cleared, force topology update");
                topologyChanged = true;
                hasFacesets = false;
            }
    
            if (m_freshSetup)
            {
                topologyChanged = true;
    
                m_freshSetup = false;
            }
    
            AbcAPI.aiPolyMeshGetSampleSummary(sample, ref m_sampleSummary, topologyChanged);
    
            AbcAPI.aiPolyMeshData vertexData = default(AbcAPI.aiPolyMeshData);
    
            UpdateSplits(m_sampleSummary.splitCount);
    
            for (int s=0; s<m_sampleSummary.splitCount; ++s)
            {
                Split split = m_splits[s];
    
                split.clear = topologyChanged;
                split.active = true;
    
                int vertexCount = AbcAPI.aiPolyMeshGetVertexBufferLength(sample, s);
    
                Array.Resize(ref split.positionCache, vertexCount);
                vertexData.positions = GetArrayPtr(split.positionCache);
    
                if (m_sampleSummary.hasNormals)
                {
                    Array.Resize(ref split.normalCache, vertexCount);
                    vertexData.normals = GetArrayPtr(split.normalCache);
                }
                else
                {
                    Array.Resize(ref split.normalCache, 0);
                    vertexData.normals = IntPtr.Zero;
                }
    
                if (m_sampleSummary.hasUVs)
                {
                    Array.Resize(ref split.uvCache, vertexCount);
                    vertexData.uvs = GetArrayPtr(split.uvCache);
                }
                else
                {
                    Array.Resize(ref split.uvCache, 0);
                    vertexData.uvs = IntPtr.Zero;
                }
    
                if (m_sampleSummary.hasTangents)
                {
                    Array.Resize(ref split.tangentCache, vertexCount);
                    vertexData.tangents = GetArrayPtr(split.tangentCache);
                }
                else
                {
                    Array.Resize(ref split.tangentCache, 0);
                    vertexData.tangents = IntPtr.Zero;
                }
    
                AbcAPI.aiPolyMeshFillVertexBuffer(sample, s, ref vertexData);
    
                split.center = vertexData.center;
                split.size = vertexData.size;
            }
    
            if (topologyChanged)
            {
                AbcAPI.aiFacesets facesets = default(AbcAPI.aiFacesets);
                AbcAPI.aiSubmeshSummary submeshSummary = default(AbcAPI.aiSubmeshSummary);
                AbcAPI.aiSubmeshData submeshData = default(AbcAPI.aiSubmeshData);
    
                if (abcMaterials != null)
                {
                    abcMaterials.GetFacesets(ref facesets);
                }
                
                int numSubmeshes = AbcAPI.aiPolyMeshPrepareSubmeshes(sample, ref facesets);
    
                if (m_submeshes.Count > numSubmeshes)
                {
                    m_submeshes.RemoveRange(numSubmeshes, m_submeshes.Count - numSubmeshes);
                }
                
                for (int s=0; s<m_sampleSummary.splitCount; ++s)
                {
                    m_splits[s].submeshCount = AbcAPI.aiPolyMeshGetSplitSubmeshCount(sample, s);
                }
    
                while (AbcAPI.aiPolyMeshGetNextSubmesh(sample, ref submeshSummary))
                {
                    if (submeshSummary.splitIndex >= m_splits.Count)
                    {
                        Debug.Log("Invalid split index");
                        continue;
                    }
    
                    Submesh submesh = null;
    
                    if (submeshSummary.index < m_submeshes.Count)
                    {
                        submesh = m_submeshes[submeshSummary.index];
                    }
                    else
                    {
                        submesh = new Submesh
                        {
                            indexCache = new int[0],
                            facesetIndex = -1,
                            splitIndex = -1,
                            index = -1,
                            update = true
                        };
    
                        m_submeshes.Add(submesh);
                    }
    
                    submesh.facesetIndex = submeshSummary.facesetIndex;
                    submesh.splitIndex = submeshSummary.splitIndex;
                    submesh.index = submeshSummary.splitSubmeshIndex;
                    submesh.update = true;
    
                    Array.Resize(ref submesh.indexCache, 3 * submeshSummary.triangleCount);
    
                    submeshData.indices = GetArrayPtr(submesh.indexCache);
    
                    AbcAPI.aiPolyMeshFillSubmeshIndices(sample, ref submeshSummary, ref submeshData);
                }
                
                if (abcMaterials != null)
                {
                    abcMaterials.AknowledgeFacesetsChanges();
                }
            }
            else
            {
                for (int i=0; i<m_submeshes.Count; ++i)
                {
                    m_submeshes[i].update = false;
                }
            }
    
            AbcDirty();
        }
    
        public override void AbcUpdate()
        {
            if (!AbcIsDirty())
            {
                return;
            }
    
            bool useSubObjects = (m_summary.topologyVariance == AbcAPI.aiTopologyVariance.Heterogeneous || m_sampleSummary.splitCount > 1);
    
            for (int s=0; s<m_splits.Count; ++s)
            {
                Split split = m_splits[s];
    
                if (split.active)
                {
                    // Feshly created splits may not have their host set yet
                    if (split.host == null)
                    {
                        if (useSubObjects)
                        {
                            string name = m_trans.gameObject.name + "_split_" + s;
    
                            Transform trans = m_trans.FindChild(name);
    
                            if (trans == null)
                            {
                                GameObject go = new GameObject();
                                go.name = name;
    
                                trans = go.GetComponent<Transform>();
                                trans.parent = m_trans;
                                trans.localPosition = Vector3.zero;
                                trans.localEulerAngles = Vector3.zero;
                                trans.localScale = Vector3.one;
                            }
    
                            split.host = trans.gameObject;
                        }
                        else
                        {
                            split.host = m_trans.gameObject;
                        }
                    }
    
                    // Feshly created splits may not have their mesh set yet
                    if (split.mesh == null)
                    {
                        split.mesh = AddMeshComponents(m_abcObj, split.host);
                        split.mesh.name = split.host.name;
                    }
    
                    if (split.clear)
                    {
                        split.mesh.Clear();
                    }
    
                    split.mesh.vertices = split.positionCache;
                    split.mesh.normals = split.normalCache;
                    split.mesh.tangents = split.tangentCache;
                    split.mesh.uv = split.uvCache;
                    // update the bounds
                    split.mesh.bounds = new Bounds(split.center, split.size);
    
                    if (split.clear)
                    {
                        split.mesh.subMeshCount = split.submeshCount;
    
                        MeshRenderer renderer = split.host.GetComponent<MeshRenderer>();
                        
                        Material[] currentMaterials = renderer.sharedMaterials;
    
                        int nmat = currentMaterials.Length;
    
                        if (nmat != split.submeshCount)
                        {
                            Material[] materials = new Material[split.submeshCount];
                            
                            int copyTo = (nmat < split.submeshCount ? nmat : split.submeshCount);
    
                            for (int i=0; i<copyTo; ++i)
                            {
                                materials[i] = currentMaterials[i];
                            }
    
    #if UNITY_EDITOR
                            for (int i=copyTo; i<split.submeshCount; ++i)
                            {
                                Material material = UnityEngine.Object.Instantiate(AbcUtils.GetDefaultMaterial());
                                material.name = "Material_" + Convert.ToString(i);
                                
                                materials[i] = material;
                            }
    #endif
    
                            renderer.sharedMaterials = materials;
                        }
                    }
    
                    split.clear = false;
    
                    split.host.SetActive(true);
                }
                else
                {
                    split.host.SetActive(false);
                }
            }
    
            for (int s=0; s<m_submeshes.Count; ++s)
            {
                Submesh submesh = m_submeshes[s];
    
                if (submesh.update)
                {
                    m_splits[submesh.splitIndex].mesh.SetIndices(submesh.indexCache, MeshTopology.Triangles, submesh.index);
    
                    submesh.update = false;
                }
            }
    
            if (!m_sampleSummary.hasNormals && !m_sampleSummary.hasTangents)
            {
                for (int s=0; s<m_sampleSummary.splitCount; ++s)
                {
                    m_splits[s].mesh.RecalculateNormals();
                }
            }
            
            AbcClean();
        }
    
        Mesh AddMeshComponents(AbcAPI.aiObject abc, GameObject gameObject)
        {
            Mesh mesh = null;
            
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                mesh = new Mesh();
                mesh.MarkDynamic();
    
                if (meshFilter == null)
                {
                    meshFilter = gameObject.AddComponent<MeshFilter>();
                }
    
                meshFilter.sharedMesh = mesh;
    
                MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
    
                if (renderer == null)
                {
                    renderer = gameObject.AddComponent<MeshRenderer>();
                }
    
    #if UNITY_EDITOR
                Material material = UnityEngine.Object.Instantiate(AbcUtils.GetDefaultMaterial());
                material.name = "Material_0";
                renderer.sharedMaterial = material;
    #endif
            }
            else
            {
                mesh = meshFilter.sharedMesh;
            }
    
            return mesh;
        }
    }
}
