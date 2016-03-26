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
    ppAttributeType_CountMask   = 7,
    ppAttributeType_TypeMask    = 7 << 4,
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
class ppFixedAttribute;

struct ppAttributeData
{
    ppAttributeType type;
    const char *name;
    void *data;

    ppAttributeData() : type(), name(), data() {}
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

ppCLinkage ppExport bool        ppWriteFile(ppContext *ctx, const char *path, int i);
ppCLinkage ppExport size_t      ppWriteFiles(ppContext *ctx, const char *pattern);
ppCLinkage ppExport ppIOAsync*  ppWriteFilesAsync(ppContext *ctx, const char *pattern);// async variant

ppCLinkage ppExport void        ppClearCaches(ppContext *ctx);
ppCLinkage ppExport int         ppGetNumCaches(ppContext *ctx);
ppCLinkage ppExport ppCache*    ppGetCache(ppContext *ctx, int i);
ppCLinkage ppExport ppCache*    ppAddCache(ppContext *ctx, int num_particles);

ppCLinkage ppExport int         ppGetNumParticles(ppCache *cache);
ppCLinkage ppExport int         ppGetNumAttributes(ppCache *cache);
ppCLinkage ppExport int         ppAddAttribute(ppCache *cache, const char *name, ppAttributeType type);
ppCLinkage ppExport void        ppGetAttributeDataByID(ppCache *cache, int i, ppAttributeData *dst);
ppCLinkage ppExport void        ppGetAttributeDataName(ppCache *cache, const char *name, ppAttributeData *dst);

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
