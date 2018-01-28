using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace UTJ.Alembic
{
    public class AlembicMesh : AlembicElement
    {
        public class Split
        {
            public AbcAPI.aiMeshSplitSummary summary;
            public PinnedList<Vector3> pointCache = new PinnedList<Vector3>();
            public PinnedList<Vector3> velocitiesCache = new PinnedList<Vector3>();
            public PinnedList<Vector3> normalCache = new PinnedList<Vector3>();
            public PinnedList<Vector4> tangentCache = new PinnedList<Vector4>();
            public PinnedList<Vector2> uv0Cache = new PinnedList<Vector2>();
            public PinnedList<Vector2> uv1Cache = new PinnedList<Vector2>();
            public PinnedList<Color> colorCache = new PinnedList<Color>();

            public List<Submesh> submeshes = new List<Submesh>();
            public Mesh mesh;
            public GameObject host;
            public bool clear = true;
            public bool active = true;

            public Vector3 center;
            public Vector3 size;
        }

        public class Submesh
        {
            public PinnedList<int> indexCache = new PinnedList<int>();
            public bool update = true;
        }

        public List<Split> m_splits = new List<Split>();
        public AbcAPI.aiMeshSummary m_summary;
        public AbcAPI.aiMeshSampleSummary m_sampleSummary;
        bool m_freshSetup = false;
        

        void UpdateSplits(int numSplits)
        {
            Split split = null;

            if (m_summary.topologyVariance == AbcAPI.aiTopologyVariance.Heterogeneous || numSplits > 1)
            {
                for (int i=0; i<numSplits; ++i)
                {
                    if (i >= m_splits.Count)
                    {
                        m_splits.Add(new Split());
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
                    split = new Split {
                        host = abcTreeNode.linkedGameObj,
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

        public override void AbcSetup(AbcAPI.aiObject abcObj, AbcAPI.aiSchema abcSchema)
        {
            base.AbcSetup(abcObj, abcSchema);

            AbcAPI.aiPolyMeshGetSummary(abcSchema, ref m_summary);

            m_freshSetup = true;
        }

        public override void AbcGetConfig(ref AbcAPI.aiConfig config)
        {
            // if 'forceUpdate' is set true, even if alembic sample data do not change at all
            // AbcSampleUpdated will still be called (topologyChanged will be false)

            config.forceUpdate = m_freshSetup;
        }

        public override void AbcSampleUpdated(AbcAPI.aiSample sample)
        {
            var vertexData = default(AbcAPI.aiPolyMeshData);

            AbcAPI.aiPolyMeshGetSampleSummary(sample, ref m_sampleSummary);
            UpdateSplits(m_sampleSummary.splitCount);

            bool topologyChanged = m_sampleSummary.topologyChanged;
            if (m_freshSetup)
            {
                topologyChanged = true;
                m_freshSetup = false;
            }

            for (int spi = 0; spi < m_sampleSummary.splitCount; ++spi)
            {
                var split = m_splits[spi];
                AbcAPI.aiPolyMeshGetSplitSummary(sample, spi, ref split.summary);

                split.clear = topologyChanged;
                split.active = true;

                int vertexCount = split.summary.vertexCount;

                split.pointCache.Resize(vertexCount);
                vertexData.positions = split.pointCache;

                if (m_summary.hasVelocities)
                    split.velocitiesCache.Resize(vertexCount);
                else
                    split.velocitiesCache.Resize(0);
                vertexData.velocities = split.velocitiesCache;

                if (m_summary.hasNormals)
                    split.normalCache.Resize(vertexCount);
                else
                    split.normalCache.Resize(0);
                vertexData.normals = split.normalCache;

                if (m_summary.hasTangents)
                    split.tangentCache.Resize(vertexCount);
                else
                    split.tangentCache.Resize(0);
                vertexData.tangents = split.tangentCache;

                if (m_summary.hasUV0)
                    split.uv0Cache.Resize(vertexCount);
                else
                    split.uv0Cache.Resize(0);
                vertexData.uv0 = split.uv0Cache;

                if (m_summary.hasUV1)
                    split.uv1Cache.Resize(vertexCount);
                else
                    split.uv1Cache.Resize(0);
                vertexData.uv1 = split.uv1Cache;

                if (m_summary.hasColors)
                    split.colorCache.Resize(vertexCount);
                else
                    split.colorCache.Resize(0);
                vertexData.colors = split.colorCache;

                AbcAPI.aiPolyMeshFillVertexBuffer(sample, spi, ref vertexData);

                split.center = vertexData.center;
                split.size = vertexData.size;
            }

            if (topologyChanged)
            {
                var submeshSummary = new AbcAPI.aiSubmeshSummary();
                var submeshData = new AbcAPI.aiSubmeshData();
                for (int spi = 0; spi < m_sampleSummary.splitCount; ++spi)
                {
                    var split = m_splits[spi];
                    int submeshCount = split.summary.submeshCount;

                    if (split.submeshes.Count > submeshCount)
                        split.submeshes.RemoveRange(submeshCount, split.submeshes.Count - submeshCount);
                    while (split.submeshes.Count < submeshCount)
                        split.submeshes.Add(new Submesh());

                    for (int smi = 0; smi < submeshCount; ++smi)
                    {
                        var submesh = split.submeshes[smi];
                        AbcAPI.aiPolyMeshGetSubmeshSummary(sample, spi, smi, ref submeshSummary);
                        submesh.indexCache.Resize(submeshSummary.indexCount);
                        submeshData.indices = submesh.indexCache;
                        AbcAPI.aiPolyMeshFillSubmeshIndices(sample, spi, smi, ref submeshData);
                    }
                }
            }
            else
            {
                for (int spi = 0; spi < m_sampleSummary.splitCount; ++spi)
                    for (int smi = 0; smi < m_splits[spi].summary.submeshCount; ++smi)
                        m_splits[spi].submeshes[smi].update = false;
            }
        }

        public override void AbcUpdate()
        {
            if (!m_abcSchema.dirty)
                return;

            AbcSampleUpdated(m_abcSchema.sample);

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
                            string name = abcTreeNode.linkedGameObj.name + "_split_" + s;

                            Transform trans = abcTreeNode.linkedGameObj.transform.Find(name);

                            if (trans == null)
                            {
                                GameObject go = new GameObject();
                                go.name = name;

                                trans = go.GetComponent<Transform>();
                                trans.parent = abcTreeNode.linkedGameObj.transform;
                                trans.localPosition = Vector3.zero;
                                trans.localEulerAngles = Vector3.zero;
                                trans.localScale = Vector3.one;
                            }

                            split.host = trans.gameObject;
                        }
                        else
                        {
                            split.host = abcTreeNode.linkedGameObj;
                        }
                    }

                    // Feshly created splits may not have their mesh set yet
                    if (split.mesh == null)
                        split.mesh = AddMeshComponents(split.host);
                    if (split.clear)
                        split.mesh.Clear();

                    split.mesh.SetVertices(split.pointCache.List);
                    if (split.normalCache.Count > 0)
                        split.mesh.SetNormals(split.normalCache.List);
                    if (split.tangentCache.Count > 0)
                        split.mesh.SetTangents(split.tangentCache.List);
                    if (split.uv0Cache.Count > 0)
                        split.mesh.SetUVs(0, split.uv0Cache.List);
                    if (split.uv1Cache.Count > 0)
                        split.mesh.SetUVs(1, split.uv1Cache.List);
                    if (split.velocitiesCache.Count > 0)
                        split.mesh.SetUVs(3, split.velocitiesCache.List);
                    if (split.colorCache.Count > 0)
                        split.mesh.SetColors(split.colorCache.List);
                    // update the bounds
                    split.mesh.bounds = new Bounds(split.center, split.size);

                    if (split.clear)
                    {
                        int submeshCount = split.summary.submeshCount;
                        split.mesh.subMeshCount = submeshCount;
                        MeshRenderer renderer = split.host.GetComponent<MeshRenderer>();
                        Material[] currentMaterials = renderer.sharedMaterials;
                        int nmat = currentMaterials.Length;
                        if (nmat != submeshCount)
                        {
                            Material[] materials = new Material[submeshCount];
                            int copyTo = (nmat < submeshCount ? nmat : submeshCount);
                            for (int i=0; i<copyTo; ++i)
                            {
                                materials[i] = currentMaterials[i];
                            }
#if UNITY_EDITOR
                            for (int i = copyTo; i < submeshCount; ++i)
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

            for (int spi = 0; spi < m_sampleSummary.splitCount; ++spi)
            {
                var split = m_splits[spi];
                for (int smi = 0; smi < m_splits[spi].summary.submeshCount; ++smi)
                {
                    var submesh = split.submeshes[smi];
                    split.mesh.SetTriangles(submesh.indexCache.List, smi);
                }
            }
        }

        Mesh AddMeshComponents(GameObject gameObject)
        {
            Mesh mesh = null;
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            bool hasMesh = meshFilter != null
                           && meshFilter.sharedMesh != null
                           && meshFilter.sharedMesh.name.IndexOf("dyn: ") == 0;

            if( !hasMesh)
            {
                mesh = new Mesh {name = "dyn: " + gameObject.name};
#if UNITY_2017_3_OR_NEWER
                mesh.indexFormat = IndexFormat.UInt32;
#endif
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

                var mat = gameObject.transform.parent.GetComponentInChildren<MeshRenderer>().sharedMaterial;
    #if UNITY_EDITOR
                if (mat == null)
                {
                    mat = UnityEngine.Object.Instantiate(AbcUtils.GetDefaultMaterial());
                    mat.name = "Material_0";    
                }
    #endif
                renderer.sharedMaterial = mat;
            }
            else
            {
                mesh = UnityEngine.Object.Instantiate(meshFilter.sharedMesh);
                meshFilter.sharedMesh = mesh;
                mesh.name = "dyn: " + gameObject.name;
            }

            return mesh;
        }
    }
}
