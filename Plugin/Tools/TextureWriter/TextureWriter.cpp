#include "pch.h"
#include "Common.h"
#include "GraphicsDevice.h"
#include "half.h"

#pragma comment(lib, "Half.lib")



template<class T> struct tvec2;
template<class T> struct tvec3;
template<class T> struct tvec4;

template<class T> struct tvec2
{
    T x, y;
    tvec2() {} // !not clear members!
    tvec2(T a) : x(a), y(a) {}
    tvec2(T a, T b) : x(a), y(b) {}
    tvec2(const tvec3<T>& src);
    tvec2(const tvec4<T>& src);
    template<class U> tvec2(const tvec2<U>& src) : x(T(src.x)), y(T(src.y)) {}
};

template<class T> struct tvec3
{
    T x, y, z;
    tvec3() {} // !not clear members!
    tvec3(T a) : x(a), y(a), z(a) {}
    tvec3(T a, T b, T c) : x(a), y(b), z(c) {}
    tvec3(const tvec2<T>& src);
    tvec3(const tvec4<T>& src);
    template<class U> tvec3(const tvec3<U>& src) : x(T(src.x)), y(T(src.y)), z(T(src.z)) {}
};

template<class T> struct tvec4
{
    T x, y, z, w;
    tvec4() {} // !not clear members!
    tvec4(T a) : x(a), y(a), z(a), w(a) {}
    tvec4(T a, T b, T c, T d) : x(a), y(b), z(c), w(d) {}
    tvec4(const tvec2<T>& src);
    tvec4(const tvec3<T>& src);
    template<class U> tvec4(const tvec3<U>& src) : x(T(src.x)), y(T(src.y)), z(T(src.z)), w() {}
    template<class U> tvec4(const tvec4<U>& src) : x(T(src.x)), y(T(src.y)), z(T(src.z)), w(T(src.w)) {}
};

template<class T> tvec2<T>::tvec2(const tvec3<T>& src) : x(src.x), y(src.y) {}
template<class T> tvec2<T>::tvec2(const tvec4<T>& src) : x(src.x), y(src.y) {}
template<class T> tvec3<T>::tvec3(const tvec2<T>& src) : x(src.x), y(src.y), z() {}
template<class T> tvec3<T>::tvec3(const tvec4<T>& src) : x(src.x), y(src.y), z(src.z) {}
template<class T> tvec4<T>::tvec4(const tvec2<T>& src) : x(src.x), y(src.y), z(), w() {}
template<class T> tvec4<T>::tvec4(const tvec3<T>& src) : x(src.x), y(src.y), z(src.z), w() {}

typedef int64_t lint;
typedef tvec2<half> half2;
typedef tvec3<half> half3;
typedef tvec4<half> half4;
typedef tvec2<float> float2;
typedef tvec3<float> float3;
typedef tvec4<float> float4;
typedef tvec2<int> int2;
typedef tvec3<int> int3;
typedef tvec4<int> int4;
typedef tvec2<lint> lint2;
typedef tvec3<lint> lint3;
typedef tvec4<lint> lint4;



template<class DstType, class SrcType>
inline void tConvert(DstType& dst, const SrcType& src)
{
    dst = DstType(src);
}

template<class DstType, class SrcType>
struct tConvertArrayImpl
{
    void operator()(void *&dst_, const void *src_, size_t num_elements)
    {
        DstType *dst = (DstType*)dst_;
        const SrcType *src = (const SrcType*)src_;
        for (size_t i = 0; i < num_elements; ++i) {
            tConvert<DstType, SrcType>(dst[i], src[i]);
        }
    }
};
template<class T>
struct tConvertArrayImpl<T, T>
{
    void operator()(void *&dst, const void *src, size_t num_elements)
    {
        dst = (void*)src;
    }
};

template<class DstType, class SrcType> inline void tConvertArray(void *&dst, const void *src, size_t num_elements)
{
    tConvertArrayImpl<DstType, SrcType>()(dst, src, num_elements);
}



enum tDataFormat
{
    tDataFormat_Unknown,
    tDataFormat_Half,
    tDataFormat_Half2,
    tDataFormat_Half3,
    tDataFormat_Half4,
    tDataFormat_Float,
    tDataFormat_Float2,
    tDataFormat_Float3,
    tDataFormat_Float4,
    tDataFormat_Int,
    tDataFormat_Int2,
    tDataFormat_Int3,
    tDataFormat_Int4,
    tDataFormat_LInt, // 64bit int
};

template<tDataFormat F> struct tDataFormatToType;
template<tTextureFormat F> struct tTextureFormatToType;

#define Def(Enum, Type) template<> struct tDataFormatToType<Enum> { typedef Type type; };
Def(tDataFormat_Half,  half)
Def(tDataFormat_Half2, half2)
Def(tDataFormat_Half3, half3)
Def(tDataFormat_Half4, half4)
Def(tDataFormat_Float,  float)
Def(tDataFormat_Float2, float2)
Def(tDataFormat_Float3, float3)
Def(tDataFormat_Float4, float4)
Def(tDataFormat_Int,  int)
Def(tDataFormat_Int2, int2)
Def(tDataFormat_Int3, int3)
Def(tDataFormat_Int4, int4)
Def(tDataFormat_LInt, lint)
#undef Def

#define Def(Enum, Type) template<> struct tTextureFormatToType<Enum> { typedef Type type; };
Def(tTextureFormat_RHalf, half)
Def(tTextureFormat_RGHalf, half2)
Def(tTextureFormat_ARGBHalf, half4)
Def(tTextureFormat_RFloat, float)
Def(tTextureFormat_RGFloat, float2)
Def(tTextureFormat_ARGBFloat, float4)
Def(tTextureFormat_RInt, int)
Def(tTextureFormat_RGInt, int2)
Def(tTextureFormat_ARGBInt, int4)
#undef Def


tCLinkage tExport bool tWriteTexture(
    void *dst_tex, int dst_width, int dst_height, tTextureFormat dst_fmt,
    const void *src, int src_num, tDataFormat src_fmt)
{
    if (dst_tex == nullptr || src == nullptr) { return false; }
    if (src_num == 0) { return true; }

#define TConvert(A1, A2)\
    case A2: return tWriteTextureWithConversion(dst_tex, dst_width, dst_height, dst_fmt, src, src_num,\
        tConvertArray<tTextureFormatToType<A1>::type, tDataFormatToType<A2>::type>)\

    switch (dst_fmt)
    {
    case tTextureFormat_RHalf:
        switch (src_fmt)
        {
            TConvert(tTextureFormat_RHalf, tDataFormat_Half);
            TConvert(tTextureFormat_RHalf, tDataFormat_Float);
        }
        break;
    case tTextureFormat_RGHalf:
        switch (src_fmt)
        {
            TConvert(tTextureFormat_RGHalf, tDataFormat_Half2);
            TConvert(tTextureFormat_RGHalf, tDataFormat_Float2);
        }
        break;
    case tTextureFormat_ARGBHalf:
        switch (src_fmt)
        {
            TConvert(tTextureFormat_ARGBHalf, tDataFormat_Half3);
            TConvert(tTextureFormat_ARGBHalf, tDataFormat_Half4);
            TConvert(tTextureFormat_ARGBHalf, tDataFormat_Float3);
            TConvert(tTextureFormat_ARGBHalf, tDataFormat_Float4);
        }
        break;

    case tTextureFormat_RFloat:
        switch (src_fmt)
        {
            TConvert(tTextureFormat_RFloat, tDataFormat_Half);
            TConvert(tTextureFormat_RFloat, tDataFormat_Float);
            TConvert(tTextureFormat_RFloat, tDataFormat_Int);
            TConvert(tTextureFormat_RFloat, tDataFormat_LInt);
        }
        break;
    case tTextureFormat_RGFloat:
        switch (src_fmt)
        {
            TConvert(tTextureFormat_RGFloat, tDataFormat_Half2);
            TConvert(tTextureFormat_RGFloat, tDataFormat_Float2);
            TConvert(tTextureFormat_RGFloat, tDataFormat_Int2);
        }
        break;
    case tTextureFormat_ARGBFloat:
        switch (src_fmt)
        {
            TConvert(tTextureFormat_ARGBFloat, tDataFormat_Half3);
            TConvert(tTextureFormat_ARGBFloat, tDataFormat_Half4);
            TConvert(tTextureFormat_ARGBFloat, tDataFormat_Float3);
            TConvert(tTextureFormat_ARGBFloat, tDataFormat_Float4);
            TConvert(tTextureFormat_ARGBFloat, tDataFormat_Int3);
            TConvert(tTextureFormat_ARGBFloat, tDataFormat_Int4);
        }
        break;
    }

#undef TConvert

    tLog("tWriteTexture(): this format combination is not supported");
    return false;
}


tCLinkage tExport bool tPointsCopyPositionsToTexture(aiPointsData *data, void *tex, int width, int height, tTextureFormat fmt)
{
    if (data == nullptr) { return false; }

    if (fmt == tTextureFormat_ARGBFloat)
    {
        return tWriteTextureWithConversion(tex, width, height, fmt, data->positions, data->count,
            [](void *dst, const abcV3 *src, size_t len) {
                for (size_t i = 0; i < len; ++i) {
                    ((abcV4*)dst)[i] = abcV4(src[i].x, src[i].y, src[i].z, 0.0f);
                }
            });
    }
    else {
        tLog("aiPointsCopyPositionsToTexture(): format must be ARGBFloat");
        return false;
    }
}

tCLinkage tExport bool tPointsCopyIDsToTexture(aiPointsData *data, void *tex, int width, int height, tTextureFormat fmt)
{
    if (data == nullptr) { return false; }

    if (fmt == tTextureFormat_RInt)
    {
        return tWriteTextureWithConversion(tex, width, height, fmt, data->ids, data->count,
            [](void *dst, const uint64_t *src, size_t len) {
                for (size_t i = 0; i < len; ++i) {
                    ((int32_t*)dst)[i] = (int32_t)(src[i]);
                }
            });
    }
    else if (fmt == tTextureFormat_RFloat)
    {
        return tWriteTextureWithConversion(tex, width, height, fmt, data->ids, data->count,
            [](void *dst, const uint64_t *src, size_t len) {
                for (size_t i = 0; i < len; ++i) {
                    ((float*)dst)[i] = (float)(src[i]);
                }
            });
    }
    else
    {
        tLog("aiPointsCopyIDsToTexture(): format must be RFloat or RInt");
        return false;
    }
}
