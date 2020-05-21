namespace UnityEngine.Formats.Alembic.Util
{
    static class Utils
    {
        public static Matrix4x4 WorldNoScale(this Transform transform)
        {
            var rotation = transform.rotation;
            var pos = transform.position;
            var rot = Matrix4x4.Rotate(rotation);
            rot = rot.transpose;
            var t = rot.MultiplyPoint(-pos);
            return Matrix4x4.TRS(t, Quaternion.Inverse(rotation), new Vector3(1, 1, 1));
        }
    }
}
