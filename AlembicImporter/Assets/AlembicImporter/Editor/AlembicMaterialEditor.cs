using System;
using System.IO;
using UnityEngine;
using UnityEditor;

class AlembicMaterialEditor : EditorWindow
{
    string xmlPath = "";
    string materialFolder = "";
    GameObject assetRoot = null;

    [MenuItem ("Assets/Import Alembic Materials")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow (typeof(AlembicMaterialEditor));
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

    string MakeDataRelativePath(string path)
    {
        if (path.Length > 0)
        {
            Uri baseUri = new Uri(Application.dataPath + "/");
            return baseUri.MakeRelativeUri(new Uri(path)).ToString();
        }
        else
        {
            return path;
        }
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
            xmlPath = MakeDataRelativePath(EditorUtility.OpenFilePanel("Select Material Assignment File", startFolder, "xml"));

            if (xmlPath.Length > 0)
            {
                materialFolder = Path.GetDirectoryName(xmlPath);
            }
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
            materialFolder = MakeDataRelativePath(EditorUtility.OpenFolderPanel("Select Materials Folder", startFolder, ""));
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
            AlembicMaterial.Import(Application.dataPath + "/" + xmlPath, assetRoot, "Assets/" + materialFolder); //relMatFolder);
        }

        if (GUI.Button(new Rect(r.x + bw + hs, r.y, bw, r.height), "Export"))
        {
            AlembicMaterial.Export(Application.dataPath + "/" + xmlPath, assetRoot);
        }
    }
}
