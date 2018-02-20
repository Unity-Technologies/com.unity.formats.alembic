#include "UnityCG.cginc"

float3 _Translate;
float4 _Rotate;
float3 _Scale;
float _PointSize;
#ifdef UNITY_SUPPORT_INSTANCING
StructuredBuffer<float3> _AlembicPoints;
StructuredBuffer<float> _AlembicIDs;
#endif

float GetPointSize()
{
    return _PointSize;
}

float3x3 ToF33(float4x4 v)
{
    // (float3x3)v don't compile on some platforms
    return float3x3(
        v[0][0], v[0][1], v[0][2],
        v[1][0], v[1][1], v[1][2],
        v[2][0], v[2][1], v[2][2]);
}


float4x4 Translate44(float3 t)
{
    return float4x4(
        1.0, 0.0, 0.0, t.x,
        0.0, 1.0, 0.0, t.y,
        0.0, 0.0, 1.0, t.z,
        0.0, 0.0, 0.0, 1.0);
}

float4x4 Scale44(float3 s)
{
    return float4x4(
        s.x, 0.0, 0.0, 0.0,
        0.0, s.y, 0.0, 0.0,
        0.0, 0.0, s.x, 0.0,
        0.0, 0.0, 0.0, 1.0);
}

// q: quaternion
float4x4 Rotate44(float4 q)
{
    return float4x4(
        1.0-2.0*q.y*q.y - 2.0*q.z*q.z,  2.0*q.x*q.y - 2.0*q.z*q.w,          2.0*q.x*q.z + 2.0*q.y*q.w,          0.0,
        2.0*q.x*q.y + 2.0*q.z*q.w,      1.0 - 2.0*q.x*q.x - 2.0*q.z*q.z,    2.0*q.y*q.z - 2.0*q.x*q.w,          0.0,
        2.0*q.x*q.z - 2.0*q.y*q.w,      2.0*q.y*q.z + 2.0*q.x*q.w,          1.0 - 2.0*q.x*q.x - 2.0*q.y*q.y,    0.0,
        0.0,                            0.0,                                0.0,                                1.0 );
}

float3 Cross(float3 l, float3 r)
{
    return float3(
        l.y * r.z - l.z * r.y,
        l.z * r.x - l.x * r.z,
        l.x * r.y - l.y * r.x);
}

// q: quaternion
float3 Rotate(float4 q, float3 p)
{
    float3 a = cross(q.xyz, p);
    float3 b = cross(q.xyz, a);
    return p + (a * q.w + b) * 2.0;
}

float3 GetAlembicPoint(int iid)
{
    return
#ifdef UNITY_SUPPORT_INSTANCING
        Rotate(_Rotate, _AlembicPoints[iid] * _Scale) + _Translate;
#else
        float3(0,0,0);
#endif
}

float GetAlembicID(int iid)
{
    return
#ifdef UNITY_SUPPORT_INSTANCING
        _AlembicIDs[iid];
#else
        0;
#endif
}

float4x4 GetPointMatrix(int iid)
{
#ifdef UNITY_SUPPORT_INSTANCING
    float3 ppos = GetAlembicPoint(iid);
    float4 prot = _Rotate;
    float3 pscale = _Scale * _PointSize;
    return mul(mul(Translate44(ppos), Rotate44(prot)), Scale44(pscale));
#else
    return unity_ObjectToWorld;
#endif
}
