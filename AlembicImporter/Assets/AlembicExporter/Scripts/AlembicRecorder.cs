using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public abstract class AlembicCustomRecorder : MonoBehaviour
{
    public abstract void SetParent(aeAPI.aeObject parent);
    public abstract void Capture();
}



[ExecuteInEditMode]
[AddComponentMenu("Alembic/Recorder")]
public class AlembicRecorder : MonoBehaviour
{
    #region impl

    public static void CaptureTransform(aeAPI.aeObject abc, Transform trans)
    {
        aeAPI.aeXFormSampleData data;
        data.inherits = false;
        data.translation = trans.position;
        data.scale = trans.lossyScale;
        trans.rotation.ToAngleAxis(out data.rotation_angle, out data.rotation_axis);
        aeAPI.aeXFormWriteSample(abc, ref data);
    }

    public static void CaptureCamera(aeAPI.aeObject abc, Camera cam)
    {
        // todo
    }

    public static void CaptureMesh(aeAPI.aeObject abc, Mesh mesh)
    {
        aeAPI.aePolyMeshSampleData data = new aeAPI.aePolyMeshSampleData();
        var indices = mesh.GetIndices(0); // todo: record all submeshes
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var uvs = mesh.uv;
        data.indices = Marshal.UnsafeAddrOfPinnedArrayElement(indices, 0);
        data.positions = Marshal.UnsafeAddrOfPinnedArrayElement(vertices, 0);
        if(normals != null)
        {
            data.normals = Marshal.UnsafeAddrOfPinnedArrayElement(normals, 0);
        }
        if(uvs != null)
        {
            data.uvs = Marshal.UnsafeAddrOfPinnedArrayElement(uvs, 0);
        }
        data.vertex_count = vertices.Length;
        data.index_count = indices.Length;

        aeAPI.aePolyMeshWriteSample(abc, ref data);
    }


    public abstract class Recorder
    {
        public abstract void Capture();
    }

    public class TransformRecorder : Recorder
    {
        Transform m_target;
        aeAPI.aeObject m_abc;

        public TransformRecorder(Transform target, aeAPI.aeObject abc)
        {
            m_target = target;
            m_abc = abc;
        }

        public override void Capture()
        {
            if (m_target != null)
            {
                CaptureTransform(m_abc, m_target);
            }
        }
    }

    public class CameraRecorder : Recorder
    {
        Camera m_target;
        aeAPI.aeObject m_abc;

        public CameraRecorder(Camera target, aeAPI.aeObject abc)
        {
            m_target = target;
            m_abc = abc;
        }

        public override void Capture()
        {
            if (m_target != null)
            {
                CaptureCamera(m_abc, m_target);
            }
        }
    }

    public class MeshRecorder : Recorder
    {
        MeshRenderer m_target;
        aeAPI.aeObject m_abc;

        public MeshRecorder(MeshRenderer target, aeAPI.aeObject abc)
        {
            m_target = target;
            m_abc = abc;
        }

        public override void Capture()
        {
            if(m_target != null)
            {
                CaptureMesh(m_abc, m_target.GetComponent<MeshFilter>().sharedMesh);
            }
        }
    }

    public class SkinnedMeshRecorder : Recorder
    {
        SkinnedMeshRenderer m_target;
        aeAPI.aeObject m_abc;

        public SkinnedMeshRecorder(SkinnedMeshRenderer target, aeAPI.aeObject abc)
        {
            m_target = target;
            m_abc = abc;
        }

        public override void Capture()
        {
            if (m_target != null)
            {
                CaptureMesh(m_abc, m_target.sharedMesh);
            }
        }
    }

    public class CustomRecorderHandler : Recorder
    {
        AlembicCustomRecorder m_target;

        public CustomRecorderHandler(AlembicCustomRecorder target)
        {
            m_target = target;
        }

        public override void Capture()
        {
            if (m_target != null)
            {
                m_target.Capture();
            }
        }
    }

    #endregion


    public enum Scope
    {
        EntireScene,
        CurrentBranch,
    }

    public string m_path;
    public aeAPI.aeConfig m_conf = aeAPI.aeConfig.default_value;

    public Scope m_scope = Scope.EntireScene;
    public bool m_preserveTreeStructure = false;

    public bool m_captureMeshRenderer = true;
    public bool m_captureSkinnedMeshRenderer = true;
    public bool m_captureCamera = true;
    public bool m_captureCustomRecorder = true;

    aeAPI.aeContext m_ctx;
    List<Recorder> m_recorders = new List<Recorder>();
    bool m_recording;
    float m_time;


    public bool isRecording { get { return m_recording; } }
    public float time { get { return m_time; } }


    T[] GetTargets<T>() where T : Component
    {
        if(m_scope == Scope.CurrentBranch)
        {
            return GetComponentsInChildren<T>();
        }
        else
        {
            return FindObjectsOfType<T>();
        }
    }

    public bool BeginRecording()
    {
        if(m_recording) {
            Debug.Log("already recording");
            return false;
        }

        m_ctx = aeAPI.aeCreateContext(ref m_conf);
        if(m_ctx.ptr == IntPtr.Zero) {
            Debug.Log("aeCreateContext() failed");
            return false;
        }
        if(!aeAPI.aeOpenArchive(m_ctx, m_path))
        {
            Debug.Log("aeOpenArchive() failed");
            aeAPI.aeDestroyContext(m_ctx);
            m_ctx = new aeAPI.aeContext();
            return false;
        }


        var top = aeAPI.aeGetTopObject(m_ctx);

        if (m_captureCamera)
        {
            foreach(var target in GetTargets<Camera>())
            {
                var trans_obj = aeAPI.aeNewXForm(top, target.name + "_trans");
                var trans_rec = new TransformRecorder(target.GetComponent<Transform>(), trans_obj);
                m_recorders.Add(trans_rec);

                var cam_obj = aeAPI.aeNewCamera(trans_obj, target.name);
                var cam_rec = new CameraRecorder(target, cam_obj);
                m_recorders.Add(cam_rec);
            }
        }

        if (m_captureMeshRenderer)
        {
            foreach (var target in GetTargets<MeshRenderer>())
            {
                var trans_obj = aeAPI.aeNewXForm(top, target.name + "_trans");
                var trans_rec = new TransformRecorder(target.GetComponent<Transform>(), trans_obj);
                m_recorders.Add(trans_rec);

                var mesh_obj = aeAPI.aeNewPolyMesh(trans_obj, target.name);
                var mesh_rec = new MeshRecorder(target, mesh_obj);
                m_recorders.Add(mesh_rec);
            }
        }

        if (m_captureSkinnedMeshRenderer)
        {
            foreach (var target in GetTargets<SkinnedMeshRenderer>())
            {
                var trans_obj = aeAPI.aeNewXForm(top, target.name + "_trans");
                var trans_rec = new TransformRecorder(target.GetComponent<Transform>(), trans_obj);
                m_recorders.Add(trans_rec);

                var mesh_obj = aeAPI.aeNewPolyMesh(trans_obj, target.name);
                var mesh_rec = new SkinnedMeshRecorder(target, mesh_obj);
                m_recorders.Add(mesh_rec);
            }
        }

        if (m_captureCustomRecorder)
        {
            foreach (var target in GetTargets<AlembicCustomRecorder>())
            {
                target.SetParent(top);
                var mesh_rec = new CustomRecorderHandler(target);
                m_recorders.Add(mesh_rec);
            }
        }

        m_recording = true;
        m_time = 0.0f;

        Debug.Log("AlembicRecorder: start " + m_path);
        return true;
    }

    public void EndRecording()
    {
        if (!m_recording) { return; }

        m_recorders.Clear();
        aeAPI.aeDestroyContext(m_ctx);
        m_ctx = new aeAPI.aeContext();
        m_recording = false;

        Debug.Log("AlembicRecorder: end: " + m_path);
    }

    public void OneShot()
    {
        if(BeginRecording())
        {
            aeAPI.aeSetTime(m_ctx, 0.0f);
            foreach (var recorder in m_recorders)
            {
                recorder.Capture();
            }
            EndRecording();
        }
    }

    IEnumerator ProcessRecording()
    {
        yield return new WaitForEndOfFrame();
        if(!m_recording) { yield break; }

        aeAPI.aeSetTime(m_ctx, m_time);
        foreach(var recorder in m_recorders) {
            recorder.Capture();
        }
    }


    void Reset()
    {
        if(m_path == null || m_path == "")
        {
            m_path = gameObject.name + ".abc";
        }
    }

    void Update()
    {
        if(m_recording)
        {
            m_time += Time.deltaTime;
            StartCoroutine(ProcessRecording());
        }
    }

    void OnDisable()
    {
        EndRecording();
    }
}
