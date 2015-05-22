#include "pch.h"
#include "AlembicImporter.h"
#include "aiGraphicsDevice.h"

aiGraphicsDevice::aiGraphicsDevice(void *device, int device_type)
    : m_device(device), m_device_type(device_type) {}
aiGraphicsDevice::~aiGraphicsDevice() {}
void* aiGraphicsDevice::getDevicePtr() { return m_device; }
int aiGraphicsDevice::getDeviceType() { return m_device_type; }




aiGraphicsDevice *g_aiGraphicsDevice;

aiCLinkage aiExport aiGraphicsDevice* aiGetGraphicsDevice() { return g_aiGraphicsDevice; }
typedef aiGraphicsDevice* (*aiGetGraphicsDeviceT)();


aiCLinkage aiExport void UnitySetGraphicsDevice(void* device, int deviceType, int eventType)
{
    if (device == nullptr) { return; }

    if (eventType == kGfxDeviceEventInitialize) {
#ifdef aiSupportD3D9
        if (deviceType == kGfxRendererD3D9)
        {
            // todo
        }
#endif // aiSupportD3D9
#ifdef aiSupportD3D11
        if (deviceType == kGfxRendererD3D11)
        {
            g_aiGraphicsDevice = aiCreateGraphicsDeviceD3D11(device);
        }
#endif // aiSupportD3D11
#ifdef aiSupportOpenGL
        if (deviceType == kGfxRendererOpenGL)
        {
            g_aiGraphicsDevice = aiCreateGraphicsDeviceOpenGL(device);
        }
#endif // aiSupportOpenGL
    }

    if (eventType == kGfxDeviceEventShutdown) {
        delete g_aiGraphicsDevice;
        g_aiGraphicsDevice = nullptr;
    }
}

aiCLinkage aiExport void UnityRenderEvent(int eventID)
{
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
                aiGraphicsDevice *dev = proc();
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

#endif // aiMaster
