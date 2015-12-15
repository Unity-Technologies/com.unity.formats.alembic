Shader "Alembic/AlembicPointsColorBall" {
Properties {
    _RMin("ColorRange R min", Range(0,1)) = 0.0
    _RMax("ColorRange R max", Range(0,1)) = 1.0
    _GMin("ColorRange G min", Range(0,1)) = 0.0
    _GMax("ColorRange G max", Range(0,1)) = 1.0
    _BMin("ColorRange G min", Range(0,1)) = 0.0
    _BMax("ColorRange G max", Range(0,1)) = 1.0
    _Emission("Emission", Range(0,1)) = 0.0
    _Glossiness ("Smoothness", Range(0,1)) = 0.5
    _Metallic("Metallic", Range(0,1)) = 0.0
}
SubShader {
    Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }

CGPROGRAM
#pragma target 3.0
#pragma surface surf Standard fullforwardshadows vertex:vert addshadow

#include "UnityCG.cginc"
#include "Assets/Ist/Foundation/Shaders/Math.cginc"
#include "Assets/Ist/Foundation/Shaders/Geometry.cginc"
#include "Assets/Ist/BatchRenderer/Shaders/BatchRenderer.cginc"

float _RMin;
float _RMax;
float _GMin;
float _GMax;
float _BMin;
float _BMax;
float _Emission;
float _Glossiness;
float _Metallic;


struct Input {
    float4 color;
};

sampler2D g_instance_texture_id;


void ApplyInstanceTransform(int instance_id, inout float4 vertex)
{
    if(instance_id >= GetNumInstances()) {
        vertex.xyz *= 0.0;
        return;
    }
    vertex.xyz *= GetBaseScale();
    vertex.xyz += GetInstanceTranslation(instance_id);
}

float3 iq_rand(float3 p)
{
    p = float3(dot(p, float3(127.1, 311.7, 311.7)), dot(p, float3(269.5, 183.3, 183.3)), dot(p, float3(269.5, 183.3, 183.3)));
    return frac(sin(p)*43758.5453);
}


void vert(inout appdata_full I, out Input O)
{
    UNITY_INITIALIZE_OUTPUT(Input, O);

    int iid = GetBatchBegin() + I.texcoord1.x;
    ApplyInstanceTransform(iid, I.vertex);
    float abcid = tex2Dlod(g_instance_texture_id, InstanceTexcoord(iid)).xyz;

    float3 cmin = float3(_RMin, _GMin, _BMin);
    float3 cmax = float3(_RMax, _GMax, _BMax);
    float3 w = iq_rand(abcid * 0.1328, abcid * 0.7532, abcid * 0.6753);
    O.color = float4(lerp(cmin, cmax, w), 0.0);
}

void surf(Input I, inout SurfaceOutputStandard O)
{
    float4 c = I.color;
    O.Albedo = c.rgb;
    O.Metallic = _Metallic;
    O.Smoothness = _Glossiness;
    O.Emission += c.rgb * _Emission;
}

ENDCG
}

FallBack Off
}
