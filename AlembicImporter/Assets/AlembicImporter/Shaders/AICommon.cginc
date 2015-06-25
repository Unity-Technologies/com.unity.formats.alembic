#ifndef AICommon_h
#define AICommon_h

#include "UnityCG.cginc"

#if defined(SHADER_API_D3D11) || defined(SHADER_API_OPENGL)
float4 _DrawData; // [0]: begin_index, [1]: index_count, [2]: is_normal_indexed, [3]: is_uv_indexed

Texture2D<float>    _Indices;
Texture2D<float4>   _Vertices;
Texture2D<float4>   _Normals;
Texture2D<float2>   _UVs;
Texture2D<float4>   _Velocities;
#endif


struct Input {
    float2 uv_MainTex;
};

void vert_texturemesh(inout appdata_full v, out Input o)
{
    UNITY_INITIALIZE_OUTPUT(Input,o);

    int begin_index         = _DrawData[0];
    int index_count         = _DrawData[1];
    int is_normal_indexed   = _DrawData[2];
    int is_uv_indexed       = _DrawData[3];

    int  ii1 = begin_index + (int)v.texcoord.x;
    if(ii1 > index_count) {
        v.vertex = 0.0;
        return;
    }

    int2 ii2 = int2( ii1 & 0x3ff, ii1 >> 10 );  // assume texture width is 1024
    int  vi1 = (int)_Indices[ii2];
    int2 vi2 = int2( vi1 & 0x3ff, vi1 >> 10 );  // 

    v.vertex = float4(_Vertices[vi2].xyz, 1.0);
#ifdef HAS_UVS
    v.texcoord.xy = is_uv_indexed ? _UVs[vi2].xy : _UVs[ii2].xy;
#endif
#ifdef HAS_NORMALS
    v.normal = is_normal_indexed ? _Normals[vi2].xyz : _Normals[ii2].xyz;
#endif
#ifdef HAS_VELOCITIES
#endif

    o.uv_MainTex = v.texcoord.xy;
}

#endif // AICommon_h
