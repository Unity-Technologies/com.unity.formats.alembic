#include "pch.h"

#ifdef tSupportD3D11
#include "Foundation.h"
#include "GraphicsDevice.h"
#include <d3d11.h>

const int aiD3D11MaxStagingTextures = 32;


class tGraphicsDeviceD3D11 : public tIGraphicsDevice
{
public:
    tGraphicsDeviceD3D11(void *device);
    ~tGraphicsDeviceD3D11();
    void* getDevicePtr() override;
    int getDeviceType() override;
    bool readTexture(void *outBuf, size_t bufsize, void *tex, int width, int height, tTextureFormat format) override;
    bool writeTexture(void *outTex, int width, int height, tTextureFormat format, const void *buf, size_t bufsize) override;

private:
    void clearStagingTextures();
    ID3D11Texture2D* findOrCreateStagingTexture(int width, int height, tTextureFormat format);

private:
    ID3D11Device *m_device;
    ID3D11DeviceContext *m_context;
    ID3D11Query *m_queryEvent;
    std::map<uint64_t, ID3D11Texture2D*> m_stagingTextures;
};


tIGraphicsDevice* aiCreateGraphicsDeviceD3D11(void *device)
{
    return new tGraphicsDeviceD3D11(device);
}

tGraphicsDeviceD3D11::tGraphicsDeviceD3D11(void *device)
    : m_device((ID3D11Device*)device)
    , m_context(nullptr)
    , m_queryEvent(nullptr)
{
    clearStagingTextures();
    if (m_device != nullptr)
    {
        m_device->GetImmediateContext(&m_context);

        D3D11_QUERY_DESC qdesc = {D3D11_QUERY_EVENT , 0};
        m_device->CreateQuery(&qdesc, &m_queryEvent);
    }
}

tGraphicsDeviceD3D11::~tGraphicsDeviceD3D11()
{
    if (m_context != nullptr)
    {
        m_context->Release();
        m_context = nullptr;

        m_queryEvent->Release();
        m_queryEvent = nullptr;
    }
}

void* tGraphicsDeviceD3D11::getDevicePtr() { return m_device; }
int tGraphicsDeviceD3D11::getDeviceType() { return kGfxRendererD3D11; }


static DXGI_FORMAT aiGetInternalFormatD3D11(tTextureFormat fmt)
{
    switch (fmt)
    {
    case tTextureFormat_ARGB32:    return DXGI_FORMAT_R8G8B8A8_TYPELESS;

    case tTextureFormat_ARGBHalf:  return DXGI_FORMAT_R16G16B16A16_FLOAT;
    case tTextureFormat_RGHalf:    return DXGI_FORMAT_R16G16_FLOAT;
    case tTextureFormat_RHalf:     return DXGI_FORMAT_R16_FLOAT;

    case tTextureFormat_ARGBFloat: return DXGI_FORMAT_R32G32B32A32_FLOAT;
    case tTextureFormat_RGFloat:   return DXGI_FORMAT_R32G32_FLOAT;
    case tTextureFormat_RFloat:    return DXGI_FORMAT_R32_FLOAT;

    case tTextureFormat_ARGBInt:   return DXGI_FORMAT_R32G32B32A32_SINT;
    case tTextureFormat_RGInt:     return DXGI_FORMAT_R32G32_SINT;
    case tTextureFormat_RInt:      return DXGI_FORMAT_R32_SINT;
    }
    return DXGI_FORMAT_UNKNOWN;
}


ID3D11Texture2D* tGraphicsDeviceD3D11::findOrCreateStagingTexture(int width, int height, tTextureFormat format)
{
    if (m_stagingTextures.size() >= aiD3D11MaxStagingTextures) {
        clearStagingTextures();
    }

    DXGI_FORMAT internalFormat = aiGetInternalFormatD3D11(format);
    uint64_t hash = width + (height << 16) + ((uint64_t)internalFormat << 32);
    {
        auto it = m_stagingTextures.find(hash);
        if (it != m_stagingTextures.end())
        {
            return it->second;
        }
    }

    D3D11_TEXTURE2D_DESC desc = {
        (UINT)width, (UINT)height, 1, 1, internalFormat, { 1, 0 },
        D3D11_USAGE_STAGING, 0, D3D11_CPU_ACCESS_READ | D3D11_CPU_ACCESS_WRITE, 0
    };
    ID3D11Texture2D *ret = nullptr;
    HRESULT hr = m_device->CreateTexture2D(&desc, nullptr, &ret);
    if (SUCCEEDED(hr))
    {
        m_stagingTextures.insert(std::make_pair(hash, ret));
    }
    return ret;
}

void tGraphicsDeviceD3D11::clearStagingTextures()
{
    for (auto& pair : m_stagingTextures)
    {
        pair.second->Release();
    }
    m_stagingTextures.clear();
}

bool tGraphicsDeviceD3D11::readTexture(void *outBuf, size_t bufsize, void *tex_, int width, int height, tTextureFormat format)
{
    if (tex_ == nullptr) {
        tLog("aiGraphicsDeviceD3D11::readTexture(): texture is null");
        return false;
    }

    if (m_context == nullptr || tex_ == nullptr) { return false; }
    int psize = tGetPixelSize(format);

    // Unity の D3D11 の RenderTexture の内容は CPU からはアクセス不可能になっている。
    // なので staging texture を用意してそれに内容を移し、CPU はそれ経由でデータを読む。
    ID3D11Texture2D *tex = (ID3D11Texture2D*)tex_;
    ID3D11Texture2D *tmp = findOrCreateStagingTexture(width, height, format);
    m_context->CopyResource(tmp, tex);

    // ID3D11DeviceContext::Map() はその時点までのコマンドの終了を待ってくれないっぽくて、
    // ↑の CopyResource() が終わるのを手動で待たないといけない。
    m_context->End(m_queryEvent);
    while (m_context->GetData(m_queryEvent, nullptr, 0, 0) == S_FALSE) {
        std::this_thread::sleep_for(std::chrono::microseconds(100));
    }

    D3D11_MAPPED_SUBRESOURCE mapped = { 0 };
    HRESULT hr = m_context->Map(tmp, 0, D3D11_MAP_READ, 0, &mapped);
    if (SUCCEEDED(hr))
    {
        char *wpixels = (char*)outBuf;
        int wpitch = width * tGetPixelSize(format);
        const char *rpixels = (const char*)mapped.pData;
        int rpitch = mapped.RowPitch;

        // 表向きの解像度と内部解像度は一致しないことがあるようで、その場合 1 ラインづつコピーする必要がある。
        // (手元の環境では内部解像度は 32 の倍数になるっぽく見える)
        if (wpitch == rpitch)
        {
            memcpy(wpixels, rpixels, bufsize);
        }
        else
        {
            for (int i = 0; i < height; ++i)
            {
                memcpy(wpixels, rpixels, wpitch);
                wpixels += wpitch;
                rpixels += rpitch;
            }
        }

        m_context->Unmap(tex, 0);
        return true;
    }
    return false;
}

bool tGraphicsDeviceD3D11::writeTexture(void *outTex, int width, int height, tTextureFormat format, const void *buf, size_t bufsize)
{
    if (outTex == nullptr) {
        tLog("aiGraphicsDeviceD3D11::writeTexture(): texture is null");
        return false;
    }

    int psize = tGetPixelSize(format);
    int pitch = psize * width;
    const size_t numPixels = bufsize / psize;

    D3D11_BOX box;
    box.left = 0;
    box.right = width;
    box.top = 0;
    box.bottom = ceildiv((UINT)numPixels, (UINT)width);
    box.front = 0;
    box.back = 1;
    ID3D11Texture2D *tex = (ID3D11Texture2D*)outTex;
    m_context->UpdateSubresource(tex, 0, &box, buf, pitch, 0);
    return true;
}

#endif // tSupportD3D11
