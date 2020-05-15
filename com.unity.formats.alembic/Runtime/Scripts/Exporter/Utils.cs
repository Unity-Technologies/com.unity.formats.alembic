namespace UnityEngine.Formats.Alembic.Util
{
    static class Utils
    {
        public static Matrix4x4 ExtractRotation(this Matrix4x4 matrix)
        {
            Vector3 forward;
            forward.x = matrix.m02;
            forward.y = matrix.m12;
            forward.z = matrix.m22;

            Vector3 upwards;
            upwards.x = matrix.m01;
            upwards.y = matrix.m11;
            upwards.z = matrix.m21;

            var q = Quaternion.LookRotation(forward, upwards);
            return Matrix4x4.Rotate(q);
        }

        public static Vector3 ExtractScale(this Matrix4x4 matrix)
        {
            var rot = ExtractRotation(matrix);
            var scaleM = matrix * rot.inverse;
            return new Vector3(scaleM.m00, scaleM.m11, scaleM.m22);
        }
    }
}
