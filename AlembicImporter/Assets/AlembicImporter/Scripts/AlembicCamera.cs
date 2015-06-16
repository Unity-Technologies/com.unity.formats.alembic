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

[ExecuteInEditMode]
public class AlembicCamera : AlembicElement
{
    Transform m_trans;
    Camera m_camera;
    AlembicImporter.aiCameraParams m_camera_params;

    public override void AbcSetup(AlembicStream abcstream, AlembicImporter.aiObject abcobj)
    {
        base.AbcSetup(abcstream, abcobj);
        m_trans = GetComponent<Transform>();
        m_camera = GetOrAddComponent<Camera>();
    }

    public override void AbcUpdate()
    {
        base.AbcUpdate();
        var trans = m_trans;
        var cam = m_camera;
        var abc = m_abcobj;

        trans.parent.forward = -trans.parent.forward;

        AlembicImporter.aiCameraGetParams(abc, ref m_camera_params);
        cam.fieldOfView = m_camera_params.field_of_view;
        cam.nearClipPlane = m_camera_params.near_clipping_plane;
        cam.farClipPlane = m_camera_params.far_clipping_plane;

        // alembic が持つカメラパラメータを DoF のパラメータに流し込みたい…、が、
        // Standard Assets (ImageEffects) が import されていない場合エラーになってしまうのでどうしたもんか考え中。
        /*
        var dof = trans.GetComponent<UnityStandardAssets.ImageEffects.DepthOfField>();
        if(dof != null)
        {
            dof.focalLength = m_camera_params.focus_distance;
            dof.focalSize = m_camera_params.focal_length;
        }
         */
    }
}
