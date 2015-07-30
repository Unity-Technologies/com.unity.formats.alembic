#include "pch.h"

#if defined(aiSupportTextureMesh)

#include "aiGraphicsDevice.h"


int aiGetPixelSize(aiETextureFormat format)
{
    switch (format)
    {
    case aiE_ARGB32:    return 4;

    case aiE_ARGBHalf:  return 8;
    case aiE_RGHalf:    return 4;
    case aiE_RHalf:     return 2;

    case aiE_ARGBFloat: return 16;
    case aiE_RGFloat:   return 8;
    case aiE_RFloat:    return 4;

    case aiE_ARGBInt:   return 16;
    case aiE_RGInt:     return 8;
    case aiE_RInt:      return 4;
    default: break;
    }
    return 0;
}


aiIGraphicsDevice* aiCreateGraphicsDeviceOpenGL(void *device);
aiIGraphicsDevice* aiCreateGraphicsDeviceD3D9(void *device);
aiIGraphicsDevice* aiCreateGraphicsDeviceD3D11(void *device);


aiIGraphicsDevice *g_the_graphics_device;
aiCLinkage aiExport aiIGraphicsDevice* aiGetGraphicsDevice() { return g_the_graphics_device; }
typedef aiIGraphicsDevice* (*aiGetGraphicsDeviceT)();


aiCLinkage aiExport void UnitySetGraphicsDevice(void* device, int deviceType, int eventType)
{
    if (eventType == kGfxDeviceEventInitialize) {
#ifdef aiSupportD3D9
        if (deviceType == kGfxRendererD3D9)
        {
            g_the_graphics_device = aiCreateGraphicsDeviceD3D9(device);
        }
#endif // aiSupportD3D9
#ifdef aiSupportD3D11
        if (deviceType == kGfxRendererD3D11)
        {
            g_the_graphics_device = aiCreateGraphicsDeviceD3D11(device);
        }
#endif // aiSupportD3D11
#ifdef aiSupportOpenGL
        if (deviceType == kGfxRendererOpenGL)
        {
            g_the_graphics_device = aiCreateGraphicsDeviceOpenGL(device);
        }
#endif // aiSupportOpenGL
    }

    if (eventType == kGfxDeviceEventShutdown) {
        delete g_the_graphics_device;
        g_the_graphics_device = nullptr;
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
BOOL WINAPI DllMain(HINSTANCE module_handle, DWORD reason_for_call, LPVOID reserved)
{
    if (reason_for_call == DLL_PROCESS_ATTACH)
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
    else if (reason_for_call == DLL_PROCESS_DETACH)
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
