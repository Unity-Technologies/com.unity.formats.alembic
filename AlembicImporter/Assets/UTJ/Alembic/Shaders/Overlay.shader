Shader "Alembic/Overlay" {
Properties
{
    [Enum(Normals,0,Tangents,1,UV0,2,UV1,3,UV2,4,UV3,5,Colors,6)] _Type("Overlay component", Int) = 6
}

CGINCLUDE
#include "UnityCG.cginc"
int _Type;

struct ia_out
{
    float4 vertex : POSITION;
    float4 normal : NORMAL;
    float4 tangent : TANGENT;
    float4 color : COLOR;
    float4 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    float4 uv2 : TEXCOORD2;
    float4 uv3 : TEXCOORD3;
    uint vertexID : SV_VertexID;
    uint instanceID : SV_InstanceID;
};

struct vs_out
{
    float4 vertex : SV_POSITION;
    float4 color : TEXCOORD0;
};


vs_out vert(ia_out v)
{
    vs_out o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    if (_Type == 0) { // normals
        o.color.rgb = v.normal.xyz * 0.5 + 0.5;
        o.color.a = 1.0;
    }
    else if (_Type == 1) { // tangents
        o.color.rgb = (v.tangent.xyz * v.tangent.w) * 0.5 + 0.5;
        o.color.a = 1.0;
    }
    else if (_Type == 2) { // uv0
        o.color = float4(v.uv0.xyz, 1.0);
    }
    else if (_Type == 3) { // uv1
        o.color = float4(v.uv1.xyz, 1.0);
    }
    else if (_Type == 4) { // uv2
        o.color = float4(v.uv2.xyz, 1.0);
    }
    else if (_Type == 5) { // uv3
        o.color = float4(v.uv3.xyz, 1.0);
    }
    else if (_Type == 6) { // colors
        o.color = v.color;
    }
    return o;
}

float4 frag(vs_out v) : SV_Target
{
    return v.color;
}

ENDCG

    SubShader
    {
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent+100" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            ENDCG
        }
    }
}
