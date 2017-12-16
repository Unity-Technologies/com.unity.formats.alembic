// Upgrade NOTE: upgraded instancing buffer 'A' to new syntax.
// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

#include "UnityCG.cginc"

#if defined(UNITY_SUPPORT_INSTANCING) && !defined(UNITY_INSTANCING_ENABLED) && !defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && (SHADER_TARGET >= 45 && defined(ALEMBIC_PROCEDURAL_INSTANCING_ENABLED))
    #define UNITY_SUPPORT_INSTANCING
    #define UNITY_PROCEDURAL_INSTANCING_ENABLED

    uint unity_InstanceID;
    #define UNITY_VERTEX_INPUT_INSTANCE_ID uint instanceID : SV_InstanceID;
    #define UNITY_SETUP_INSTANCE_ID(v) unity_InstanceID = v.instanceID;
#endif


float3 _Translate;
float4 _Rotate;
float3 _Scale;
float _PointSize;
float _AlembicID;

#ifdef UNITY_SUPPORT_INSTANCING
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            StructuredBuffer<float3> _AlembicPoints;
            StructuredBuffer<float> _AlembicIDs;
    #else
            UNITY_INSTANCING_BUFFER_START (Props)
                UNITY_DEFINE_INSTANCED_PROP (float, _AlembicIDs)
#define _AlembicIDs_arr Props
            UNITY_INSTANCING_BUFFER_END(Props)
    #endif
#elif !defined(UNITY_VERTEX_INPUT_INSTANCE_ID)
    // for pre-5.5
    #define UNITY_VERTEX_INPUT_INSTANCE_ID
    #define UNITY_INSTANCING_BUFFER_START(A, B)
    #define UNITY_DEFINE_INSTANCED_PROP(A, B)
#define B_arr A
    #define UNITY_ACCESS_INSTANCED_PROP(A_arr, A)
    #define UNITY_INSTANCING_BUFFER_END(A)
    #define UNITY_SETUP_INSTANCE_ID(A)
    #define UNITY_TRANSFER_INSTANCE_ID(A, B)
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

float3 GetAlembicPoint()
{
    return
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        Rotate(_Rotate, _AlembicPoints[unity_InstanceID] * _Scale) + _Translate;
#else
        float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]);
#endif
}

float GetAlembicID()
{
    return
#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        _AlembicIDs[unity_InstanceID];
#else
        _AlembicID;
#endif
}

float4x4 GetPointMatrix()
{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    float3 ppos = GetAlembicPoint();
    float4 prot = _Rotate;
    float3 pscale = _Scale * _PointSize;
    return mul(mul(Translate44(ppos), Rotate44(prot)), Scale44(pscale));
#else
    return unity_ObjectToWorld;
#endif
}
