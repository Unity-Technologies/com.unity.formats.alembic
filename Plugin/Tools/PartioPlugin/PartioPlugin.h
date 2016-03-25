#ifndef PartioPlugin_h
#define PartioPlugin_h

#define ppCLinkage extern "C"
#ifdef ppStaticLink
    #define ppExport 
#elif ppImpl
    #ifdef _MSC_VER
        #define ppExport __declspec(dllexport)
    #else
        #define ppExport
    #endif
#else
    #ifdef _MSC_VER
        #define ppExport __declspec(dllimport)
    #else
        #define ppExport
    #endif
#endif

class ppContext;
class ppCache;

struct ppCacheData
{
    float *positions;
    float *velocities;
    int *ids;
    int num;

    ppCacheData() : positions(), velocities(), ids(), num() {}
};

struct ppConfig
{
    bool swap_handedness;

    ppConfig() : swap_handedness() {}

};

ppCLinkage ppExport ppContext*  ppCreateContext(const ppConfig *conf);
ppCLinkage ppExport void        ppDestroyContext(ppContext *ctx);

ppCLinkage ppExport bool        ppReadFile(ppContext *ctx, const char *path);
ppCLinkage ppExport size_t      ppReadFiles(ppContext *ctx, const char *path);
ppCLinkage ppExport void        ppReadFilesBegin(ppContext *ctx, const char *path); // async variant
ppCLinkage ppExport size_t      ppReadFilesEnd(ppContext *ctx, const char *path);   // 

ppCLinkage ppExport bool        ppWriteFile(ppContext *ctx, const char *path);
ppCLinkage ppExport size_t      ppWriteFiles(ppContext *ctx, const char *path);
ppCLinkage ppExport void        ppWriteFilesBegin(ppContext *ctx, const char *path);// async variant
ppCLinkage ppExport void        ppWriteFilesEnd(ppContext *ctx, const char *path);  // 

ppCLinkage ppExport int         ppGetNumCache(ppContext *ctx);
ppCLinkage ppExport ppCache*    ppGetCache(ppContext *ctx, int nth);
ppCLinkage ppExport void        ppGetData(ppCache *cache, ppCacheData *data);
ppCLinkage ppExport void        ppSetData(ppCache *cache, ppCacheData *data);

#endif // PartioPlugin_h
