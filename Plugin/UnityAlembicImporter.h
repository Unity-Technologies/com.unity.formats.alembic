
#ifdef _MSC_VER
    #define uaiExport __declspec(dllexport)
#else
    #define uaiExport
#endif

#define uaiCLinkage extern "C"


uaiCLinkage uaiExport int   uaiCreateContext();
uaiCLinkage uaiExport void  uaiDestroyContext(int ctx);

uaiCLinkage uaiExport bool  uaiLoad(const char *path);
uaiCLinkage uaiExport int   uaiGetVertexCount(int ctx);
uaiCLinkage uaiExport void  uaiFillVertex(int ctx, float *dst);
