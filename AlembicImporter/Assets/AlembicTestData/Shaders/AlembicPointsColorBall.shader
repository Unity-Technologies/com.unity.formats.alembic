Shader "Alembic/PointsColorBall" {
Properties {
    _ColorBuffer("Color", 2D) = "white" {}
    _RandomBuffer("Random", 2D) = "white" {}
    _RandomDiffuse("Random Diffuse", Float) = 0.0

    _Emission("Emission", Range(0,1)) = 0.0
    _Glossiness ("Smoothness", Range(0,1)) = 0.5
    _Metallic("Metallic", Range(0,1)) = 0.0
}
SubShader{
    Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1" }

CGPROGRAM
#pragma target 3.0
#pragma surface surf Standard fullforwardshadows vertex:vert addshadow

#include "Assets/AlembicImporter/Shaders/PseudoInstancing.cginc"

sampler2D _RandomBuffer;
sampler2D _ColorBuffer;
float4 _RandomBuffer_TexelSize;
float4 _ColorBuffer_TexelSize;

float _Emission;
float _Glossiness;
float _Metallic;
float _RandomDiffuse;

struct Input {
    float4 color;
};


float4 Random(float iid)
{
    float4 coord = float4(_RandomBuffer_TexelSize.xy * float2(iid, iid * _RandomBuffer_TexelSize.x), 0.0, 0.0);
    return tex2Dlod(_RandomBuffer, coord) * 2.0 - 1.0;
}

float4 GetColor(float random)
{
    return tex2Dlod(_ColorBuffer, float4(random, 0.0, 0.0, 0.0));
}

void ApplyInstanceTransform(int instance_id, inout float4 vertex)
{
    if(instance_id >= GetNumInstances()) {
        vertex.xyz *= 0.0;
        return;
    }
    vertex.xyz *= GetModelScale();
    vertex.xyz += GetInstanceTranslation(instance_id) * GetTransScale();
}


void vert(inout appdata_full I, out Input O)
{
    UNITY_INITIALIZE_OUTPUT(Input, O);

    int iid = GetInstanceID(I.texcoord1.x);
    float objid = GetObjectID(iid);
    float4 rand = Random(iid);

    ApplyInstanceTransform(iid, I.vertex);
    I.vertex.xyz += rand.xyz * _RandomDiffuse;

    O.color = GetColor(rand.x);
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
