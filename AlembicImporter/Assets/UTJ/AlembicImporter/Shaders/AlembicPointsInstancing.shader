Shader "UTJ/Alembic/PointsInstancing" {
Properties {
    [Toggle(APPLY_TRANSFORM)] _ApplyTransform("Apply Transform", Int) = 1
    _Color ("Color", Color) = (1,1,1,1)
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
    _Glossiness("Smoothness", Range(0,1)) = 0.5
    _Metallic("Metallic", Range(0,1)) = 0.0
    _Emission("Emission", Color) = (0,0,0,0)
}
SubShader{
    Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1" }

CGPROGRAM
#pragma target 3.0
#pragma surface surf Standard fullforwardshadows vertex:vert addshadow
#pragma multi_compile ___ APPLY_TRANSFORM

#include "UnityCG.cginc"
#include "PseudoInstancing.cginc"

sampler2D _MainTex;
sampler2D _NormalMap;
sampler2D _EmissionMap;
sampler2D _SpecularMap;
sampler2D _GrossMap;
half _Glossiness;
half _Metallic;
half4 _Color;
half4 _Emission;
float4x4 _Transform;

struct Input {
    float2 uv_MainTex;
};


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
    ApplyInstanceTransform(iid, I.vertex);
#if APPLY_TRANSFORM
    I.vertex = mul(_Transform, I.vertex);
    I.normal = normalize(mul(_Transform, float4(I.normal, 0.0)).xyz);
#endif

    O.uv_MainTex = float4(I.texcoord.xy, 0.0, 0.0);
}

void surf(Input I, inout SurfaceOutputStandard O)
{
    fixed4 c = tex2D(_MainTex, I.uv_MainTex.xy) * _Color;
    O.Albedo = c.rgb;
    O.Metallic = _Metallic;
    O.Smoothness = _Glossiness;
    O.Alpha = c.a;
    O.Emission += _Emission;
}

ENDCG
}

FallBack Off
}
