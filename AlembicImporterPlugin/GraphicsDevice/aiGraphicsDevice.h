#include "pch.h"

#if defined(aiSupportTexture)

// Graphics device identifiers in Unity
enum GfxDeviceRenderer
{
    kGfxRendererOpenGL = 0,          // OpenGL
    kGfxRendererD3D9,                // Direct3D 9
    kGfxRendererD3D11,               // Direct3D 11
    kGfxRendererGCM,                 // Sony PlayStation 3 GCM
    kGfxRendererNull,                // "null" device (used in batch mode)
    kGfxRendererHollywood,           // Nintendo Wii
    kGfxRendererXenon,               // Xbox 360
    kGfxRendererOpenGLES,            // OpenGL ES 1.1
    kGfxRendererOpenGLES20Mobile,    // OpenGL ES 2.0 mobile variant
    kGfxRendererMolehill,            // Flash 11 Stage3D
    kGfxRendererOpenGLES20Desktop,   // OpenGL ES 2.0 desktop variant (i.e. NaCl)
    kGfxRendererCount
};

// Event types for UnitySetGraphicsDevice
enum GfxDeviceEventType {
    kGfxDeviceEventInitialize = 0,
    kGfxDeviceEventShutdown,
    kGfxDeviceEventBeforeReset,
    kGfxDeviceEventAfterReset,
};


enum aiRenderTextureFormat
{
    aiE_ARGB32 = 0,
    aiE_Depth = 1,
    aiE_ARGBHalf = 2,
    aiE_Shadowmap = 3,
    aiE_RGB565 = 4,
    aiE_ARGB4444 = 5,
    aiE_ARGB1555 = 6,
    aiE_Default = 7,
    aiE_ARGB2101010 = 8,
    aiE_DefaultHDR = 9,
    aiE_ARGBFloat = 11,
    aiE_RGFloat = 12,
    aiE_RGHalf = 13,
    aiE_RFloat = 14,
    aiE_RHalf = 15,
    aiE_R8 = 16,
    aiE_ARGBInt = 17,
    aiE_RGInt = 18,
    aiE_RInt = 19,
};

class aiIGraphicsDevice
{
public:
    virtual ~aiIGraphicsDevice() {}
    virtual void* getDevicePtr() = 0;
    virtual int getDeviceType() = 0;
    virtual bool readTexture(void *o_buf, size_t bufsize, void *tex, int width, int height, aiRenderTextureFormat format) = 0;
    virtual bool writeTexture(void *o_tex, int width, int height, aiRenderTextureFormat format, const void *data, size_t datasize) = 0;
};
aiCLinkage aiExport aiIGraphicsDevice* aiGetGraphicsDevice();
int aiGetPixelSize(aiRenderTextureFormat format);
void* aiGetConversionBuffer(size_t size); // return thread-local buffer

// T: input data type
// F: convert function
//   [](void *dst, const T *src, int nth_element)
//   example: float3 to float4
//     [](void *dst, const abcV3 *src, int i) {
//         ((abcV4*)dst)[i] = abcV4(src[i].x, src[i].y, src[i].z, 0.0f);
//     }
template<class T, class F>
inline bool aiWriteTextureWithConversion(void *o_tex, int width, int height, aiRenderTextureFormat format, const T *data, size_t num_elements, const F& converter)
{
    auto dev = aiGetGraphicsDevice();
    if (dev == nullptr) { return false; }

    size_t bufsize = width * height * aiGetPixelSize(format);
    void *buf = aiGetConversionBuffer(bufsize);
    for (size_t i = 0; i < num_elements; ++i) {
        converter(buf, data, i);
    }
    return dev->writeTexture(o_tex, width, height, format, buf, bufsize);
}

template<class IntType> inline IntType ceildiv(IntType a, IntType b) { return a / b + (a%b == 0 ? 0 : 1); }
template<class IntType> inline IntType ceilup(IntType a, IntType b) { return ceildiv(a, b) * b; }

#endif
