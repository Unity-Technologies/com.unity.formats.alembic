using System.Runtime.InteropServices;

namespace UnityEngine.Formats.Alembic.Sdk
{
    [StructLayout(LayoutKind.Sequential)]
    struct CameraData
    {
        public Bool visibility { get; set; }
        public float focalLength { get; set; }
        public Vector2 sensorSize { get; set; }
        public Vector2 lensShift { get; set; }
        public Vector2 nearFar { get; set; }
    }
}