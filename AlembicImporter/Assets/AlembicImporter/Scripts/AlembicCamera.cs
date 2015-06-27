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
    AbcAPI.aiCameraData m_abcdata;

    public override void AbcSetup(
        AlembicStream abcstream,
        AbcAPI.aiObject abcobj,
        AbcAPI.aiSchema abcschema)
    {
        base.AbcSetup(abcstream, abcobj, abcschema);
        m_trans = GetComponent<Transform>();
        m_camera = GetOrAddComponent<Camera>();
    }

    public override void AbcOnUpdateSample(AbcAPI.aiSample sample)
    {
        AbcAPI.aiCameraGetData(sample, ref m_abcdata);
    }

    public override void AbcUpdate()
    {
        var trans = m_trans;
        var cam = m_camera;

        trans.parent.forward = -trans.parent.forward;

        cam.fieldOfView = m_abcdata.field_of_view;
        cam.nearClipPlane = m_abcdata.near_clipping_plane;
        cam.farClipPlane = m_abcdata.far_clipping_plane;

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
