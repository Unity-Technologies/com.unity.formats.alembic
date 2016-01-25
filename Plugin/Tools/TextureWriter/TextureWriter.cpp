#include "pch.h"
#include "Foundation.h"
#include "GraphicsDevice.h"

#include "half.h"
#pragma comment(lib, "Half.lib")
typedef tvec1<half> half1;
typedef tvec2<half> half2;
typedef tvec3<half> half3;
typedef tvec4<half> half4;

template<> inline half tScalar(int src) { return half((float)src); }
template<> inline half tScalar(lint src) { return half((float)src); }


template<class DstType, class SrcType>
struct tConvertArrayImpl
{
    void operator()(void *&dst_, const void *src_, size_t num_elements)
    {
        DstType *dst = (DstType*)dst_;
        const SrcType *src = (const SrcType*)src_;
        for (size_t i = 0; i < num_elements; ++i) {
            dst[i] = DstType(src[i]);
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
    tDataFormat_Half1,
    tDataFormat_Half2,
    tDataFormat_Half3,
    tDataFormat_Half4,
    tDataFormat_Float1,
    tDataFormat_Float2,
    tDataFormat_Float3,
    tDataFormat_Float4,
    tDataFormat_Int1,
    tDataFormat_Int2,
    tDataFormat_Int3,
    tDataFormat_Int4,
    tDataFormat_LInt1, // 64bit int
};

template<tDataFormat F> struct tDataFormatToType;
template<tTextureFormat F> struct tTextureFormatToType;

#define Def(Enum, Type) template<> struct tDataFormatToType<Enum> { typedef Type type; };
Def(tDataFormat_Half1, half1)
Def(tDataFormat_Half2, half2)
Def(tDataFormat_Half3, half3)
Def(tDataFormat_Half4, half4)
Def(tDataFormat_Float1, float1)
Def(tDataFormat_Float2, float2)
Def(tDataFormat_Float3, float3)
Def(tDataFormat_Float4, float4)
Def(tDataFormat_Int1, int1)
Def(tDataFormat_Int2, int2)
Def(tDataFormat_Int3, int3)
Def(tDataFormat_Int4, int4)
Def(tDataFormat_LInt1, lint1)
#undef Def

#define Def(Enum, Type) template<> struct tTextureFormatToType<Enum> { typedef Type type; };
Def(tTextureFormat_RHalf, half1)
Def(tTextureFormat_RGHalf, half2)
Def(tTextureFormat_ARGBHalf, half4)
Def(tTextureFormat_RFloat, float1)
Def(tTextureFormat_RGFloat, float2)
Def(tTextureFormat_ARGBFloat, float4)
Def(tTextureFormat_RInt, int1)
Def(tTextureFormat_RGInt, int2)
Def(tTextureFormat_ARGBInt, int4)
#undef Def


tCLinkage tExport int tWriteTexture(
    void *dst_tex, int dst_width, int dst_height, tTextureFormat dst_fmt,
    const void *src, int src_num, tDataFormat src_fmt)
{
    if (dst_tex == nullptr || src == nullptr) { return 0; }
    if (src_num == 0) { return 1; }

#define TCase(Dst, Src)\
    case Src: return (int)tWriteTextureWithConversion(dst_tex, dst_width, dst_height, dst_fmt, src, src_num,\
        tConvertArray<tTextureFormatToType<Dst>::type, tDataFormatToType<Src>::type>)\

#define TBlock(Dst)\
    case Dst:\
        switch (src_fmt)\
        {\
            TCase(Dst, tDataFormat_Half1);\
            TCase(Dst, tDataFormat_Half2);\
            TCase(Dst, tDataFormat_Half3);\
            TCase(Dst, tDataFormat_Half4);\
            TCase(Dst, tDataFormat_Float1);\
            TCase(Dst, tDataFormat_Float2);\
            TCase(Dst, tDataFormat_Float3);\
            TCase(Dst, tDataFormat_Float4);\
            TCase(Dst, tDataFormat_Int1);\
            TCase(Dst, tDataFormat_Int2);\
            TCase(Dst, tDataFormat_Int3);\
            TCase(Dst, tDataFormat_Int4);\
            TCase(Dst, tDataFormat_LInt1);\
        } break;\

    switch (dst_fmt)
    {
        TBlock(tTextureFormat_RHalf)
        TBlock(tTextureFormat_RGHalf)
        TBlock(tTextureFormat_ARGBHalf)
        TBlock(tTextureFormat_RFloat)
        TBlock(tTextureFormat_RGFloat)
        TBlock(tTextureFormat_ARGBFloat)
        TBlock(tTextureFormat_RInt)
        TBlock(tTextureFormat_RGInt)
        TBlock(tTextureFormat_ARGBInt)
    }

#undef TBlock
#undef TCase

    tLog("tWriteTexture(): this format combination is not supported %d - %d", dst_fmt, src_fmt);
    return 0;
}



// async API
#include "Concurrency.h"

namespace {
    ist::task_group g_task_group;
}

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
