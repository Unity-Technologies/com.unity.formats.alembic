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
    #pragma warning(disable: 4190)
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

    struct abcSampleSelector
    {
        uint64_t m_requestedIndex;
        double m_requestedTime;
        int m_requestedTimeIndexType;
    };
#endif // AlembicExporter_h
#endif // aiImpl

enum aiTextureFormat;

struct  aiConfig;
struct  aiCameraData;
struct  aiXFormData;
struct  aiMeshSummary;
struct  aiMeshSampleSummary;
struct  aiPolyMeshData;
struct  aiSubmeshSummary;
struct  aiSubmeshData;
struct  aiFacesets;

class   aiContext;
class   aiObject;
#ifdef aeImpl
    class aiSchemaBase;
    class aiSampleBase;
    class aiXFormSample;    // : aiSampleBase
    class aiCameraSample;   // : aiSampleBase
    class aiPolyMeshSample; // : aiSampleBase
    class aiPointsSample;   // : aiSampleBase
    class aiCurvesSample;   // : aiSampleBase
    class aiSubDSample;     // : aiSampleBase
#else
    // force make castable
    typedef void aiSchemaBase;
    typedef void aiSampleBase;
    typedef void aiXFormSample;
    typedef void aiCameraSample;
    typedef void aiPolyMeshSample;
    typedef void aiPointsSample;
    typedef void aiCurvesSample;
    typedef void aiSubDSample;
#endif

class   aiXForm;    // : aiSchemaBase
class   aiCamera;   // : aiSchemaBase
class   aiPolyMesh; // : aiSchemaBase
class   aiPoints;   // : aiSchemaBase
class   aiCurves;   // : aiSchemaBase
class   aiSubD;     // : aiSchemaBase
class   aiProperty;

struct  aiPolyMeshData;
struct  aiPointsData;
struct  aiCurvesSampleData;
struct  aiSubDSampleData;

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

enum aiTimeSamplingType
{
    aiTimeSamplingType_Uniform,
    aiTimeSamplingType_Cyclic,
    aiTimeSamplingType_Acyclic,
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
        : swapHandedness(false)
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
};

struct aiTimeSamplingData
{
    aiTimeSamplingType type;
    float startTime;
    float endTime;
    float interval;     // relevant only if type is Uniform or Cyclic
    int numTimes;       // relevant only if type is Acyclic
    double *times;      // relevant only if type is Acyclic

    aiTimeSamplingData()
        : type(aiTimeSamplingType_Uniform)
        , startTime(0.0f), endTime(0.0f), interval(1.0f / 30.0f)
        , numTimes(0), times(nullptr)
    {}
};

struct aiXFormData
{
    abcV3 translation;
    abcV4 rotation; // quaternion
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
    int32_t peakTriangulatedIndexCount;
    int32_t peakSubmeshCount;

    inline aiMeshSummary()
        : topologyVariance(0)
        , peakVertexCount(0)
        , peakIndexCount(0)
        , peakTriangulatedIndexCount(0)
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

struct aiPolyMeshData
{
    abcV3 *positions;
    abcV3 *velocities;
    abcV3 *normals;
    abcV2 *uvs;
    abcV4 *tangents;

    int *indices;
    int *normalIndices;
    int *uvIndices;
    int *faces;

    int positionCount;
    int normalCount;
    int uvCount;

    int indexCount;
    int normalIndexCount;
    int uvIndexCount;
    int faceCount;

    int triangulatedIndexCount;

    abcV3 center;
    abcV3 size;

    inline aiPolyMeshData()
        : positions(nullptr), velocities(nullptr), normals(nullptr), uvs(nullptr), tangents(nullptr)
        , indices(nullptr), normalIndices(nullptr), uvIndices(nullptr), faces(nullptr)
        , positionCount(0), normalCount(0), uvCount(0)
        , indexCount(0), normalIndexCount(0), uvIndexCount(0), faceCount(0)
        , triangulatedIndexCount(0)
        , center(0.0f, 0.0f, 0.0f) , size(0.0f, 0.0f, 0.0f)
    {
    }

    aiPolyMeshData(const aiPolyMeshData&) = default;
    aiPolyMeshData& operator=(const aiPolyMeshData&) = default;
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

struct aiPointsData
{
    abcV3       *positions;
    abcV3       *velocities;
    uint64_t    *ids;
    int32_t     count;

    abcV3       center;
    abcV3       size;

    inline aiPointsData()
        : positions(nullptr)
        , velocities(nullptr)
        , ids(nullptr)
        , count(0)
        , center(0.0f, 0.0f, 0.0f)
        , size(0.0f, 0.0f, 0.0f)
    {
    }

    aiPointsData(const aiPointsData&) = default;
    aiPointsData& operator=(const aiPointsData&) = default;
};

struct aiPropertyData
{
    void *data;
    int size;
    aiPropertyType type;

    aiPropertyData() : data(nullptr), size(0), type(aiPropertyType_Unknown) {}
    aiPropertyData(aiPropertyType t, void *d, int s) : data(d), size(s), type(t) {}
};


typedef void (aiSTDCall *aiNodeEnumerator)(aiObject *node, void *userData);
typedef void (aiSTDCall *aiConfigCallback)(void *csObj, aiConfig *config);
typedef void (aiSTDCall *aiSampleCallback)(void *csObj, aiSampleBase *sample, bool topologyChanged);


aiCLinkage aiExport abcSampleSelector aiTimeToSampleSelector(float time);
aiCLinkage aiExport abcSampleSelector aiIndexToSampleSelector(int index);

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

aiCLinkage aiExport int             aiGetNumTimeSamplings(aiContext* ctx);
aiCLinkage aiExport void            aiGetTimeSampling(aiContext* ctx, int i, aiTimeSamplingData *dst);
// deep copy to dst->times. if time sampling type is not Acyclic (which have no times), do exact same thing as aiGetTimeSampling()
aiCLinkage aiExport void            aiCopyTimeSampling(aiContext* ctx, int i, aiTimeSamplingData *dst);

aiCLinkage aiExport void            aiUpdateSamples(aiContext* ctx, float time);
aiCLinkage aiExport void            aiUpdateSamplesBegin(aiContext* ctx, float time);   // async version
aiCLinkage aiExport void            aiUpdateSamplesEnd(aiContext* ctx);                 // async version

aiCLinkage aiExport void            aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *userData);
aiCLinkage aiExport const char*     aiGetNameS(aiObject* obj);
aiCLinkage aiExport const char*     aiGetFullNameS(aiObject* obj);
aiCLinkage aiExport int             aiGetNumChildren(aiObject* obj);
aiCLinkage aiExport aiObject*       aiGetChild(aiObject* obj, int i);
aiCLinkage aiExport aiObject*       aiGetParent(aiObject* obj);

aiCLinkage aiExport void            aiSchemaSetSampleCallback(aiSchemaBase* schema, aiSampleCallback cb, void* arg);
aiCLinkage aiExport void            aiSchemaSetConfigCallback(aiSchemaBase* schema, aiConfigCallback cb, void* arg);
aiCLinkage aiExport int             aiSchemaGetNumSamples(aiSchemaBase* schema);
aiCLinkage aiExport aiSampleBase*   aiSchemaUpdateSample(aiSchemaBase* schema, const abcSampleSelector *ss);
aiCLinkage aiExport aiSampleBase*   aiSchemaGetSample(aiSchemaBase* schema, const abcSampleSelector *ss);
aiCLinkage aiExport int             aiSchemaGetSampleIndex(aiSchemaBase* schema, const abcSampleSelector *ss);
aiCLinkage aiExport float           aiSchemaGetSampleTime(aiSchemaBase* schema, const abcSampleSelector *ss);
aiCLinkage aiExport int             aiSchemaGetTimeSamplingIndex(aiSchemaBase* schema);

aiCLinkage aiExport aiXForm*        aiGetXForm(aiObject* obj);
aiCLinkage aiExport void            aiXFormGetData(aiXFormSample* sample, aiXFormData *outData);

aiCLinkage aiExport aiPolyMesh*     aiGetPolyMesh(aiObject* obj);
aiCLinkage aiExport void            aiPolyMeshGetSummary(aiPolyMesh* schema, aiMeshSummary* summary);
aiCLinkage aiExport void            aiPolyMeshGetSampleSummary(aiPolyMeshSample* sample, aiMeshSampleSummary* summary, bool forceRefresh=false);
// return pointers to actual data. no conversions (swap handedness / faces) are applied.
aiCLinkage aiExport void            aiPolyMeshGetDataPointer(aiPolyMeshSample* sample, aiPolyMeshData* data);
// copy mesh data without splitting. swap handedness / faces are applied.
// if triangulate is true, triangulation is applied. in this case:
// - if position indices and normal / uv indices are deferent, index expanding is applied inevitably.
// - if position indices and normal / uv indices are same and always_expand_indices is false, expanding is not applied.
aiCLinkage aiExport void            aiPolyMeshCopyData(aiPolyMeshSample* sample, aiPolyMeshData* data, bool triangulate = false, bool always_expand_indices = false);
// all these below aiPolyMesh* are mesh splitting functions
aiCLinkage aiExport int             aiPolyMeshGetVertexBufferLength(aiPolyMeshSample* sample, int splitIndex);
aiCLinkage aiExport void            aiPolyMeshFillVertexBuffer(aiPolyMeshSample* sample, int splitIndex, aiPolyMeshData* data);
aiCLinkage aiExport int             aiPolyMeshPrepareSubmeshes(aiPolyMeshSample* sample, const aiFacesets* facesets);
aiCLinkage aiExport int             aiPolyMeshGetSplitSubmeshCount(aiPolyMeshSample* sample, int splitIndex);
aiCLinkage aiExport bool            aiPolyMeshGetNextSubmesh(aiPolyMeshSample* sample, aiSubmeshSummary* summary);
aiCLinkage aiExport void            aiPolyMeshFillSubmeshIndices(aiPolyMeshSample* sample, const aiSubmeshSummary* summary, aiSubmeshData* data);

aiCLinkage aiExport aiCamera*       aiGetCamera(aiObject* obj);
aiCLinkage aiExport void            aiCameraGetData(aiCameraSample* sample, aiCameraData *outData);

aiCLinkage aiExport aiPoints*       aiGetPoints(aiObject* obj);
aiCLinkage aiExport int             aiPointsGetPeakVertexCount(aiPoints *schema);
aiCLinkage aiExport void            aiPointsGetDataPointer(aiPointsSample* sample, aiPointsData *outData);
aiCLinkage aiExport void            aiPointsCopyData(aiPointsSample* sample, aiPointsData *outData);

aiCLinkage aiExport int             aiSchemaGetNumProperties(aiSchemaBase* schema);
aiCLinkage aiExport aiProperty*     aiSchemaGetPropertyByIndex(aiSchemaBase* schema, int i);
aiCLinkage aiExport aiProperty*     aiSchemaGetPropertyByName(aiSchemaBase* schema, const char *name);
aiCLinkage aiExport const char*     aiPropertyGetNameS(aiProperty* prop);
aiCLinkage aiExport aiPropertyType  aiPropertyGetType(aiProperty* prop);
aiCLinkage aiExport int             aiPropertyGetTimeSamplingIndex(aiProperty* prop);
aiCLinkage aiExport void            aiPropertyGetDataPointer(aiProperty* prop, const abcSampleSelector *ss, aiPropertyData *data);
aiCLinkage aiExport void            aiPropertyCopyData(aiProperty* prop, const abcSampleSelector *ss, aiPropertyData *data);

#ifdef aiSupportTexture
aiCLinkage aiExport bool            aiPointsCopyPositionsToTexture(aiPointsData *data, void *tex, int width, int height, aiTextureFormat fmt);
aiCLinkage aiExport bool            aiPointsCopyIDsToTexture(aiPointsData *data, void *tex, int width, int height, aiTextureFormat fmt);
#endif // aiSupportTexture

#endif // AlembicImporter_h
