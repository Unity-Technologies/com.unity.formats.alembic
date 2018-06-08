using UnityEngine;

namespace UTJ.Alembic
{
    [AddComponentMenu("UTJ/Alembic/Camera Params")]
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

        public AspectRatioMode m_cameraSspectRatio = AspectRatioMode.Ratio_16_9;

        [Tooltip("in cm")]
        public float m_focusDistance = 5.0f;

        [Tooltip("in mm. if 0.0f, automatically computed by aperture and fieldOfView. alembic's default value is 35.0")]
        public float m_focalLength = 0.0f;

        [Tooltip("in cm")]
        public float m_aperture = 2.4f;

        public float GetAspectRatio()
        {
            switch (m_cameraSspectRatio)
            {
                case AspectRatioMode.Ratio_16_9:  return 16.0f / 9.0f;
                case AspectRatioMode.Ratio_16_10: return 16.0f / 10.0f;
                case AspectRatioMode.Ratio_4_3:   return 4.0f / 3.0f;
            }

            var cam = GetComponent<Camera>();
            return (float)cam.pixelWidth / (float)cam.pixelHeight;
        }
    }
}
