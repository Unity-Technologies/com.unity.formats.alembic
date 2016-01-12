using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("Alembic/Camera Params")]
[RequireComponent(typeof(Camera))]
public class AlembicCameraParams : MonoBehaviour
{
    public enum AspectRatioMode
    {
        Ratio_16_9,
        Ratio_16_10,
        Ratio_4_3,
        WidthPerHeight,
    };

    public AspectRatioMode m_aspectRatioMode = AspectRatioMode.Ratio_16_9;
    public float m_focalLength = 0.0f;
    public float m_focusDistance = 5.0f; // if 0.0f, automatically computed by aperture and fieldOfView. alembic's default value is 0.05f.
    public float m_aperture = 2.4f;

    public float GetAspectRatio()
    {
        switch (m_aspectRatioMode)
        {
            case AspectRatioMode.Ratio_16_9:  return 16.0f / 9.0f;
            case AspectRatioMode.Ratio_16_10: return 16.0f / 10.0f;
            case AspectRatioMode.Ratio_4_3:   return 4.0f / 3.0f;
        }

        var cam = GetComponent<Camera>();
        return (float)cam.pixelWidth / (float)cam.pixelHeight;
    }
}
