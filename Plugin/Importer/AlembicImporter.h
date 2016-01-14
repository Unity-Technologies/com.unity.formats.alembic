#ifndef AlembicImporter_h
#define AlembicImporter_h

// options:
// graphics device options are relevant only if aiSupportTexture is defined
// 
// #define aiSupportTexture
//  #define aiSupportD3D11
//  #define aiSupportOpenGL
// 
// #define aiWithTBB

#include <cstdint>

#ifdef _WIN32
    #define aiWindows
#endif // _WIN32

#ifdef _MSC_VER
    #define aiSTDCall __stdcall
#else // _MSC_VER
    #define aiSTDCall __attribute__((stdcall))
#endif // _MSC_VER

#define aiCLinkage extern "C"

#ifdef aiImpl
    #ifdef _MSC_VER
        #define aiExport __declspec(dllexport)
    #else
        #define aiExport __attribute__((visibility("default")))
    #endif
#else
    #ifdef _MSC_VER
        #define aiExport __declspec(dllimport)
        #pragma comment(lib, "AlembicImporter.lib")
    #else
    #endif

#ifndef AlembicExporter_h
    struct abcV2
    {
        float x, y;

        abcV2() {}
        abcV2(float _x, float _y) : x(_x), y(_y) {}
    };

    struct abcV3
    {
        float x, y, z;

        abcV3() {}
        abcV3(float _x, float _y, float _z) : x(_x), y(_y), z(_z) {}
    };

    struct abcV4
    {
        float x, y, z, w;

        abcV4() {}
        abcV4(float _x, float _y, float _z, float _w) : x(_x), y(_y), w(_w) {}
    };
#endif // AlembicExporter_h
#endif // aiImpl

enum aiTextureFormat;

struct  aiConfig;
struct  aiCameraData;
struct  aiXFormData;
struct  aiMeshSummary;
struct  aiMeshSampleSummary;
struct  aiMeshSampleData;
struct  aiSubmeshSummary;
struct  aiSubmeshData;
struct  aiFacesets;

class   aiContext;
class   aiObject;
class   aiSchemaBase;
class   aiSampleBase;
class   aiXForm;
class   aiXFormSample;
class   aiPolyMesh;
class   aiPolyMeshSample;
class   aiPoints;
class   aiPointsSample;
struct  aiPointsSampleData;
class   aiCurves;
class   aiCurvesSample;
struct  aiCurvesSampleData;
class   aiSubD;
class   aiSubDSample;
struct  aiSubDSampleData;
class   aiCamera;
class   aiCameraSample;
class   aiProperty;

enum aiNormalsMode
{
    NM_ReadFromFile = 0,
    NM_ComputeIfMissing,
    NM_AlwaysCompute,
    NM_Ignore
};
    
enum aiTangentsMode
{
    TM_None = 0,
    TM_Smooth,
    TM_Split
};

enum aiPropertyType
{
    aiPropertyType_Unknown,

    // scalar types
    aiPropertyType_Bool,
    aiPropertyType_Int,
    aiPropertyType_UInt,
    aiPropertyType_Float,
    aiPropertyType_Float2,
    aiPropertyType_Float3,
    aiPropertyType_Float4,
    aiPropertyType_Float4x4,

    // array types
    aiPropertyType_BoolArray,
    aiPropertyType_IntArray,
    aiPropertyType_UIntArray,
    aiPropertyType_FloatArray,
    aiPropertyType_Float2Array,
    aiPropertyType_Float3Array,
    aiPropertyType_Float4Array,
    aiPropertyType_Float4x4Array,

    aiPropertyType_ScalarTypeBegin  = aiPropertyType_Bool,
    aiPropertyType_ScalarTypeEnd    = aiPropertyType_Float4x4,

    aiPropertyType_ArrayTypeBegin   = aiPropertyType_BoolArray,
    aiPropertyType_ArrayTypeEnd     = aiPropertyType_Float4x4Array,
};

struct aiConfig
{
    bool swapHandedness;
    bool swapFaceWinding;
    aiNormalsMode normalsMode;
    aiTangentsMode tangentsMode;
    bool cacheTangentsSplits;
    float aspectRatio;
    bool forceUpdate;
    bool useThreads;
    int32_t cacheSamples;
    bool submeshPerUVTile;

    inline aiConfig()
        : swapHandedness(true)
        , swapFaceWinding(false)
        , normalsMode(NM_ComputeIfMissing)
        , tangentsMode(TM_None)
        , cacheTangentsSplits(true)
        , aspectRatio(-1.0f)
        , forceUpdate(false)
        , useThreads(true)
        , cacheSamples(0)
        , submeshPerUVTile(true)
    {
    }

    aiConfig(const aiConfig&) = default;
    aiConfig& operator=(const aiConfig&) = default;

    inline std::string toString() const
    {
        std::ostringstream oss;

        oss << "{swapHandedness: " << (swapHandedness ? "true" : "false");
        oss << ", swapFaceWinding: " << (swapFaceWinding ? "true" : "false");
        oss << ", submeshPerUVTile: " << (submeshPerUVTile ? "true" : "false");
        oss << ", normalsMode: " << (normalsMode == NM_ReadFromFile
                                     ? "read_from_file"
                                     : (normalsMode == NM_ComputeIfMissing
                                        ? "compute_if_missing"
                                        : (normalsMode == NM_AlwaysCompute
                                           ? "always_compute"
                                           : "ignore")));
        oss << ", tangentsMode: " << (tangentsMode == TM_None
                                      ? "none"
                                      : (tangentsMode == TM_Smooth
                                         ? "smooth"
                                         : "split"));
        oss << ", cacheTangentsSplits: " << (cacheTangentsSplits ? "true" : "false");
        oss << ", aspectRatio: " << aspectRatio;
        oss << ", forceUpdate: " << (forceUpdate ? "true" : "false") << "}";
        
        return oss.str();
    }
};

struct aiXFormData
{
    abcV3 translation;
    abcV4 rotation;
    abcV3 scale;
    bool inherits;

    inline aiXFormData()
        : translation(0.0f, 0.0f, 0.0f)
        , rotation(0.0f, 0.0f, 0.0f, 1.0f)
        , scale(1.0f, 1.0f, 1.0f)
        , inherits(false)
    {
    }

    aiXFormData(const aiXFormData&) = default;
    aiXFormData& operator=(const aiXFormData&) = default;
};

struct aiCameraData
{
    float nearClippingPlane;
    float farClippingPlane;
    float fieldOfView;
    float focusDistance;
    float focalLength;

    inline aiCameraData()
        : nearClippingPlane(0.0f)
        , farClippingPlane(0.0f)
        , fieldOfView(0.0f)
        , focusDistance(0.0f)
        , focalLength(0.0f)
    {
    }

    aiCameraData(const aiCameraData&) = default;
    aiCameraData& operator=(const aiCameraData&) = default;
};

struct aiMeshSummary
{
    int32_t topologyVariance;
    int32_t peakVertexCount;
    int32_t peakIndexCount;
    int32_t peakSubmeshCount;

    inline aiMeshSummary()
        : topologyVariance(0)
        , peakVertexCount(0)
        , peakIndexCount(0)
        , peakSubmeshCount(0)
    {
    }

    aiMeshSummary(const aiMeshSummary&) = default;
    aiMeshSummary& operator=(const aiMeshSummary&) = default;
};

struct aiMeshSampleSummary
{
    int32_t splitCount;
    bool hasNormals;
    bool hasUVs;
    bool hasTangents;

    inline aiMeshSampleSummary()
        : splitCount(0)
        , hasNormals(false)
        , hasUVs(false)
        , hasTangents(false)
    {
    }

    aiMeshSampleSummary(const aiMeshSampleSummary&) = default;
    aiMeshSampleSummary& operator=(const aiMeshSampleSummary&) = default;
};

struct aiMeshSampleData
{
    abcV3 *positions;
    abcV3 *normals;
    abcV2 *uvs;
    abcV4 *tangents;
    abcV3 center;
    abcV3 size;

    inline aiMeshSampleData()
        : positions(nullptr)
        , normals(nullptr)
        , uvs(nullptr)
        , tangents(nullptr)
        , center(0.0f, 0.0f, 0.0f)
        , size(0.0f, 0.0f, 0.0f)
    {
    }

    aiMeshSampleData(const aiMeshSampleData&) = default;
    aiMeshSampleData& operator=(const aiMeshSampleData&) = default;
};

struct aiSubmeshSummary
{
    int32_t index;
    int32_t splitIndex;
    int32_t splitSubmeshIndex;
    int32_t facesetIndex;
    int32_t triangleCount;

    inline aiSubmeshSummary()
        : index(0)
        , splitIndex(0)
        , splitSubmeshIndex(0)
        , facesetIndex(-1)
        , triangleCount(0)
    {
    }

    aiSubmeshSummary(const aiSubmeshSummary&) = default;
    aiSubmeshSummary& operator=(const aiSubmeshSummary&) = default;
};

struct aiSubmeshData
{
    int32_t *indices;

    inline aiSubmeshData()
        : indices(0)
    {
    }

    aiSubmeshData(const aiSubmeshData&) = default;
    aiSubmeshData& operator=(const aiSubmeshData&) = default;
};

struct aiFacesets
{
    int32_t count;
    int32_t *faceCounts;
    int32_t *faceIndices;

    inline aiFacesets()
        : count(0)
        , faceCounts(0)
        , faceIndices(0)
    {
    }

    aiFacesets(const aiFacesets&) = default;
    aiFacesets& operator=(const aiFacesets&) = default;
};

struct aiPropertyData
{
    const void *data;
    int size;
    aiPropertyType type;

    aiPropertyData() : data(nullptr), size(0), type(aiPropertyType_Unknown) {}
    aiPropertyData(aiPropertyType t, const void *d, int s) : data(d), size(s), type(t) {}
};


typedef void (aiSTDCall *aiNodeEnumerator)(aiObject *node, void *userData);
typedef void (aiSTDCall *aiConfigCallback)(void *csObj, aiConfig *config);
typedef void (aiSTDCall *aiSampleCallback)(void *csObj, aiSampleBase *sample, bool topologyChanged);


aiCLinkage aiExport void            aiEnableFileLog(bool on, const char *path);

aiCLinkage aiExport void            aiCleanup();
aiCLinkage aiExport aiContext*      aiCreateContext(int uid);
aiCLinkage aiExport void            aiDestroyContext(aiContext* ctx);

aiCLinkage aiExport bool            aiLoad(aiContext* ctx, const char *path);
aiCLinkage aiExport void            aiSetConfig(aiContext* ctx, const aiConfig* conf);
aiCLinkage aiExport float           aiGetStartTime(aiContext* ctx);
aiCLinkage aiExport float           aiGetEndTime(aiContext* ctx);
aiCLinkage aiExport aiObject*       aiGetTopObject(aiContext* ctx);
aiCLinkage aiExport void            aiDestroyObject(aiContext* ctx, aiObject* obj);

aiCLinkage aiExport void            aiUpdateSamples(aiContext* ctx, float time);
aiCLinkage aiExport void            aiUpdateSamplesBegin(aiContext* ctx, float time);
aiCLinkage aiExport void            aiUpdateSamplesEnd(aiContext* ctx);

aiCLinkage aiExport void            aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *userData);
aiCLinkage aiExport const char*     aiGetNameS(aiObject* obj);
aiCLinkage aiExport const char*     aiGetFullNameS(aiObject* obj);

aiCLinkage aiExport void            aiSchemaSetSampleCallback(aiSchemaBase* schema, aiSampleCallback cb, void* arg);
aiCLinkage aiExport void            aiSchemaSetConfigCallback(aiSchemaBase* schema, aiConfigCallback cb, void* arg);
aiCLinkage aiExport const aiSampleBase* aiSchemaUpdateSample(aiSchemaBase* schema, float time);
aiCLinkage aiExport const aiSampleBase* aiSchemaGetSample(aiSchemaBase* schema, float time);

aiCLinkage aiExport bool            aiHasXForm(aiObject* obj);
aiCLinkage aiExport aiXForm*        aiGetXForm(aiObject* obj);
aiCLinkage aiExport void            aiXFormGetData(aiXFormSample* sample, aiXFormData *outData);

aiCLinkage aiExport bool            aiHasPolyMesh(aiObject* obj);
aiCLinkage aiExport aiPolyMesh*     aiGetPolyMesh(aiObject* obj);
aiCLinkage aiExport void            aiPolyMeshGetSummary(aiPolyMesh* schema, aiMeshSummary* summary);
aiCLinkage aiExport void            aiPolyMeshGetSampleSummary(aiPolyMeshSample* sample, aiMeshSampleSummary* summary, bool forceRefresh);
// direct copy (no splitting)
aiCLinkage aiExport void            aiPolyMeshGetData(aiPolyMeshSample* sample, aiMeshSampleData* data);
// copy with splitting
aiCLinkage aiExport int             aiPolyMeshGetVertexBufferLength(aiPolyMeshSample* sample, int splitIndex);
aiCLinkage aiExport void            aiPolyMeshFillVertexBuffer(aiPolyMeshSample* sample, int splitIndex, aiMeshSampleData* data);
aiCLinkage aiExport int             aiPolyMeshPrepareSubmeshes(aiPolyMeshSample* sample, const aiFacesets* facesets);
aiCLinkage aiExport int             aiPolyMeshGetSplitSubmeshCount(aiPolyMeshSample* sample, int splitIndex);
aiCLinkage aiExport bool            aiPolyMeshGetNextSubmesh(aiPolyMeshSample* sample, aiSubmeshSummary* summary);
aiCLinkage aiExport void            aiPolyMeshFillSubmeshIndices(aiPolyMeshSample* sample, const aiSubmeshSummary* summary, aiSubmeshData* data);

aiCLinkage aiExport bool            aiHasCamera(aiObject* obj);
aiCLinkage aiExport aiCamera*       aiGetCamera(aiObject* obj);
aiCLinkage aiExport void            aiCameraGetData(aiCameraSample* sample, aiCameraData *outData);

aiCLinkage aiExport bool            aiHasPoints(aiObject* obj);
aiCLinkage aiExport aiPoints*       aiGetPoints(aiObject* obj);
aiCLinkage aiExport int             aiPointsGetPeakVertexCount(aiPoints *schema);
aiCLinkage aiExport void            aiPointsGetData(aiPointsSample* sample, aiPointsSampleData *outData);

aiCLinkage aiExport int             aiSchemaGetNumProperties(aiSchemaBase* schema);
aiCLinkage aiExport aiProperty*     aiSchemaGetPropertyByIndex(aiSchemaBase* schema, int i);
aiCLinkage aiExport aiProperty*     aiSchemaGetPropertyByName(aiSchemaBase* schema, const char *name);
aiCLinkage aiExport const char*     aiPropertyGetNameS(aiProperty* prop);
aiCLinkage aiExport aiPropertyType  aiPropertyGetType(aiProperty* prop);
aiCLinkage aiExport void            aiPropertyGetData(aiProperty* prop, aiPropertyData *o_data);

#ifdef aiSupportTexture
aiCLinkage aiExport bool            aiPointsCopyPositionsToTexture(aiPointsSampleData *data, void *tex, int width, int height, aiTextureFormat fmt);
aiCLinkage aiExport bool            aiPointsCopyIDsToTexture(aiPointsSampleData *data, void *tex, int width, int height, aiTextureFormat fmt);
#endif // aiSupportTexture

#endif // AlembicImporter_h
