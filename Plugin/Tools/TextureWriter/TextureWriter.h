#ifndef TextureWriter_h
#define TextureWriter_h

enum tTextureFormat;
enum tDataFormat;
struct tWriteTextureTask;

enum tDataFormat
{
    tDataFormat_Unknown,
    tDataFormat_Half1,
    tDataFormat_Half2,
    tDataFormat_Half3,
    tDataFormat_Half4,
    tDataFormat_Float1,
    tDataFormat_Float2,
    tDataFormat_Float3,
    tDataFormat_Float4,
    tDataFormat_Int1,
    tDataFormat_Int2,
    tDataFormat_Int3,
    tDataFormat_Int4,
    tDataFormat_LInt1, // 64bit int
};


// dst_tex: texture object. ID3D11Texture* on d3d11, GLuint (texture handle) on OpenGL, etc.
tCLinkage tExport int tWriteTexture(
    void *dst_tex, int dst_width, int dst_height, tTextureFormat dst_fmt,
    const void *src, int src_num, tDataFormat src_fmt);

// async APIs
tCLinkage tExport tWriteTextureTask* tWriteTextureBegin(
    void *dst_tex, int dst_width, int dst_height, tTextureFormat dst_fmt,
    const void *src, int src_num, tDataFormat src_fmt);
tCLinkage tExport int tWriteTextureEnd(tWriteTextureTask *task); // -> bool
tCLinkage tExport int tWriteTextureIsComplete(tWriteTextureTask *task); // -> bool

// for static link usage. initialize graphics device manually.
void tUnitySetGraphicsDevice(void* device, int deviceType, int eventType);

#endif // TextureWriter_h
