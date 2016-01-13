Shader "Alembic/PointsColorBall" {
Properties {
    [Toggle(APPLY_TRANSFORM)] _ApplyTransform("Apply Transform", Int) = 1
    [Toggle(APPLY_SMOOTHING)] _ApplySmoothing("Apply Smoothing", Int) = 1
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
#pragma target 5.0
#pragma surface surf Standard fullforwardshadows vertex:vert addshadow
#pragma multi_compile ___ APPLY_TRANSFORM
#pragma multi_compile ___ APPLY_SMOOTHING

#include "Assets/AlembicImporter/Shaders/PseudoInstancing.cginc"

sampler2D _RandomBuffer;
sampler2D _ColorBuffer;
float4 _RandomBuffer_TexelSize;
float4 _ColorBuffer_TexelSize;

float _Emission;
float _Glossiness;
float _Metallic;
float _RandomDiffuse;
float4x4 _Transform;

struct Input {
    float4 color;
#if APPLY_SMOOTHING
    float4 world_pos;
    float4 sphere;
#endif // APPLY_SMOOTHING
};

float3 IntersectionRayPlane(float3 ray_pos, float3 ray_dir, float4 plane)
{
    float t = (-dot(ray_pos, plane.xyz) - plane.w) / dot(plane.xyz, ray_dir);
    return ray_pos + ray_dir * t;
}

float3 IntersectionEyeViewPlane(float3 world_pos, float3 plane_pos)
{
    float3 camera_dir = normalize(_WorldSpaceCameraPos.xyz - plane_pos);
    float4 plane = float4(camera_dir, dot(plane_pos, -camera_dir));
    float3 ray_pos = _WorldSpaceCameraPos.xyz;
    float3 ray_dir = normalize(world_pos - _WorldSpaceCameraPos.xyz);
    return IntersectionRayPlane(ray_pos, ray_dir, plane);
}

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
    float4 rand = Random(objid);

    ApplyInstanceTransform(iid, I.vertex);
#if APPLY_TRANSFORM
    I.vertex = mul(_Transform, I.vertex);
    I.normal = normalize(mul(_Transform, float4(I.normal, 0.0)).xyz);
#endif
    I.vertex.xyz += rand.xyz * _RandomDiffuse;

    O.color = GetColor(rand.x);
#if APPLY_SMOOTHING
    O.world_pos = I.vertex;
    float3 sphere_pos = GetInstanceTranslation(iid) + (rand.xyz * _RandomDiffuse);
#if APPLY_TRANSFORM
    sphere_pos += float3(_Transform[0].w, _Transform[1].w, _Transform[2].w);
#endif
    float sphere_radius = GetModelScale().x * 0.5 * 0.9;
    O.sphere = float4(sphere_pos, sphere_radius);
#endif // APPLY_SMOOTHING
}

void surf(Input I, inout SurfaceOutputStandard O)
{
#if APPLY_SMOOTHING
    float3 proj_pos = IntersectionEyeViewPlane(I.world_pos.xyz, I.sphere.xyz);
    float proj_dist = length(proj_pos - I.sphere.xyz);
    if (proj_dist > I.sphere.w) {
        discard;
    }
#endif // APPLY_SMOOTHING

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
