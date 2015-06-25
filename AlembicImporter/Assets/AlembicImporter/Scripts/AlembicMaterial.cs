using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;

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
                    Debug.Log("AlembicMaterial.Update: Not enough submeshes");
                    return;
                }

                if (assigned_materials.Length != abcmesh.m_submeshes.Count)
                {
                    // should have one material for each submesh
                    Debug.Log("AlembicMaterial.Update: Submesh count doesn't match material count");
                    return;
                }

                foreach (AlembicMesh.Submesh submesh in abcmesh.m_submeshes)
                {
                    if (submesh.faceset_index != -1)
                    {
                        if (submesh.faceset_index < 0 || submesh.faceset_index >= materials.Count)
                        {
                            // invalid faceset_index, do no update material assignments at all
                            Debug.Log("AlembicMaterial.Update: Invalid faceset index");
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

    // --- Import / Export methods

    static char[] PathSep = new char[1] { '/' };
    static char[] FaceSep = new char[1] { ',' };
    static char[] RangeSep = new char[1] { '-' };

    static GameObject SearchNodeInstance(Transform parent, string name, int instNum, ref int curInst)
    {
        Transform node = parent.FindChild (name);

        if (node != null)
        {
            if (curInst == instNum)
            {
                return node.gameObject;
            }
            else
            {
                ++curInst;
            }
        }

        for (int i = 0; i < parent.childCount; ++i)
        {
            GameObject rv = SearchNodeInstance(parent.GetChild(i), name, instNum, ref curInst);

            if (rv != null)
            {
                return rv;
            }
        }

        return null;
    }
  
    static GameObject FindNode(GameObject root, string path, int instNum)
    {
        if (root == null)
        {
            return null;
        }

        string[] paths = path.Split(PathSep, StringSplitOptions.RemoveEmptyEntries);

        if (paths.Length == 0)
        {
            return null;
        }

        bool isFullPath = path.StartsWith("/");

        if (isFullPath)
        {
            int curPath = 0;

            Transform curNode = root.transform;

            while (curNode != null && curPath < paths.Length)
            {
                Transform child = curNode.FindChild(paths[curPath]);

                if (child == null)
                {
                    Debug.Log("Object \"" + curNode.name + "\" has no child named \"" + paths[curPath] + "\"");
                    curNode = null;
                    break;
                }
                else
                {
                    curNode = child;
                    ++curPath;
                }
            }

            if (curNode == null)
            {
                Debug.Log("Failed to find object \"" + path + "\"");
                return null;
            }
            else
            {
                return curNode.gameObject;
            }
        }
        else
        {
            string name = path;
            int curInst = 0;

            int idx = name.LastIndexOf("/");
            if (idx >= 0 && idx < path.Length)
            {
                name = name.Substring(idx + 1);
            }

            if (instNum < 0)
            {
                instNum = 0;
            }

            return SearchNodeInstance(root.transform, name, instNum, ref curInst);
        }
    }
  
    static Material GetMaterial(string name, string matfolder)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:material", new string[1] { matfolder });

        if (guids.Length == 1)
        {
            return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]), typeof(Material)) as Material;
        }
        else
        {
            return null;
        }
    }

    public static void Import(string xmlPath, GameObject root, string materialFolder)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(xmlPath);
        
        XmlNode xmlRoot = doc.DocumentElement;
        
        XmlNodeList nodes = xmlRoot.SelectNodes("/assignments/node");

        Dictionary<GameObject, List<AlembicMaterial.Assignment> > all_assignments = new Dictionary<GameObject, List<AlembicMaterial.Assignment> >();

        foreach (XmlNode node in nodes)
        {
            string path = node.Attributes["path"].Value;

            XmlNodeList shaders = node.SelectNodes("shader");

            foreach (XmlNode shader in shaders)
            {
                XmlAttribute name = shader.Attributes["name"];
                XmlAttribute inst = shader.Attributes["instance"];

                if (name == null)
                {
                    continue;
                }

                int instNum = (inst == null ? 0 : Convert.ToInt32(inst.Value));

                GameObject target = FindNode(root, path, instNum);
                
                if (target == null)
                {
                    continue;
                }
                
                MeshFilter mesh = target.GetComponent<MeshFilter>();

                if (mesh == null)
                {
                    continue;
                }

                Renderer renderer = target.GetComponent<Renderer>();

                if (renderer == null)
                {
                    continue;
                }

                List<AlembicMaterial.Assignment> assignments;

                if (!all_assignments.ContainsKey(target))
                {
                    assignments = new List<AlembicMaterial.Assignment>();
                    all_assignments.Add(target, assignments);
                }
                else
                {
                    assignments = all_assignments[target];
                }

                Material material = GetMaterial(name.Value, materialFolder);

                if (material == null)
                {
                    material = new Material(Shader.Find("Standard"));

                    material.color = new Color(UnityEngine.Random.value,
                                               UnityEngine.Random.value,
                                               UnityEngine.Random.value);
                    
                    AssetDatabase.CreateAsset(material, materialFolder + "/" + name.Value + ".mat");
                }

                // Get or create material assignment
                bool newlyAssigned = false;
                AlembicMaterial.Assignment a = null;
                
                if (a == null)
                {
                    a = new AlembicMaterial.Assignment();
                    a.material = material;
                    a.faces = new List<int>();

                    assignments.Add(a);

                    newlyAssigned = true;
                }

                string faceset = shader.InnerText;
                faceset.Trim();

                if (faceset.Length > 0 && (newlyAssigned || a.faces.Count > 0))
                {
                    string[] items = faceset.Split(FaceSep, StringSplitOptions.RemoveEmptyEntries);

                    for (int i=0; i<items.Length; ++i)
                    {
                        string[] rng = items[i].Split(RangeSep, StringSplitOptions.RemoveEmptyEntries);

                        if (rng.Length == 1)
                        {
                            a.faces.Add(Convert.ToInt32(rng[0]));
                        }
                        else if (rng.Length == 2)
                        {
                            int j0 = Convert.ToInt32(rng[0]);
                            int j1 = Convert.ToInt32(rng[1]);

                            for (int j=j0; j<=j1; ++j)
                            {
                                a.faces.Add(j);
                            }
                        }
                    }

                    if (!newlyAssigned)
                    {
                        a.faces = new List<int>(new HashSet<int>(a.faces));
                    }
                }
                else if (faceset.Length == 0 && a.faces.Count > 0)
                {
                    // Shader assgined to whole object, remove any face level assignments
                    a.faces.Clear();
                }
            }
        }

        // Update AlembicMaterial components
        foreach (KeyValuePair<GameObject, List<AlembicMaterial.Assignment> > pair in all_assignments)
        {
            AlembicMaterial abcmaterial = pair.Key.GetComponent<AlembicMaterial>();

            if (abcmaterial == null)
            {
                abcmaterial = pair.Key.AddComponent<AlembicMaterial>();
            }

            abcmaterial.UpdateAssignments(pair.Value);
        }

        // Force refresh
        AlembicStream abcstream = root.GetComponent<AlembicStream>();
            
        if (abcstream != null)
        {
            abcstream.m_force_refresh = true;
        }
    }

    public static void Export(string xmlPath, GameObject root)
    {
        // Not Yet Implemented
        Debug.Log("Material assignment export not yet implemented");
    }
}
