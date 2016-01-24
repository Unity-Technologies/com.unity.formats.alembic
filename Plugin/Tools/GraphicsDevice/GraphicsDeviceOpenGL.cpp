#include "pch.h"

#ifdef tSupportOpenGL
#include "Common.h"
#include "GraphicsDevice.h"

#include <GL/glew.h>
#ifdef _WIN32
#pragma comment(lib, "opengl32.lib")
#pragma comment(lib, "glew32s.lib")
#endif


class tGraphicsDeviceOpenGL : public tIGraphicsDevice
{
public:
    tGraphicsDeviceOpenGL(void *device);
    ~tGraphicsDeviceOpenGL();
    void* getDevicePtr() override;
    int getDeviceType() override;
    bool readTexture(void *outBuf, size_t bufsize, void *tex, int width, int height, tTextureFormat format) override;
    bool writeTexture(void *outTex, int width, int height, tTextureFormat format, const void *buf, size_t bufsize) override;

private:
    void *m_device;
};


tIGraphicsDevice* aiCreateGraphicsDeviceOpenGL(void *device)
{
    return new tGraphicsDeviceOpenGL(device);
}


void* tGraphicsDeviceOpenGL::getDevicePtr() { return m_device; }
int tGraphicsDeviceOpenGL::getDeviceType() { return kGfxRendererOpenGL; }

tGraphicsDeviceOpenGL::tGraphicsDeviceOpenGL(void *device)
    : m_device(device)
{
    glewInit();
}

tGraphicsDeviceOpenGL::~tGraphicsDeviceOpenGL()
{
}


static void fcGetInternalFormatOpenGL(tTextureFormat format, GLenum &outFmt, GLenum &outType)
{
    switch (format)
    {
    case tTextureFormat_ARGB32:    outFmt = GL_RGBA; outType = GL_UNSIGNED_BYTE; return;

    case tTextureFormat_ARGBHalf:  outFmt = GL_RGBA; outType = GL_HALF_FLOAT; return;
    case tTextureFormat_RGHalf:    outFmt = GL_RG; outType = GL_HALF_FLOAT; return;
    case tTextureFormat_RHalf:     outFmt = GL_RED; outType = GL_HALF_FLOAT; return;

    case tTextureFormat_ARGBFloat: outFmt = GL_RGBA; outType = GL_FLOAT; return;
    case tTextureFormat_RGFloat:   outFmt = GL_RG; outType = GL_FLOAT; return;
    case tTextureFormat_RFloat:    outFmt = GL_RED; outType = GL_FLOAT; return;

    case tTextureFormat_ARGBInt:   outFmt = GL_RGBA_INTEGER; outType = GL_INT; return;
    case tTextureFormat_RGInt:     outFmt = GL_RG_INTEGER; outType = GL_INT; return;
    case tTextureFormat_RInt:      outFmt = GL_RED_INTEGER; outType = GL_INT; return;
    }
}

bool tGraphicsDeviceOpenGL::readTexture(void *outBuf, size_t bufsize, void *tex, int width, int height, tTextureFormat format)
{
    GLenum internalFormat = 0;
    GLenum internalType = 0;
    fcGetInternalFormatOpenGL(format, internalFormat, internalType);

    //// glGetTextureImage() is available only OpenGL 4.5 or later...
    // glGetTextureImage((GLuint)(size_t)tex, 0, internalFormat, internalType, bufsize, outBuf);

    glFinish();
    glBindTexture(GL_TEXTURE_2D, (GLuint)(size_t)tex);
    glGetTexImage(GL_TEXTURE_2D, 0, internalFormat, internalType, outBuf);
    glBindTexture(GL_TEXTURE_2D, 0);
    return true;
}

bool tGraphicsDeviceOpenGL::writeTexture(void *outTex, int width, int height, tTextureFormat format, const void *buf, size_t bufsize)
{
    GLenum internalFormat = 0;
    GLenum internalType = 0;
    fcGetInternalFormatOpenGL(format, internalFormat, internalType);

    //// glTextureSubImage2D() is available only OpenGL 4.5 or later...
    // glTextureSubImage2D((GLuint)(size_t)outTex, 0, 0, 0, width, height, internalFormat, internalType, buf);

    glBindTexture(GL_TEXTURE_2D, (GLuint)(size_t)outTex);
    glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, width, height, internalFormat, internalType, buf);
    glBindTexture(GL_TEXTURE_2D, 0);
    return true;
}

#endif // tSupportOpenGL
