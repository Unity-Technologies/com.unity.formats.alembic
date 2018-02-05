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
            public PinnedList<Vector3> points = new PinnedList<Vector3>();
            public PinnedList<Vector3> velocities = new PinnedList<Vector3>();
            public PinnedList<Vector3> normals = new PinnedList<Vector3>();
            public PinnedList<Vector4> tangents = new PinnedList<Vector4>();
            public PinnedList<Vector2> uv0 = new PinnedList<Vector2>();
            public PinnedList<Vector2> uv1 = new PinnedList<Vector2>();
            public PinnedList<Color> colors = new PinnedList<Color>();

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

        aiPolyMesh m_abcSchema;
        aiMeshSummary m_summary;
        aiMeshSampleSummary m_sampleSummary;
        PinnedList<aiMeshSplitSummary> m_splitSummaries = new PinnedList<aiMeshSplitSummary>();
        PinnedList<aiSubmeshSummary> m_submeshSummaries = new PinnedList<aiSubmeshSummary>();
        PinnedList<aiPolyMeshData> m_splitData = new PinnedList<aiPolyMeshData>();
        PinnedList<aiSubmeshData> m_submeshData = new PinnedList<aiSubmeshData>();

        List<Split> m_splits = new List<Split>();
        List<Submesh> m_submeshes = new List<Submesh>();
        bool m_freshSetup = false;


        public aiMeshSummary summary { get { return m_summary; } }
        public aiMeshSampleSummary sampleSummary { get { return m_sampleSummary; } }


        void UpdateSplits(int numSplits)
        {
            Split split = null;

            if (m_summary.topologyVariance == aiTopologyVariance.Heterogeneous || numSplits > 1)
            {
                for (int i = 0; i < numSplits; ++i)
                {
                    if (i >= m_splits.Count)
                        m_splits.Add(new Split());
                    else
                        m_splits[i].active = true;
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

            for (int i = numSplits; i < m_splits.Count; ++i)
                m_splits[i].active = false;
        }

        public override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup(abcObj, abcSchema);
            m_abcSchema = (aiPolyMesh)abcSchema;

            m_abcSchema.GetSummary(ref m_summary);
            m_freshSetup = true;
        }

        public override void AbcPrepareSample()
        {
            if(m_freshSetup)
            {
                m_freshSetup = false;
                m_abcSchema.schema.MarkForceUpdate();
            }
        }

        public override void AbcSyncDataBegin()
        {
            m_abcSchema.Sync();
            if (!m_abcSchema.schema.isDataUpdated)
                return;

            var sample = m_abcSchema.sample;

            sample.GetSummary(ref m_sampleSummary);
            int splitCount = m_sampleSummary.splitCount;
            int submeshCount = m_sampleSummary.submeshCount;

            m_splitSummaries.ResizeDiscard(splitCount);
            m_splitData.ResizeDiscard(splitCount);
            m_submeshSummaries.ResizeDiscard(submeshCount);
            m_submeshData.ResizeDiscard(submeshCount);

            sample.GetSplitSummaries(m_splitSummaries);
            sample.GetSubmeshSummaries(m_submeshSummaries);

            UpdateSplits(splitCount);

            bool topologyChanged = m_sampleSummary.topologyChanged;

            // setup buffers
            var vertexData = default(aiPolyMeshData);
            for (int spi = 0; spi < splitCount; ++spi)
            {
                var split = m_splits[spi];

                split.clear = topologyChanged;
                split.active = true;

                int vertexCount = m_splitSummaries[spi].vertexCount;

                if (!m_summary.constantPoints || topologyChanged)
                    split.points.ResizeDiscard(vertexCount);
                else
                    split.points.ResizeDiscard(0);
                vertexData.positions = split.points;

                if (m_summary.hasVelocities && (!m_summary.constantVelocities || topologyChanged))
                    split.velocities.ResizeDiscard(vertexCount);
                else
                    split.velocities.ResizeDiscard(0);
                vertexData.velocities = split.velocities;

                if (m_summary.hasNormals && (!m_summary.constantNormals || topologyChanged))
                    split.normals.ResizeDiscard(vertexCount);
                else
                    split.normals.ResizeDiscard(0);
                vertexData.normals = split.normals;

                if (m_summary.hasTangents && (!m_summary.constantTangents || topologyChanged))
                    split.tangents.ResizeDiscard(vertexCount);
                else
                    split.tangents.ResizeDiscard(0);
                vertexData.tangents = split.tangents;

                if (m_summary.hasUV0 && (!m_summary.constantUV0 || topologyChanged))
                    split.uv0.ResizeDiscard(vertexCount);
                else
                    split.uv0.ResizeDiscard(0);
                vertexData.uv0 = split.uv0;

                if (m_summary.hasUV1 && (!m_summary.constantUV1 || topologyChanged))
                    split.uv1.ResizeDiscard(vertexCount);
                else
                    split.uv1.ResizeDiscard(0);
                vertexData.uv1 = split.uv1;

                if (m_summary.hasColors && (!m_summary.constantColors || topologyChanged))
                    split.colors.ResizeDiscard(vertexCount);
                else
                    split.colors.ResizeDiscard(0);
                vertexData.colors = split.colors;

                m_splitData[spi] = vertexData;
            }

            {
                if (m_submeshes.Count > submeshCount)
                    m_submeshes.RemoveRange(submeshCount, m_submeshes.Count - submeshCount);
                while (m_submeshes.Count < submeshCount)
                    m_submeshes.Add(new Submesh());

                var submeshData = default(aiSubmeshData);
                for (int smi = 0; smi < submeshCount; ++smi)
                {
                    var submesh = m_submeshes[smi];
                    m_submeshes[smi].update = true;
                    submesh.indexCache.ResizeDiscard(m_submeshSummaries[smi].indexCount);
                    submeshData.indices = submesh.indexCache;
                    m_submeshData[smi] = submeshData;
                }
            }

            // kick async copy
            sample.FillVertexBuffer(m_splitData, m_submeshData);
        }

        public override void AbcSyncDataEnd()
        {
#if UNITY_EDITOR
            for (int s = 0; s < m_splits.Count; ++s)
            {
                var split = m_splits[s];
                var mf = split.host.GetComponent<MeshFilter>();
                if (mf != null)
                    mf.sharedMesh = split.mesh;
            }
#endif

            if (!m_abcSchema.schema.isDataUpdated)
                return;

            // wait async copy complete
            var sample = m_abcSchema.sample;
            sample.Sync();

            bool useSubObjects = (m_summary.topologyVariance == aiTopologyVariance.Heterogeneous || m_sampleSummary.splitCount > 1);

            for (int s = 0; s < m_splits.Count; ++s)
            {
                var split = m_splits[s];
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

                    if (split.points.Count > 0)
                        split.mesh.SetVertices(split.points.List);
                    if (split.normals.Count > 0)
                        split.mesh.SetNormals(split.normals.List);
                    if (split.tangents.Count > 0)
                        split.mesh.SetTangents(split.tangents.List);
                    if (split.uv0.Count > 0)
                        split.mesh.SetUVs(0, split.uv0.List);
                    if (split.uv1.Count > 0)
                        split.mesh.SetUVs(1, split.uv1.List);
                    if (split.velocities.Count > 0)
                        split.mesh.SetUVs(3, split.velocities.List);
                    if (split.colors.Count > 0)
                        split.mesh.SetColors(split.colors.List);

                    // update the bounds
                    var data = m_splitData[s];
                    split.mesh.bounds = new Bounds(data.center, data.size);

                    if (split.clear)
                    {
                        int submeshCount = m_splitSummaries[s].submeshCount;
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

            for (int smi = 0; smi < m_sampleSummary.submeshCount; ++smi)
            {
                var submesh = m_submeshes[smi];
                if (submesh.update)
                {
                    var sum = m_submeshSummaries[smi];
                    var split = m_splits[sum.splitIndex];
                    split.mesh.SetTriangles(submesh.indexCache.List, sum.submeshIndex);
                }
            }
        }

        Mesh AddMeshComponents(GameObject gameObject)
        {
            Mesh mesh = null;
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            bool hasMesh = meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh.name.IndexOf("dyn: ") == 0;

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
