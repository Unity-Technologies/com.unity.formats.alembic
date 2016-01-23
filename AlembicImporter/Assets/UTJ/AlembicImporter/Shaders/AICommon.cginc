#ifndef AICommon_h
#define AICommon_h

#include "UnityCG.cginc"

#if defined(SHADER_API_D3D11) || defined(SHADER_API_OPENGL)
float4 _DrawData; // [0]: begin_index, [1]: index_count, [2]: is_normal_indexed, [3]: is_uv_indexed

Texture2D<int>      _Indices;
Texture2D<float>    _Vertices;
Texture2D<float>    _Normals;
Texture2D<float>    _UVs;
Texture2D<float>    _Velocities;
#endif


struct Input {
    float2 uv_MainTex;
};


void vert_texturemesh(inout appdata_full v, out Input o)
{
    UNITY_INITIALIZE_OUTPUT(Input,o);

    int begin_index         = _DrawData[0];
    int index_count         = _DrawData[1];
    int vertex_count        = _DrawData[2];
    int iheight = index_count >> 10;
    int vheight = vertex_count >> 10;

    int  ii1 = begin_index + (int)v.texcoord.x;
    if(ii1 > index_count) {
        v.vertex = 0.0;
        return;
    }

    int2 ii2 = int2( ii1 & 0x3ff, ii1 >> 10 );  // assume texture width is 1024
    int  vi1 = _Indices[ii2];
    int2 vi2 = int2( vi1 & 0x3ff, vi1 >> 10 );  //

    v.vertex = float4(
        _Vertices[vi2 * int2(3,1) + int2(0,0)],
        _Vertices[vi2 * int2(3,1) + int2(1,0)],
        _Vertices[vi2 * int2(3,1) + int2(2,0)],
        1.0 );

#ifdef HAS_UVS
#ifdef IS_UV_INDEXED
    v.texcoord.xy = float4(
        _UVs[vi2 * int2(2,1) + int2(0,0)],
        _UVs[vi2 * int2(2,1) + int2(1,0)] );
#else // IS_UV_INDEXED
    v.texcoord.xy = float4(
        _UVs[ii2 * int2(2,1) + int2(0,0)],
        _UVs[ii2 * int2(2,1) + int2(1,0)] );
#endif // IS_UV_INDEXED
#endif // HAS_UVS

#ifdef HAS_NORMALS
#ifdef IS_NORMAL_INDEXED
    v.normal = float3(
        _Normals[vi2 * int2(3,1) + int2(0,0)],
        _Normals[vi2 * int2(3,1) + int2(1,0)],
        _Normals[vi2 * int2(3,1) + int2(2,0)] );
#else  // IS_NORMAL_INDEXED
    v.normal = float3(
        _Normals[ii2 * int2(3,1) + int2(0,0)],
        _Normals[ii2 * int2(3,1) + int2(1,0)],
        _Normals[ii2 * int2(3,1) + int2(2,0)] );
#endif // IS_NORMAL_INDEXED
#endif // HAS_NORMALS

#ifdef HAS_VELOCITIES
    float3 velocity = float3(
        _Velocities[vi2 * int2(3,1) + int2(0,0)],
        _Velocities[vi2 * int2(3,1) + int2(1,0)],
        _Velocities[vi2 * int2(3,1) + int2(2,0)] );
#endif

    o.uv_MainTex = v.texcoord.xy;
}

#endif // AICommon_h
