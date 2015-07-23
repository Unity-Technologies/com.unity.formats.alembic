
class aiGraphicsDevice
{
public:
    aiGraphicsDevice(void *device, int device_type);
    virtual ~aiGraphicsDevice();
    void* getDevicePtr();
    int getDeviceType();
    virtual void copyDataToTexture(void *texptr, int width, int height, const void *data, int datasize) = 0;

private:
    void *m_device;
    int m_deviceType;
};

#ifdef aiSupportD3D11
aiGraphicsDevice* aiCreateGraphicsDeviceD3D11(void *device);
#endif

#ifdef aiSupportOpenGL
aiGraphicsDevice* aiCreateGraphicsDeviceOpenGL(void *device);
#endif



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
