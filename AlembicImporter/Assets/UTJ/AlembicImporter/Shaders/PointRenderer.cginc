#include "UnityCG.cginc"

#if defined(UNITY_SUPPORT_INSTANCING) && !defined(UNITY_INSTANCING_ENABLED) && !defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && (SHADER_TARGET >= 45 && defined(ALEMBIC_PROCEDURAL_INSTANCING_ENABLED))
    #define UNITY_SUPPORT_INSTANCING
    #define UNITY_PROCEDURAL_INSTANCING_ENABLED

    uint unity_InstanceID;
    #define UNITY_VERTEX_INPUT_INSTANCE_ID uint instanceID : SV_InstanceID;
    #define UNITY_SETUP_INSTANCE_ID(v) unity_InstanceID = v.instanceID;
#endif


float _PointSize;
float _AlembicID;

#ifdef UNITY_SUPPORT_INSTANCING
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            StructuredBuffer<float3> _AlembicPoints;
            StructuredBuffer<float> _AlembicIDs;
    #else
            UNITY_INSTANCING_CBUFFER_START (Props)
                UNITY_DEFINE_INSTANCED_PROP (float, _AlembicIDs)
            UNITY_INSTANCING_CBUFFER_END
    #endif
#elif !defined(UNITY_VERTEX_INPUT_INSTANCE_ID)
    // for pre-5.5
    #define UNITY_VERTEX_INPUT_INSTANCE_ID
    #define UNITY_INSTANCING_CBUFFER_START(A, B)
    #define UNITY_DEFINE_INSTANCED_PROP(A, B)
    #define UNITY_ACCESS_INSTANCED_PROP(A)
    #define UNITY_INSTANCING_CBUFFER_END
    #define UNITY_SETUP_INSTANCE_ID(A)
    #define UNITY_TRANSFER_INSTANCE_ID(A, B)
#endif

float GetPointSize()
{
    return
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        _PointSize;
#else
        unity_ObjectToWorld[0][0];
#endif
}

float3 GetAlembicPoint()
{
    return
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        _AlembicPoints[unity_InstanceID];
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

float3x3 ToF33(float4x4 v)
{
    // (float3x3)v don't compile on some platforms
    return float3x3(
        v[0][0], v[0][1], v[0][2],
        v[1][0], v[1][1], v[1][2],
        v[2][0], v[2][1], v[2][2]);
}
