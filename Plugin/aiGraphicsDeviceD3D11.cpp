#include "pch.h"
#include "aiGraphicsDevice.h"

#ifdef aiSupportD3D11

inline int ceildiv(int v, int d) { return v / d + (v%d == 0 ? 0 : 1); }
#define aiSafeRelease(obj) if(obj) { obj->Release(); obj=nullptr; }



class aiGraphicsDeviceD3D11 : public aiGraphicsDevice
{
typedef aiGraphicsDevice super;
public:
    aiGraphicsDeviceD3D11(void *dev);
    virtual ~aiGraphicsDeviceD3D11();
    virtual void copyDataToTexture(void *texptr, int width, int height, const void *data, int datasize);

private:
    ID3D11Device        *m_pDevice;
    ID3D11DeviceContext *m_pImmediateContext;
};


aiGraphicsDevice* aiCreateGraphicsDeviceD3D11(void *device)
{
    return new aiGraphicsDeviceD3D11(device);
}


aiGraphicsDeviceD3D11::aiGraphicsDeviceD3D11(void *dev)
    : super(dev, kGfxRendererD3D11)
    , m_pDevice(nullptr)
    , m_pImmediateContext(nullptr)
{
    m_pDevice = (ID3D11Device*)dev;
    m_pDevice->GetImmediateContext(&m_pImmediateContext);
}

aiGraphicsDeviceD3D11::~aiGraphicsDeviceD3D11()
{
    aiSafeRelease(m_pImmediateContext);
}


void aiGraphicsDeviceD3D11::copyDataToTexture(void *texptr, int width, int height, const void *dataptr, int datasize)
{
    ID3D11Texture2D *tex = (ID3D11Texture2D*)texptr;

    D3D11_BOX box;
    box.left = 0;
    box.right = width;
    box.top = 0;
    box.bottom = ceildiv(datasize / (sizeof(float)*4), width);
    box.front = 0;
    box.back = 1;
    m_pImmediateContext->UpdateSubresource(tex, 0, &box, dataptr, width*16, 0);
}

#endif // aiSupportD3D11
