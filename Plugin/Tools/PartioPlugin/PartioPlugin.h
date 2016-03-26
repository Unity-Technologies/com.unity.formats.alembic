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

#ifndef ppImpl
    struct ppV3
    {
        float x, y, z;

        ppV3() {}
        ppV3(float _x, float _y, float _z) : x(_x), y(_y), z(_z) {}
    };
    typedef void ppIOAsync;
#endif // ppImpl

enum ppAttributeType
{
    ppAttributeType_Unknown     = 0,
    ppAttributeType_FloatType   = 1 << 4,
    ppAttributeType_IntType     = 2 << 4,

    ppAttributeType_Float1  = ppAttributeType_FloatType | 1,
    ppAttributeType_Float2  = ppAttributeType_FloatType | 2,
    ppAttributeType_Float3  = ppAttributeType_FloatType | 3,
    ppAttributeType_Int1    = ppAttributeType_IntType | 1,
    ppAttributeType_Int2    = ppAttributeType_IntType | 2,
    ppAttributeType_Int3    = ppAttributeType_IntType | 3,
};

class ppContext;
class ppCache;
class ppAttribute;

struct ppAttributeData
{
    ppAttributeType type;
    int num;
    const char *name;
    void *data;
};

struct ppConfig
{
    bool swap_handedness;

    ppConfig() : swap_handedness() {}

};

ppCLinkage ppExport ppContext*  ppCreateContext(const ppConfig *conf);
ppCLinkage ppExport void        ppDestroyContext(ppContext *ctx);

ppCLinkage ppExport bool        ppReadFile(ppContext *ctx, const char *path);
ppCLinkage ppExport size_t      ppReadFiles(ppContext *ctx, const char *pattern);
ppCLinkage ppExport ppIOAsync*  ppReadFilesAsync(ppContext *ctx, const char *pattern); // async variant

ppCLinkage ppExport bool        ppWriteFile(ppContext *ctx, const char *path, int nth);
ppCLinkage ppExport size_t      ppWriteFiles(ppContext *ctx, const char *pattern);
ppCLinkage ppExport ppIOAsync*  ppWriteFilesAsync(ppContext *ctx, const char *pattern);// async variant

ppCLinkage ppExport void        ppClearCaches(ppContext *ctx);
ppCLinkage ppExport int         ppGetNumCaches(ppContext *ctx);
ppCLinkage ppExport ppCache*    ppGetCache(ppContext *ctx, int nth);
ppCLinkage ppExport ppCache*    ppNewCache(ppContext *ctx);

ppCLinkage ppExport void        ppClearAttributes(ppCache *ctx);
ppCLinkage ppExport int         ppGetNumAttributes(ppCache *ctx);
ppCLinkage ppExport int         ppNewAttribute(ppCache *cache, const char *name, int length, ppAttributeType type);
ppCLinkage ppExport void        ppGetAttributeData(ppCache *cache, int attr, ppAttributeData *dst);
ppCLinkage ppExport void        ppSetAttributeData(ppCache *cache, int attr, const ppAttributeData *dst);

// just for convenience
struct ppCacheData
{
    ppV3 *positions;
    ppV3 *velocities;
    int *ids;
    int num;

    ppCacheData() : positions(), velocities(), ids(), num() {}
};
ppCLinkage ppExport void        ppGetData(ppCache *cache, ppCacheData *data);
ppCLinkage ppExport void        ppSetData(ppCache *cache, ppCacheData *data);

#endif // PartioPlugin_h
