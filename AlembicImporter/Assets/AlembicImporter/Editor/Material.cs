using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

class Materials : EditorWindow
{
    string xmlPath = "";
    string materialFolder = "";
    GameObject assetRoot = null;

    static char[] PathSep = new char[1] { '/' };
    static char[] FaceSep = new char[1] { ',' };
    static char[] RangeSep = new char[1] { '-' };

    //[MenuItem ("Window/Alembic/Materials")]
    [MenuItem ("Assets/Import Alembic Materials")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow (typeof(Materials));
    }

    Rect NewControlRect(bool hasLabel, float height, float hpad, float vpad)
    {
        Rect r = EditorGUILayout.GetControlRect(hasLabel, height + vpad);

        r.x += hpad;
        r.width -= 2 * hpad;
        r.y += vpad;
        r.height -= vpad;

        return r;
    }

    public void OnGUI()
    {
        float hp = 5;
        float vp = 5;
        float hs = 5;
        float rh = 16;
        float lw;
        float fw;
        float bw;
        Rect r;

        // Material assignment file controls
        r = NewControlRect(true, rh, hp, vp);
        lw = r.width * 0.33f;

        EditorGUI.PrefixLabel(new Rect(r.x, r.y, lw, r.height), new GUIContent("Assignments File"));

        bw = 26;
        fw = r.width - lw - bw - 2 * hs;
        xmlPath = EditorGUI.TextField(new Rect(r.x + lw + hs, r.y, fw, r.height), xmlPath);
        if (GUI.Button(new Rect(r.x + lw + hs + fw + hs, r.y, bw, r.height), "..."))
        {
            string startFolder = (xmlPath.Length > 0 ? Path.GetDirectoryName(xmlPath) : Application.dataPath);
            xmlPath = EditorUtility.OpenFilePanel("Select Material Assignment File", startFolder, "xml");
        }

        // Material folder controls
        r = NewControlRect(true, rh, hp, vp);
        lw = r.width * 0.33f;
        
        EditorGUI.PrefixLabel(new Rect(r.x, r.y, lw, r.height), new GUIContent("Materials Folder"));
        
        bw = 26;
        fw = r.width - lw - bw - 2 * hs;
        materialFolder = EditorGUI.TextField(new Rect(r.x + lw + hs, r.y, fw, r.height), materialFolder);
        if (GUI.Button(new Rect(r.x + lw + hs + fw + hs, r.y, bw, r.height), "..."))
        {
            string startFolder = (materialFolder.Length > 0 ? materialFolder : Application.dataPath);
            materialFolder = EditorUtility.OpenFolderPanel("Select Materials Folder", startFolder, "");
        }

        // Target asset node controls
        r = NewControlRect(true, rh, hp, vp);
        lw = r.width * 0.33f;

        EditorGUI.PrefixLabel(new Rect(r.x, r.y, lw, r.height), new GUIContent("Asset Root"));

        fw = r.width - lw - hs;
        assetRoot = (GameObject) EditorGUI.ObjectField(new Rect(r.x + lw + hs, r.y, fw, r.height), assetRoot, typeof(GameObject), true);

        // Action buttons
        r = NewControlRect(false, rh, hp, vp);
        bw = 0.5f * (r.width - hs);

        if (GUI.Button(new Rect(r.x, r.y, bw, r.height), "Import"))
        {
            Uri tmp = new Uri(Application.dataPath + "/");

            string relMatFolder = "Assets/" + tmp.MakeRelativeUri(new Uri(materialFolder)).ToString();

            Import(xmlPath, assetRoot, relMatFolder);
        }

        if (GUI.Button(new Rect(r.x + bw + hs, r.y, bw, r.height), "Export"))
        {
            Export(xmlPath, assetRoot);
        }
    }

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

    static void Import(string xmlPath, GameObject root, string materialFolder)
    {
        //AlembicMaterial[] mal = root.GetComponentsInChildren<AlembicMaterial>();
        //if (mal.Length > 0)
        //{
        //  foreach (AlembicMaterial ma in mal)
        //  {
        //    Component.DestroyImmediate(ma);
        //  }
        //}

        XmlDocument doc = new XmlDocument();
        doc.Load(xmlPath);
        
        XmlNode xmlRoot = doc.DocumentElement;
        
        XmlNodeList nodes = xmlRoot.SelectNodes("/assignments/node");

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

                AlembicMaterial aa = target.GetComponent<AlembicMaterial>();

                if (aa == null)
                {
                    aa = target.AddComponent<AlembicMaterial>();
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

                for (int i=0; i<aa.assignments.Count; ++i)
                {
                    if (aa.assignments[i].material == material)
                    {
                        a = aa.assignments[i];
                        break;
                    }
                }
                
                if (a == null)
                {
                    a = new AlembicMaterial.Assignment();
                    a.material = material;
                    a.faces = new List<int>();

                    aa.assignments.Add(a);

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
    }

    static void Export(string xmlPath, GameObject root)
    {
        // Not Yet Implemented
        Debug.Log("Material assignment export not yet implemented");
    }
}
