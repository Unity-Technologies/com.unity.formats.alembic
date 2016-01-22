Shader "Hidden/Alembic/Standard" {
Properties {
    _Color ("Color", Color) = (1,1,1,1)
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
    _Glossiness ("Smoothness", Range(0,1)) = 0.5
    _Metallic ("Metallic", Range(0,1)) = 0.0
}
SubShader {
    Tags { "RenderType"="Opaque" "Queue"="Geometry" }

CGPROGRAM
//#pragma multi_compile ___ HAS_UVS
//#pragma multi_compile ___ IS_UV_INDEXED
//#pragma multi_compile ___ HAS_NORMALS
//#pragma multi_compile ___ IS_NORMAL_INDEXED
//#pragma multi_compile ___ HAS_VELOCITIES


#pragma target 5.0
#pragma only_renderers d3d11 opengl

#pragma surface surf Standard fullforwardshadows vertex:vert_texturemesh addshadow
#include "AICommon.cginc"

sampler2D _MainTex;
sampler2D _NormalMap;
sampler2D _EmissionMap;
sampler2D _SpecularMap;
sampler2D _GrossMap;
half _Glossiness;
half _Metallic;
fixed4 _Color;

void surf(Input IN, inout SurfaceOutputStandard o)
{
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
    o.Albedo = c.rgb;
    o.Metallic = _Metallic;
    o.Smoothness = _Glossiness;
    o.Alpha = c.a;
}
ENDCG
}
}
