namespace UnityEngine.Formats.Alembic.Util
{
    static class Utils
    {
        static Matrix4x4 ExtractRotation(this Matrix4x4 matrix)
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

        public static Vector3 ScaleFromHierarchy(this Component compo)
        {
            float x = 1, y = 1, z = 1;
            var c = compo;
            while (true)
            {
                var s = c.transform.localScale;
                var r = Matrix4x4.Rotate(c.transform.localRotation).transpose;
                s = r * s;
                x *= s.x;
                y *= s.y;
                z *= s.z;
                c = c.transform.parent;
                if (c == null)
                    break;
            }

            return new Vector3(x, y, z);
        }

        /*   static float3 cross(this float3 a, float3 b)
           {
               return Vector3.Cross(a, b);
           }

           static float dot(this float3 a, float3 b)
           {
               return Vector3.Dot(a, b);
           }

           static float fabs(float f)
           {
               return Mathf.Abs(f);
           }

           static void extractRotation(float3x3 A, ref quaternion q, int maxIter)
           {
               for (var iter = 0; iter < maxIter; iter++)
               {
                   var R = new float3x3(q);
                   var omega = (R.c0.cross(A.c0) + R.c1.cross(A.c1) + R.c2.cross(A.c2)) *
                       (1.0f / fabs(R.c0.dot(A.c0) + R.c1.dot(A.c1) + R.c2.dot(A.c2)) + 1.0e-9f);
                   var w = Vector3.Magnitude(omega);
                   if (w < 1.0e-9)
                       break;
                   q = Quaternion.AngleAxis(w, (1.0f / w) * omega) *  q;//quaternion(AngleAxisd(w, (1.0 / w) * omega)) *

                   Quaternion.Normalize(q);
               }
           }

           public static Vector3 extractScale(this Matrix4x4 m)
           {
               var q = new quaternion();
               var f33 = new float3x3(m.m00, m.m01, m.m02,
                   m.m10, m.m11, m.m12,
                   m.m20, m.m21, m.m22);
               extractRotation(f33, ref q, 20);

               var scaleM = m * Matrix4x4.Rotate(new Quaternion(q.value.x, q.value.y, q.value.z, q.value.w)).inverse;
               return new Vector3(scaleM.m00, scaleM.m11, scaleM.m22);
           }*/
    }
}
