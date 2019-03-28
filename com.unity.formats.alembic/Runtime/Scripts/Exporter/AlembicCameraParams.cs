using UnityEngine;

#if ENABLE_ALEMBIC_CAMERA_PARAMS
namespace UnityEngine.Formats.Alembic.Util
{
    [AddComponentMenu("UTJ/Alembic/Camera Params")]
    [RequireComponent(typeof(Camera))]
    internal class AlembicCameraParams : MonoBehaviour
    {
        public enum AspectRatioMode
        {
            Ratio16x9,
            Ratio16x10,
            Ratio4x3,
            WidthPerHeight,
        };

        public AspectRatioMode m_cameraAspectRatio = AspectRatioMode.Ratio16x9;

        [Tooltip("in cm")]
        public float m_focusDistance = 5.0f;

        [Tooltip("in mm. if 0.0f, automatically computed by aperture and fieldOfView. alembic's default value is 35.0")]
        public float m_focalLength = 0.0f;

        [Tooltip("in cm")]
        public float m_aperture = 2.4f;

        public float AspectRatio
        {
            get
            {
                switch (m_cameraAspectRatio)
                {
                    case AspectRatioMode.Ratio16x9: return 16.0f / 9.0f;
                    case AspectRatioMode.Ratio16x10: return 16.0f / 10.0f;
                    case AspectRatioMode.Ratio4x3: return 4.0f / 3.0f;
                }

                var cam = GetComponent<Camera>();
                return (float)cam.pixelWidth / (float)cam.pixelHeight;
            }
        }
    }
}
#endif // ENABLE_ALEMBIC_CAMERA_PARAMS