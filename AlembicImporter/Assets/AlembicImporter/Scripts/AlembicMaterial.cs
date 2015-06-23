using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[ExecuteInEditMode]
public class AlembicMaterial : MonoBehaviour
{
    [Serializable]
    public class Assignment
    {
        public Material material;
        // a size of 0 means 'all faces'
        public List<int> faces;
    }

    // structure to hold assignment face sets in a C friendly form
    public class Facesets
    {
        public int[] face_counts;
        public int[] face_indices;
    }

    // assignments visible in inspector
    public List<Assignment> assignments = new List<Assignment>();
    
    // internal cache
    Facesets facesets_cache = new Facesets { face_counts = new int[0], face_indices = new int[0] };

    void Start()
    {
    }
    
    void Update()
    {
    }

    public bool UpdateCache(ref AlembicImporter.aiFacesets facesets)
    {
        bool dirty = false;
        int count = 0;
        int indices_count = 0;

        // resize only if necessary
        if (facesets_cache.face_counts.Length < assignments.Count)
        {
            Array.Resize(ref facesets_cache.face_counts, assignments.Count);
            dirty = true;
        }

        for (int i=0; i<assignments.Count; ++i)
        {
            int face_count = assignments[i].faces.Count;

            dirty = dirty || (facesets_cache.face_counts[count] != face_count);
            
            facesets_cache.face_counts[count++] = face_count;

            // resize only if necessary
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

        facesets.count = count;
        facesets.face_counts = Marshal.UnsafeAddrOfPinnedArrayElement(facesets_cache.face_counts, 0);
        facesets.face_indices = Marshal.UnsafeAddrOfPinnedArrayElement(facesets_cache.face_indices, 0);

        return dirty;
    }
}
