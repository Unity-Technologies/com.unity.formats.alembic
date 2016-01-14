#include "pch.h"

#if defined(aiSupportTexture) && defined(aiSupportOpenGL)
#include "AlembicImporter.h"
#include "aiGraphicsDevice.h"
#include "aiLogger.h"
#include "aiMisc.h"

#ifndef aiDontForceStaticGLEW
#define GLEW_STATIC
#endif

#include <GL/glew.h>

#if defined(aiWindows) && !defined(aiNoAutoLink)
#pragma comment(lib, "opengl32.lib")
#pragma comment(lib, "glew32s.lib")
#endif


class aiGraphicsDeviceOpenGL : public aiIGraphicsDevice
{
public:
    aiGraphicsDeviceOpenGL(void *device);
    ~aiGraphicsDeviceOpenGL();
    void* getDevicePtr() override;
    int getDeviceType() override;
    bool readTexture(void *outBuf, size_t bufsize, void *tex, int width, int height, aiTextureFormat format) override;
    bool writeTexture(void *outTex, int width, int height, aiTextureFormat format, const void *buf, size_t bufsize) override;

private:
    void *m_device;
};


aiIGraphicsDevice* aiCreateGraphicsDeviceOpenGL(void *device)
{
    return new aiGraphicsDeviceOpenGL(device);
}


void* aiGraphicsDeviceOpenGL::getDevicePtr() { return m_device; }
int aiGraphicsDeviceOpenGL::getDeviceType() { return kGfxRendererOpenGL; }

aiGraphicsDeviceOpenGL::aiGraphicsDeviceOpenGL(void *device)
    : m_device(device)
{
    glewInit();
}

aiGraphicsDeviceOpenGL::~aiGraphicsDeviceOpenGL()
{
}


static void fcGetInternalFormatOpenGL(aiTextureFormat format, GLenum &outFmt, GLenum &outType)
{
    switch (format)
    {
    case aiTextureFormat_ARGB32:    outFmt = GL_RGBA; outType = GL_UNSIGNED_BYTE; return;

    case aiTextureFormat_ARGBHalf:  outFmt = GL_RGBA; outType = GL_HALF_FLOAT; return;
    case aiTextureFormat_RGHalf:    outFmt = GL_RG; outType = GL_HALF_FLOAT; return;
    case aiTextureFormat_RHalf:     outFmt = GL_RED; outType = GL_HALF_FLOAT; return;

    case aiTextureFormat_ARGBFloat: outFmt = GL_RGBA; outType = GL_FLOAT; return;
    case aiTextureFormat_RGFloat:   outFmt = GL_RG; outType = GL_FLOAT; return;
    case aiTextureFormat_RFloat:    outFmt = GL_RED; outType = GL_FLOAT; return;

    case aiTextureFormat_ARGBInt:   outFmt = GL_RGBA_INTEGER; outType = GL_INT; return;
    case aiTextureFormat_RGInt:     outFmt = GL_RG_INTEGER; outType = GL_INT; return;
    case aiTextureFormat_RInt:      outFmt = GL_RED_INTEGER; outType = GL_INT; return;
    }
}

bool aiGraphicsDeviceOpenGL::readTexture(void *outBuf, size_t bufsize, void *tex, int width, int height, aiTextureFormat format)
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

bool aiGraphicsDeviceOpenGL::writeTexture(void *outTex, int width, int height, aiTextureFormat format, const void *buf, size_t bufsize)
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

#endif // aiSupportOpenGL
