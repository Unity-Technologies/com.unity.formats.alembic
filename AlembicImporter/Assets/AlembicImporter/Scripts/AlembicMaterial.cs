using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[ExecuteInEditMode]
public class AlembicMaterial : MonoBehaviour
{
    public class Assignment
    {
        public Material material;
        public List<int> faces;
    }

    public class Facesets
    {
        public int[] face_counts;
        public int[] face_indices;
    }

    bool dirty = false;
    List<Material> materials = new List<Material>();
    Facesets facesets_cache = new Facesets { face_counts = new int[0], face_indices = new int[0] };
    
    void Start()
    {
    }
    
    void Update()
    {
        if (materials.Count > 0)
        {
            AlembicMesh abcmesh = gameObject.GetComponent<AlembicMesh>();

            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();

            if (abcmesh != null && renderer != null)
            {
                bool changed = false;
                int submesh_index = 0;
                Material[] assigned_materials = renderer.sharedMaterials;

                if (abcmesh.m_submeshes.Count < materials.Count)
                {
                    // should have at least materials.Count submeshes
                    return;
                }

                if (assigned_materials.Length != abcmesh.m_submeshes.Count)
                {
                    // should have one material for each submesh
                    return;
                }

                foreach (AlembicMesh.Submesh submesh in abcmesh.m_submeshes)
                {
                    if (submesh.faceset_index != -1)
                    {
                        if (submesh.faceset_index < 0 || submesh.faceset_index >= materials.Count)
                        {
                            // invalid faceset_index, do no update material assignments at all
                            return;
                        }

                        assigned_materials[submesh_index] = materials[submesh.faceset_index];
                        changed = true;
                    }
                    else
                    {
                        // should I reset to default material or leave it as it is
                    }

                    ++submesh_index;
                }

                if (changed)
                {
                    renderer.sharedMaterials = assigned_materials;
                }
            }

            materials.Clear();
        }
    }

    public int GetFacesetsCount()
    {
        return facesets_cache.face_counts.Length;
    }

    public void GetFacesets(ref AlembicImporter.aiFacesets facesets)
    {
        facesets.count = facesets_cache.face_counts.Length;
        facesets.face_counts = Marshal.UnsafeAddrOfPinnedArrayElement(facesets_cache.face_counts, 0);
        facesets.face_indices = Marshal.UnsafeAddrOfPinnedArrayElement(facesets_cache.face_indices, 0);
    }

    public bool HasFacesetsChanged()
    {
        return dirty;
    }

    public void AknowledgeFacesetsChanges()
    {
        dirty = false;
    }

    public void UpdateAssignments(List<Assignment> assignments)
    {
        int count = 0;
        int indices_count = 0;

        // keep list of materials for next update
        materials.Clear();

        if (facesets_cache.face_counts.Length < assignments.Count)
        {
            Array.Resize(ref facesets_cache.face_counts, assignments.Count);
            dirty = true;
        }

        for (int i=0; i<assignments.Count; ++i)
        {
            materials.Add(assignments[i].material);

            int face_count = assignments[i].faces.Count;

            dirty = dirty || (facesets_cache.face_counts[count] != face_count);
            
            facesets_cache.face_counts[count++] = face_count;

            if (facesets_cache.face_indices.Length < (indices_count + face_count))
            {
                Array.Resize(ref facesets_cache.face_indices, indices_count + face_count);
                dirty = true;
            }

            for (int j=0; j<face_count; ++j, ++indices_count)
            {
                int face_index = assignments[i].faces[j];

                dirty = dirty || (facesets_cache.face_indices[indices_count] != face_index);
                
                facesets_cache.face_indices[indices_count] = face_index;
            }
        }
    }
}
