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
    template<class U> tvec2(const tvec2<U>& src);
    template<class U> tvec2(const tvec3<U>& src);
    template<class U> tvec2(const tvec4<U>& src);
};

template<class T> struct tvec3
{
    T x, y, z;
    tvec3() {} // !not clear members!
    tvec3(T a) : x(a), y(a), z(a) {}
    tvec3(T a, T b, T c) : x(a), y(b), z(c) {}
    template<class U> tvec3(const tvec2<U>& src);
    template<class U> tvec3(const tvec3<U>& src);
    template<class U> tvec3(const tvec4<U>& src);
};

template<class T> struct tvec4
{
    T x, y, z, w;
    tvec4() {} // !not clear members!
    tvec4(T a) : x(a), y(a), z(a), w(a) {}
    tvec4(T a, T b, T c, T d) : x(a), y(b), z(c), w(d) {}
    template<class U> tvec4(const tvec2<U>& src);
    template<class U> tvec4(const tvec3<U>& src);
    template<class U> tvec4(const tvec4<U>& src);
};

template<class DstType, class SrcType> inline DstType tScalar(SrcType src) { return DstType(src); }
template<> inline half tScalar<>(int src) { return half((float)src); }

template<class T> template<class U> tvec2<T>::tvec2(const tvec2<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)) {}
template<class T> template<class U> tvec2<T>::tvec2(const tvec3<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)) {}
template<class T> template<class U> tvec2<T>::tvec2(const tvec4<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)) {}
template<class T> template<class U> tvec3<T>::tvec3(const tvec2<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)), z() {}
template<class T> template<class U> tvec3<T>::tvec3(const tvec3<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)), z(tScalar<T>(src.z)) {}
template<class T> template<class U> tvec3<T>::tvec3(const tvec4<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)), z(tScalar<T>(src.z)) {}
template<class T> template<class U> tvec4<T>::tvec4(const tvec2<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)), z(), w() {}
template<class T> template<class U> tvec4<T>::tvec4(const tvec3<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)), z(tScalar<T>(src.z)), w() {}
template<class T> template<class U> tvec4<T>::tvec4(const tvec4<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)), z(tScalar<T>(src.z)), w(tScalar<T>(src.w)) {}

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
template<>
inline void tConvert<half, int>(half& dst, const int& src)
{
    dst = half((float)src); // prevent warning
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


tCLinkage tExport int tWriteTexture(
    void *dst_tex, int dst_width, int dst_height, tTextureFormat dst_fmt,
    const void *src, int src_num, tDataFormat src_fmt)
{
    if (dst_tex == nullptr || src == nullptr) { return 0; }
    if (src_num == 0) { return 1; }

#define TConvert(A1, A2)\
    case A2: return (int)tWriteTextureWithConversion(dst_tex, dst_width, dst_height, dst_fmt, src, src_num,\
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

    tLog("tWriteTexture(): this format combination is not supported %d - %d", dst_fmt, src_fmt);
    return 0;
}



// async API
#include "Concurrency.h"

ist::task_group g_task_group;

struct tWriteTextureTask
{
    bool task_completed;
    int result;

    tWriteTextureTask() : task_completed(false), result(0) {}
};

tCLinkage tExport tWriteTextureTask* tWriteTextureBegin(
    void *dst_tex, int dst_width, int dst_height, tTextureFormat dst_fmt,
    const void *src, int src_num, tDataFormat src_fmt)
{
    tWriteTextureTask *task = new tWriteTextureTask();

    g_task_group.run([=]() {
        task->result = tWriteTexture(dst_tex, dst_width, dst_height, dst_fmt, src, src_num, src_fmt);
        task->task_completed = true;
    });

    return task;
}

tCLinkage tExport int tWriteTextureEnd(tWriteTextureTask *task)
{
    while (!task->task_completed) {
        std::this_thread::yield();
    }

    int ret = task->result;
    delete task;
    return ret;
}

tCLinkage tExport int tWriteTextureIsComplete(tWriteTextureTask *task)
{
    return task->task_completed ? 1 : 0;
}
