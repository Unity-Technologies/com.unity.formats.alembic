#include "pch.h"

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


enum tTextureFormat
{
    tTextureFormat_Unknown,
    tTextureFormat_ARGB32,
    tTextureFormat_ARGB2101010,
    tTextureFormat_RHalf,
    tTextureFormat_RGHalf,
    tTextureFormat_ARGBHalf,
    tTextureFormat_RFloat,
    tTextureFormat_RGFloat,
    tTextureFormat_ARGBFloat,
    tTextureFormat_RInt,
    tTextureFormat_RGInt,
    tTextureFormat_ARGBInt,
};

class tIGraphicsDevice
{
public:
    virtual ~tIGraphicsDevice() {}
    virtual void* getDevicePtr() = 0;
    virtual int getDeviceType() = 0;
    virtual bool readTexture(void *o_buf, size_t bufsize, void *tex, int width, int height, tTextureFormat format) = 0;
    virtual bool writeTexture(void *o_tex, int width, int height, tTextureFormat format, const void *data, size_t datasize) = 0;
};
tCLinkage tExport tIGraphicsDevice* tGetGraphicsDevice();


int tGetPixelSize(tTextureFormat format);
void* tGetConversionBuffer(size_t size); // return thread-local buffer

// T: input data type
// F: convert function
//   [](void *dst, const T *src, int nth_element)
//   example: float3 to float4
//     [](void *dst, const abcV3 *src, int i) {
//         ((abcV4*)dst)[i] = abcV4(src[i].x, src[i].y, src[i].z, 0.0f);
//     }
template<class T, class F>
inline bool tWriteTextureWithConversion(void *o_tex, int width, int height, tTextureFormat format, const T *data, size_t num_elements, const F& converter)
{
    auto dev = tGetGraphicsDevice();
    if (dev == nullptr) { return false; }

    size_t bufsize = width * height * tGetPixelSize(format);
    void *buf = tGetConversionBuffer(bufsize);
    for (size_t i = 0; i < num_elements; ++i) {
        converter(buf, data, (int)i);
    }
    return dev->writeTexture(o_tex, width, height, format, buf, bufsize);
}
