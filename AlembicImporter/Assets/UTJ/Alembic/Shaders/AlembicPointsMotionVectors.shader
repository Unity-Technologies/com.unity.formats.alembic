Shader "Hidden/Alembic/PointsMotionVectors"
{
    SubShader
    {
        Pass
        {
            Name "MOTIONVECTORS"
            Tags{ "LightMode" = "MotionVectors" }

            ZTest LEqual
            Cull Back
            ZWrite Off

CGPROGRAM
#pragma vertex VertMotionVectors
#pragma fragment FragMotionVectors
#pragma target 4.5

#include "UnityCG.cginc"
#include "PointRenderer.cginc"

#if defined(USING_STEREO_MATRICES)
    float4x4 _StereoNonJitteredVP[2];
    float4x4 _StereoPreviousVP[2];
#else
    float4x4 _NonJitteredVP;
    float4x4 _PreviousVP;
#endif
float4x4 _PreviousM;
bool _HasLastPositionData;
bool _ForceNoMotion;
float _MotionVectorDepthBias;

struct MotionVectorData
{
    float4 transferPos : TEXCOORD0;
    float4 transferPosOld : TEXCOORD1;
    float4 pos : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

struct MotionVertexInput
{
    float4 vertex : POSITION;
    uint iid : SV_InstanceID;
};

MotionVectorData VertMotionVectors(MotionVertexInput v)
{
    MotionVectorData o;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


    float4 vertex = v.vertex;
    float4 vertex_old = v.vertex;
    vertex = mul(GetPointMatrix(v.iid), vertex);
    vertex_old = mul(GetOldPointMatrix(v.iid), vertex_old);

    o.pos = mul(UNITY_MATRIX_VP, vertex);
#if defined(UNITY_REVERSED_Z)
    o.pos.z -= _MotionVectorDepthBias * o.pos.w;
#else
    o.pos.z += _MotionVectorDepthBias * o.pos.w;
#endif

#if defined(USING_STEREO_MATRICES)
    o.transferPos = mul(_StereoNonJitteredVP[unity_StereoEyeIndex], vertex);
    o.transferPosOld = mul(_StereoPreviousVP[unity_StereoEyeIndex], vertex_old);
#else
    o.transferPos = mul(_NonJitteredVP, vertex);
    o.transferPosOld = mul(_PreviousVP, vertex_old);
#endif

    return o;
}

half4 FragMotionVectors(MotionVectorData i) : SV_Target
{
    float3 hPos = (i.transferPos.xyz / i.transferPos.w);
    float3 hPosOld = (i.transferPosOld.xyz / i.transferPosOld.w);

    // V is the viewport position at this pixel in the range 0 to 1.
    float2 vPos = (hPos.xy + 1.0f) / 2.0f;
    float2 vPosOld = (hPosOld.xy + 1.0f) / 2.0f;

#if UNITY_UV_STARTS_AT_TOP
    vPos.y = 1.0 - vPos.y;
    vPosOld.y = 1.0 - vPosOld.y;
#endif
    half2 uvDiff = vPos - vPosOld;
    return lerp(half4(uvDiff, 0, 1), 0, (half)_ForceNoMotion);
}
ENDCG
        }
    }
}


