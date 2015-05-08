using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AlembicImporter
{
    public delegate void uaiNodeEnumerator(int ctx, IntPtr obj);

    [DllImport ("UnityAlembicImporter")] public static extern int       uaiCreateContext();
    [DllImport ("UnityAlembicImporter")] public static extern void      uaiDestroyContext(int ctx);
    
    [DllImport ("UnityAlembicImporter")] public static extern bool      uaiLoad(int ctx, string path);
    [DllImport ("UnityAlembicImporter")] public static extern IntPtr    uaiGetTopObject(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern void      uaiEnumerateChild(int ctx, IntPtr obj, uaiNodeEnumerator e);
    [DllImport ("UnityAlembicImporter")] public static extern void      uaiSetCurrentObject(int ctx, IntPtr obj);
    
    [DllImport ("UnityAlembicImporter")] public static extern IntPtr    uaiGetName(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern IntPtr    uaiGetFullName(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern uint      uaiGetNumChildren(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern bool      uaiHasXForm(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern bool      uaiIsPolyMesh(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern Vector3   uaiGetPosition(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern Vector3   uaiGetRotation(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern Vector3   uaiGetScale(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern Matrix4x4 uaiGetMatrix(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern uint      uaiGetVertexCount(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern uint      uaiGetIndexCount(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern void      uaiCopyMeshData(int ctx, IntPtr vertices, IntPtr indices);


    [MenuItem ("AlembicImporter/Import")]
    static void Test()
    {
        var path = EditorUtility.OpenFilePanel("", "", "abc");

        int ctx = uaiCreateContext();
        if (!uaiLoad(ctx, path))
        {
            Debug.Log("uaiLoad(\"" + path + "\") failed");
        }
        else
        {
            uaiEnumerateChild(ctx, uaiGetTopObject(ctx), TestCallback);
        }
        uaiDestroyContext(ctx);
    }

    static void TestCallback(int ctx, IntPtr node)
    {
        Debug.Log("Node: " + Marshal.PtrToStringAnsi(uaiGetFullName(ctx)) + " (" + (uaiHasXForm(ctx) ? "x" : "") + (uaiIsPolyMesh(ctx) ? "p" : "") + ")");
        uaiEnumerateChild(ctx, node, TestCallback);
    }
}
