#include "pch.h"

#if defined(aiSupportTexture)
#include "AlembicImporter.h"
#include "aiGraphicsDevice.h"


int aiGetPixelSize(aiTextureFormat format)
{
    switch (format)
    {
    case aiTextureFormat_ARGB32:    return 4;

    case aiTextureFormat_ARGBHalf:  return 8;
    case aiTextureFormat_RGHalf:    return 4;
    case aiTextureFormat_RHalf:     return 2;

    case aiTextureFormat_ARGBFloat: return 16;
    case aiTextureFormat_RGFloat:   return 8;
    case aiTextureFormat_RFloat:    return 4;

    case aiTextureFormat_ARGBInt:   return 16;
    case aiTextureFormat_RGInt:     return 8;
    case aiTextureFormat_RInt:      return 4;
    }
    return 0;
}


namespace {
    thread_local std::vector<char> *g_conversion_buffer;
}

void* aiGetConversionBuffer(size_t size)
{
    if (g_conversion_buffer == nullptr)
    {
        g_conversion_buffer = new std::vector<char>();
    }
    g_conversion_buffer->resize(size);
    return &(*g_conversion_buffer)[0];
}



aiIGraphicsDevice* aiCreateGraphicsDeviceOpenGL(void *device);
aiIGraphicsDevice* aiCreateGraphicsDeviceD3D9(void *device);
aiIGraphicsDevice* aiCreateGraphicsDeviceD3D11(void *device);


aiIGraphicsDevice *g_theGraphicsDevice;
aiCLinkage aiExport aiIGraphicsDevice* aiGetGraphicsDevice() { return g_theGraphicsDevice; }
typedef aiIGraphicsDevice* (*aiGetGraphicsDeviceT)();


aiCLinkage aiExport void UnitySetGraphicsDevice(void* device, int deviceType, int eventType)
{
    if (eventType == kGfxDeviceEventInitialize) {
#ifdef aiSupportD3D9
        if (deviceType == kGfxRendererD3D9)
        {
            g_theGraphicsDevice = aiCreateGraphicsDeviceD3D9(device);
        }
#endif // aiSupportD3D9
#ifdef aiSupportD3D11
        if (deviceType == kGfxRendererD3D11)
        {
            g_theGraphicsDevice = aiCreateGraphicsDeviceD3D11(device);
        }
#endif // aiSupportD3D11
#ifdef aiSupportOpenGL
        if (deviceType == kGfxRendererOpenGL)
        {
            g_theGraphicsDevice = aiCreateGraphicsDeviceOpenGL(device);
        }
#endif // aiSupportOpenGL
    }

    if (eventType == kGfxDeviceEventShutdown) {
        delete g_theGraphicsDevice;
        g_theGraphicsDevice = nullptr;
    }
}

aiCLinkage aiExport void UnityRenderEvent(int eventID)
{
}


#ifdef aiSupportOpenGL
aiCLinkage aiExport void aiInitializeOpenGL()
{
    UnitySetGraphicsDevice(nullptr, kGfxRendererOpenGL, kGfxDeviceEventInitialize);
}
#endif

#ifdef aiSupportD3D9
aiCLinkage aiExport void aiInitializeD3D9(void *device)
{
    UnitySetGraphicsDevice(device, kGfxRendererD3D9, kGfxDeviceEventInitialize);
}
#endif

#ifdef aiSupportD3D11
aiCLinkage aiExport void aiInitializeD3D11(void *device)
{
    UnitySetGraphicsDevice(device, kGfxRendererD3D11, kGfxDeviceEventInitialize);
}
#endif

aiCLinkage aiExport void aiFinalizeGraphicsDevice()
{
    UnitySetGraphicsDevice(nullptr, kGfxRendererNull, kGfxDeviceEventShutdown);
}



#if !defined(aiMaster) && defined(aiWindows)

// PatchLibrary で突っ込まれたモジュールは UnitySetGraphicsDevice() が呼ばれないので、
// DLL_PROCESS_ATTACH のタイミングで先にロードされているモジュールからデバイスをもらって同等の処理を行う。
BOOL WINAPI DllMain(HINSTANCE, DWORD reasonForCall, LPVOID reserved)
{
    if (reasonForCall == DLL_PROCESS_ATTACH)
    {
        HMODULE m = ::GetModuleHandleA("AlembicImporter.dll");
        if (m) {
            auto proc = (aiGetGraphicsDeviceT)::GetProcAddress(m, "aiGetGraphicsDevice");
            if (proc) {
                aiIGraphicsDevice *dev = proc();
                if (dev) {
                    UnitySetGraphicsDevice(dev->getDevicePtr(), dev->getDeviceType(), kGfxDeviceEventInitialize);
                }
            }
        }
    }
    else if (reasonForCall == DLL_PROCESS_DETACH)
    {
    }
    return TRUE;
}

// "DllMain already defined in MSVCRT.lib" 対策
#ifdef _X86_
extern "C" { int _afxForceUSRDLL; }
#else
extern "C" { int __afxForceUSRDLL; }
#endif

#endif

#endif 
