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
    public AbcAPI.aiAspectRatioModeOverride m_aspectRatioMode = AbcAPI.aiAspectRatioModeOverride.InheritStreamSetting;

    Transform m_trans;
    Camera m_camera;
    AbcAPI.aiCameraData m_abcData;
    bool m_pendingUpdate;

    public override void AbcSetup(AlembicStream abcStream,
                                  AbcAPI.aiObject abcObj,
                                  AbcAPI.aiSchema abcSchema)
    {
        base.AbcSetup(abcStream, abcObj, abcSchema);

        m_trans = GetComponent<Transform>();
        m_camera = GetOrAddComponent<Camera>();

        m_pendingUpdate = false;
    }

    public override void AbcGetConfig(ref AbcAPI.aiConfig config)
    {
        if (m_aspectRatioMode != AbcAPI.aiAspectRatioModeOverride.InheritStreamSetting)
        {
            config.aspectRatio = AbcAPI.GetAspectRatio((AbcAPI.aiAspectRatioMode) m_aspectRatioMode);
        }
    }

    public override void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged)
    {
        if (sample.ptr != (IntPtr)0)
        {
            AbcAPI.aiCameraGetData(sample, ref m_abcData);
            m_pendingUpdate = true;
        }
    }

    public override void AbcUpdate()
    {
        if (m_pendingUpdate)
        {
            m_trans.parent.forward = -m_trans.parent.forward;
            m_camera.fieldOfView = m_abcData.fieldOfView;
            m_camera.nearClipPlane = m_abcData.nearClippingPlane;
            m_camera.farClipPlane = m_abcData.farClippingPlane;
            
            // no use for focusDistance and focalLength yet (could be usefull for DoF component)
            
            m_pendingUpdate = false;
        }
    }
}
