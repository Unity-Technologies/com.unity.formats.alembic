#ifndef AICommon_h
#define AICommon_h

#include "UnityCG.cginc"

struct DrawData
{
    int4 params; // [0]: begin_index, [1]: is_normal_indexed, [2]: is_uv_indexed
};

#if defined(SHADER_API_D3D11) || defined(SHADER_API_OPENGL)
Texture2D<int>      _Indices;
Texture2D<float4>   _Vertices;
Texture2D<float4>   _Velocities;
Texture2D<float4>   _Normals;
Texture2D<float2>   _UVs;
StructuredBuffer<DrawData> _DrawData;
#endif


struct Input {
    float2 uv_MainTex;
};

void vert_texturemesh(inout appdata_full v, out Input o)
{
    UNITY_INITIALIZE_OUTPUT(Input,o);

    int4 params = _DrawData[0].params;
    int begin_index         = params[0];
    int is_normal_indexed   = params[1];
    int is_uv_indexed       = params[2];

    int  ii1 = begin_index + (int)v.vertex.x;
    int2 ii2 = int2( ii1 & 0x3ff, ii1 >> 10 );  // assume texture width is 1024
    int  vi1 = _Indices[ii2];
    int2 vi2 = int2( vi1 & 0x3ff, vi1 >> 10 );  // 

    v.vertex        = float4(_Vertices[vi2].xyz, 1.0);
    v.texcoord.xy   = is_uv_indexed ? _UVs[vi2].xy : _UVs[ii2].xy;
    v.normal        = is_normal_indexed ? _Normals[vi2].xyz : _Normals[ii2].xyz;

    o.uv_MainTex = v.texcoord.xy;
}

#endif // AICommon_h
