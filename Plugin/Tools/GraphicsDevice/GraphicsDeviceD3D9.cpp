#include "pch.h"

#ifdef tSupportD3D9
#include "Common.h"
#include "GraphicsDevice.h"
#include <d3d9.h>

const int aiD3D9MaxStagingTextures = 32;

class aiGraphicsDeviceD3D9 : public tIGraphicsDevice
{
public:
    aiGraphicsDeviceD3D9(void *device);
    ~aiGraphicsDeviceD3D9();
    void* getDevicePtr() override;
    int getDeviceType() override;
    bool readTexture(void *outBuf, size_t bufsize, void *tex, int width, int height, tTextureFormat format) override;
    bool writeTexture(void *outTex, int width, int height, tTextureFormat format, const void *buf, size_t bufsize) override;

private:
    void clearStagingTextures();
    IDirect3DSurface9* findOrCreateStagingTexture(int width, int height, tTextureFormat format);

private:
    IDirect3DDevice9 *m_device;
    std::map<uint64_t, IDirect3DSurface9*> m_stagingTextures;
};


tIGraphicsDevice* aiCreateGraphicsDeviceD3D9(void *device)
{
    return new aiGraphicsDeviceD3D9(device);
}


aiGraphicsDeviceD3D9::aiGraphicsDeviceD3D9(void *device)
    : m_device((IDirect3DDevice9*)device)
{
}

aiGraphicsDeviceD3D9::~aiGraphicsDeviceD3D9()
{
    clearStagingTextures();
}

void* aiGraphicsDeviceD3D9::getDevicePtr() { return m_device; }
int aiGraphicsDeviceD3D9::getDeviceType() { return kGfxRendererD3D9; }


void aiGraphicsDeviceD3D9::clearStagingTextures()
{
    for (auto& pair : m_stagingTextures)
    {
        pair.second->Release();
    }
    m_stagingTextures.clear();
}



static D3DFORMAT aiGetInternalFormatD3D9(tTextureFormat fmt)
{
    switch (fmt)
    {
    case tTextureFormat_ARGB32:    return D3DFMT_A8R8G8B8;

    case tTextureFormat_ARGBHalf:  return D3DFMT_A16B16G16R16F;
    case tTextureFormat_RGHalf:    return D3DFMT_G16R16F;
    case tTextureFormat_RHalf:     return D3DFMT_R16F;

    case tTextureFormat_ARGBFloat: return D3DFMT_A32B32G32R32F;
    case tTextureFormat_RGFloat:   return D3DFMT_G32R32F;
    case tTextureFormat_RFloat:    return D3DFMT_R32F;
    }
    return D3DFMT_UNKNOWN;
}

IDirect3DSurface9* aiGraphicsDeviceD3D9::findOrCreateStagingTexture(int width, int height, tTextureFormat format)
{
    if (m_stagingTextures.size() >= aiD3D9MaxStagingTextures) {
        clearStagingTextures();
    }

    D3DFORMAT internalFormat = aiGetInternalFormatD3D9(format);
    if (internalFormat == D3DFMT_UNKNOWN) { return nullptr; }

    uint64_t hash = width + (height << 16) + ((uint64_t)internalFormat << 32);
    {
        auto it = m_stagingTextures.find(hash);
        if (it != m_stagingTextures.end())
        {
            return it->second;
        }
    }

    IDirect3DSurface9 *ret = nullptr;
    HRESULT hr = m_device->CreateOffscreenPlainSurface(width, height, internalFormat, D3DPOOL_SYSTEMMEM, &ret, NULL);
    if (SUCCEEDED(hr))
    {
        m_stagingTextures.insert(std::make_pair(hash, ret));
    }
    return ret;
}


bool aiGraphicsDeviceD3D9::readTexture(void *outBuf, size_t bufsize, void *tex_, int width, int height, tTextureFormat format)
{
    HRESULT hr;
    IDirect3DTexture9 *tex = (IDirect3DTexture9*)tex_;

    // D3D11 と同様 render target の内容は CPU からはアクセス不可能になっている。
    // staging texture を用意してそれに内容を移し、CPU はそれ経由でデータを読む。
    IDirect3DSurface9 *surfDst = findOrCreateStagingTexture(width, height, format);
    if (surfDst == nullptr) { return false; }

    IDirect3DSurface9* surfSrc = nullptr;
    hr = tex->GetSurfaceLevel(0, &surfSrc);
    if (FAILED(hr)){ return false; }

    bool ret = false;
    hr = m_device->GetRenderTargetData(surfSrc, surfDst);
    if (SUCCEEDED(hr))
    {
        D3DLOCKED_RECT locked;
        hr = surfDst->LockRect(&locked, nullptr, D3DLOCK_READONLY);
        if (SUCCEEDED(hr))
        {
            char *wpixels = (char*)outBuf;
            int wpitch = width * tGetPixelSize(format);
            const char *rpixels = (const char*)locked.pBits;
            int rpitch = locked.Pitch;

            // D3D11 と同様表向き解像度と内部解像度が違うケースを考慮
            // (しかし、少なくとも手元の環境では常に wpitch == rpitch っぽい)
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
            surfDst->UnlockRect();

            // D3D9 の ARGB32 のピクセルの並びは BGRA になっているので並べ替える
            if (format == tTextureFormat_ARGB32) {
                BGRA2RGBA((RGBA<uint8_t>*)outBuf, int(bufsize / 4));
            }
            ret = true;
        }
    }

    surfSrc->Release();
    return ret;
}

bool aiGraphicsDeviceD3D9::writeTexture(void *outTex, int width, int height, tTextureFormat format, const void *buf, size_t bufsize)
{
    int psize = tGetPixelSize(format);
    int pitch = psize * width;
    const size_t numPixels = bufsize / psize;

    HRESULT hr;
    IDirect3DTexture9 *tex = (IDirect3DTexture9*)outTex;

    // D3D11 と違い、D3D9 では書き込みも staging texture を経由する必要がある。
    IDirect3DSurface9 *surfSrc = findOrCreateStagingTexture(width, height, format);
    if (surfSrc == nullptr) { return false; }

    IDirect3DSurface9* surfDst = nullptr;
    hr = tex->GetSurfaceLevel(0, &surfDst);
    if (FAILED(hr)){ return false; }

    bool ret = false;
    D3DLOCKED_RECT locked;
    hr = surfSrc->LockRect(&locked, nullptr, D3DLOCK_DISCARD);
    if (SUCCEEDED(hr))
    {
        const char *rpixels = (const char*)buf;
        int rpitch = psize * width;
        char *wpixels = (char*)locked.pBits;
        int wpitch = locked.Pitch;

        // こちらも ARGB32 の場合 BGRA に並べ替える必要がある
        if (format == tTextureFormat_ARGB32) {
            CopyWithBGRA2RGBA((RGBA<uint8_t>*)wpixels, (RGBA<uint8_t>*)rpixels, int(bufsize / 4));
        }
        else {
            memcpy(wpixels, rpixels, bufsize);
        }
        surfSrc->UnlockRect();

        hr = m_device->UpdateSurface(surfSrc, nullptr, surfDst, nullptr);
        if (SUCCEEDED(hr)) {
            ret = true;
        }
    }
    surfDst->Release();

    return false;
}

#endif // tSupportD3D9
