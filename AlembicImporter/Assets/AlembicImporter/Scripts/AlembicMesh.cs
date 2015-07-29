using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

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

    public override void AbcSetup(AlembicStream abcStream,
                                  AbcAPI.aiObject abcObj,
                                  AbcAPI.aiSchema abcSchema)
    {
        base.AbcSetup(abcStream, abcObj, abcSchema);

        AbcAPI.aiPolyMeshGetSummary(abcSchema, ref m_summary);

        Split split = null;

        int maxNumSplits = AbcUtils.CeilDiv(m_summary.peakIndexCount, 65000);

        if (m_splits.Count > maxNumSplits)
        {
            for (int s=maxNumSplits; s<m_splits.Count; ++s)
            {
                GameObject.DestroyImmediate(m_splits[s].host);
            }

            m_splits.RemoveRange(maxNumSplits, m_splits.Count - maxNumSplits);
        }

        m_submeshes.Clear();

        for (int s=0; s<maxNumSplits; ++s)
        {
            Transform trans = (maxNumSplits > 1 ? null : m_trans);

            if (s >= m_splits.Count)
            {
                split = new Split
                {
                    positionCache = new Vector3[0],
                    normalCache = new Vector3[0],
                    uvCache = new Vector2[0],
                    tangentCache = new Vector4[0],
                    mesh = null,
                    host = null,
                    active = true
                };

                if (trans == null)
                {
                    GameObject go = new GameObject();
                    go.name = m_trans.gameObject.name + "_split_" + s;

                    trans = go.GetComponent<Transform>();
                    trans.parent = m_trans;
                    trans.localPosition = Vector3.zero;
                    trans.localEulerAngles = Vector3.zero;
                    trans.localScale = Vector3.one;
                }

                m_splits.Add(split);
            }
            else
            {
                if (trans == null)
                {
                    trans = m_trans.FindChild(m_trans.gameObject.name + "_split_" + s);
                }

                split = m_splits[s];
            }

            Mesh mesh = AddMeshComponents(abcObj, trans);
            mesh.name = trans.gameObject.name;

            split.mesh = mesh;
            split.host = trans.gameObject;
        }
    }

    // in config is the one set by aiSetConfig()
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

        config.forceUpdate = (abcMaterials != null ? abcMaterials.HasFacesetsChanged() : hasFacesets);
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

        if (m_submeshes.Count == 0)
        {
            AbcVerboseLog("AlembicMesh.AbcSampleUpdated: No submesh created yet, force topology update");
            topologyChanged = true;
        }

        AbcAPI.aiPolyMeshGetSampleSummary(sample, ref m_sampleSummary, topologyChanged);

        AbcAPI.aiMeshSampleData vertexData = default(AbcAPI.aiMeshSampleData);

        for (int s=0; s<m_sampleSummary.splitCount; ++s)
        {
            Split split = m_splits[s];

            split.clear = topologyChanged;
            split.active = true;

            int vertexCount = AbcAPI.aiPolyMeshGetVertexBufferLength(sample, s);

            Array.Resize(ref split.positionCache, vertexCount);
            vertexData.positions = Marshal.UnsafeAddrOfPinnedArrayElement(split.positionCache, 0);

            if (m_sampleSummary.hasNormals)
            {
                Array.Resize(ref split.normalCache, vertexCount);
                vertexData.normals = Marshal.UnsafeAddrOfPinnedArrayElement(split.normalCache, 0);
            }
            else
            {
                Array.Resize(ref split.normalCache, 0);
                vertexData.normals = (IntPtr)0;
            }

            if (m_sampleSummary.hasUVs)
            {
                Array.Resize(ref split.uvCache, vertexCount);
                vertexData.uvs = Marshal.UnsafeAddrOfPinnedArrayElement(split.uvCache, 0);
            }
            else
            {
                Array.Resize(ref split.uvCache, 0);
                vertexData.uvs = (IntPtr)0;
            }

            if (m_sampleSummary.hasTangents)
            {
                Array.Resize(ref split.tangentCache, vertexCount);
                vertexData.tangents = Marshal.UnsafeAddrOfPinnedArrayElement(split.tangentCache, 0);
            }
            else
            {
                Array.Resize(ref split.tangentCache, 0);
                vertexData.tangents = (IntPtr)0;
            }

            AbcAPI.aiPolyMeshFillVertexBuffer(sample, s, ref vertexData);
        }

        for (int s=m_sampleSummary.splitCount; s<m_splits.Count; ++s)
        {
            m_splits[s].active = false;
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

                submeshData.indices = Marshal.UnsafeAddrOfPinnedArrayElement(submesh.indexCache, 0);

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

        for (int s=0; s<m_splits.Count; ++s)
        {
            Split split = m_splits[s];

            if (split.active)
            {
                if (split.clear)
                {
                    split.mesh.Clear();
                }

                split.mesh.vertices = split.positionCache;
                split.mesh.normals = split.normalCache;
                split.mesh.tangents = split.tangentCache;
                split.mesh.uv = split.uvCache;

                if (split.clear)
                {
                    split.mesh.subMeshCount = split.submeshCount;

                    MeshRenderer renderer = split.host.GetComponent<MeshRenderer>();
                    
                    int nmat = renderer.sharedMaterials.Length;

                    if (nmat != split.submeshCount)
                    {
                        Material[] materials = new Material[split.submeshCount];
                        
                        for (int i=0; i<nmat; ++i)
                        {
                            materials[i] = renderer.sharedMaterials[i];
                        }

                        for (int i=nmat; i<split.submeshCount; ++i)
                        {
                            materials[i] = UnityEngine.Object.Instantiate(AbcUtils.GetDefaultMaterial());
                            materials[i].name = "Material_" + Convert.ToString(i);
                        }

                        renderer.sharedMaterials = materials;
                    }
                }

                split.host.SetActive(true);
            }
            else
            {
                split.mesh.Clear();
                split.host.SetActive(false);
            }
        }

        for (int s=0; s<m_submeshes.Count; ++s)
        {
            Submesh submesh = m_submeshes[s];

            if (submesh.update)
            {
                m_splits[submesh.splitIndex].mesh.SetIndices(submesh.indexCache, MeshTopology.Triangles, submesh.index);
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

    static Mesh AddMeshComponents(AbcAPI.aiObject abc, Transform trans)
    {
        Mesh mesh = null;
        
        MeshFilter meshFilter = trans.gameObject.GetComponent<MeshFilter>();
        
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            mesh = new Mesh();
            mesh.MarkDynamic();

            if (meshFilter == null)
            {
                meshFilter = trans.gameObject.AddComponent<MeshFilter>();
            }

            meshFilter.sharedMesh = mesh;

            MeshRenderer renderer = trans.gameObject.GetComponent<MeshRenderer>();
                
            if (renderer == null)
            {
                renderer = trans.gameObject.AddComponent<MeshRenderer>();
            }
            
            renderer.sharedMaterial = UnityEngine.Object.Instantiate(AbcUtils.GetDefaultMaterial());
            renderer.sharedMaterial.name = "Material_0";
        }
        else
        {
            mesh = meshFilter.sharedMesh;
        }

        return mesh;
    }
}
