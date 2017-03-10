#pragma once

#include <cstdint>

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
#ifdef abciImpl
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
    float fieldOfView;      // in degree. vertical one
    float aspectRatio;

    float focusDistance;    // in cm
    float focalLength;      // in mm
    float aperture;         // in cm. vertical one

    inline aiCameraData()
        : nearClippingPlane(0.3f)
        , farClippingPlane(1000.0f)
        , fieldOfView(60.0f)
        , aspectRatio(16.0f / 9.0f)
        , focusDistance(5.0f)
        , focalLength(0.0f)
        , aperture(2.4f)
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


struct aiPointsSummary
{
    bool hasVelocity;
    bool positionIsConstant;
    bool idIsConstant;
    int32_t peakCount;
    uint64_t minID;
    uint64_t maxID;
    abcV3 boundsCenter;
    abcV3 boundsExtents;

    aiPointsSummary()
        : hasVelocity(false)
        , positionIsConstant(false), idIsConstant(false)
        , peakCount(0)
        , minID(0), maxID(0)
        , boundsCenter(0.0f, 0.0f, 0.0f), boundsExtents(0.0f, 0.0f, 0.0f)
    {}
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


typedef void (abciSTDCall *aiNodeEnumerator)(aiObject *node, void *userData);
typedef void (abciSTDCall *aiConfigCallback)(void *csObj, aiConfig *config);
typedef void (abciSTDCall *aiSampleCallback)(void *csObj, aiSampleBase *sample, bool topologyChanged);


abciAPI abcSampleSelector aiTimeToSampleSelector(float time);
abciAPI abcSampleSelector aiIndexToSampleSelector(int index);

abciAPI void            aiEnableFileLog(bool on, const char *path);

abciAPI void            aiCleanup();
abciAPI aiContext*      aiCreateContext(int uid);
abciAPI void            aiDestroyContext(aiContext* ctx);

abciAPI bool            aiLoad(aiContext* ctx, const char *path);
abciAPI void            aiSetConfig(aiContext* ctx, const aiConfig* conf);
abciAPI float           aiGetStartTime(aiContext* ctx);
abciAPI float           aiGetEndTime(aiContext* ctx);
abciAPI aiObject*       aiGetTopObject(aiContext* ctx);
abciAPI void            aiDestroyObject(aiContext* ctx, aiObject* obj);

abciAPI int             aiGetNumTimeSamplings(aiContext* ctx);
abciAPI void            aiGetTimeSampling(aiContext* ctx, int i, aiTimeSamplingData *dst);
// deep copy to dst->times. if time sampling type is not Acyclic (which have no times), do exact same thing as aiGetTimeSampling()
abciAPI void            aiCopyTimeSampling(aiContext* ctx, int i, aiTimeSamplingData *dst);

abciAPI void            aiUpdateSamples(aiContext* ctx, float time);
abciAPI void            aiUpdateSamplesBegin(aiContext* ctx, float time);   // async version
abciAPI void            aiUpdateSamplesEnd(aiContext* ctx);                 // async version

abciAPI void            aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *userData);
abciAPI const char*     aiGetNameS(aiObject* obj);
abciAPI const char*     aiGetFullNameS(aiObject* obj);
abciAPI int             aiGetNumChildren(aiObject* obj);
abciAPI aiObject*       aiGetChild(aiObject* obj, int i);
abciAPI aiObject*       aiGetParent(aiObject* obj);

abciAPI void            aiSchemaSetSampleCallback(aiSchemaBase* schema, aiSampleCallback cb, void* arg);
abciAPI void            aiSchemaSetConfigCallback(aiSchemaBase* schema, aiConfigCallback cb, void* arg);
abciAPI aiObject*       aiSchemaGetObject(aiSchemaBase* schema);
abciAPI int             aiSchemaGetNumSamples(aiSchemaBase* schema);
abciAPI aiSampleBase*   aiSchemaUpdateSample(aiSchemaBase* schema, const abcSampleSelector *ss);
abciAPI aiSampleBase*   aiSchemaGetSample(aiSchemaBase* schema, const abcSampleSelector *ss);
abciAPI int             aiSchemaGetSampleIndex(aiSchemaBase* schema, const abcSampleSelector *ss);
abciAPI float           aiSchemaGetSampleTime(aiSchemaBase* schema, const abcSampleSelector *ss);
abciAPI int             aiSchemaGetTimeSamplingIndex(aiSchemaBase* schema);

abciAPI aiXForm*        aiGetXForm(aiObject* obj);
abciAPI void            aiXFormGetData(aiXFormSample* sample, aiXFormData *outData);

abciAPI aiPolyMesh*     aiGetPolyMesh(aiObject* obj);
abciAPI void            aiPolyMeshGetSummary(aiPolyMesh* schema, aiMeshSummary* summary);
abciAPI void            aiPolyMeshGetSampleSummary(aiPolyMeshSample* sample, aiMeshSampleSummary* summary, bool forceRefresh=false);
// return pointers to actual data. no conversions (swap handedness / faces) are applied.
abciAPI void            aiPolyMeshGetDataPointer(aiPolyMeshSample* sample, aiPolyMeshData* data);
// copy mesh data without splitting. swap handedness / faces are applied.
// if triangulate is true, triangulation is applied. in this case:
// - if position indices and normal / uv indices are deferent, index expanding is applied inevitably.
// - if position indices and normal / uv indices are same and always_expand_indices is false, expanding is not applied.
abciAPI void            aiPolyMeshCopyData(aiPolyMeshSample* sample, aiPolyMeshData* data, bool triangulate = false, bool always_expand_indices = false);
// all these below aiPolyMesh* are mesh splitting functions
abciAPI int             aiPolyMeshGetVertexBufferLength(aiPolyMeshSample* sample, int splitIndex);
abciAPI void            aiPolyMeshFillVertexBuffer(aiPolyMeshSample* sample, int splitIndex, aiPolyMeshData* data);
abciAPI int             aiPolyMeshPrepareSubmeshes(aiPolyMeshSample* sample, const aiFacesets* facesets);
abciAPI int             aiPolyMeshGetSplitSubmeshCount(aiPolyMeshSample* sample, int splitIndex);
abciAPI bool            aiPolyMeshGetNextSubmesh(aiPolyMeshSample* sample, aiSubmeshSummary* summary);
abciAPI void            aiPolyMeshFillSubmeshIndices(aiPolyMeshSample* sample, const aiSubmeshSummary* summary, aiSubmeshData* data);

abciAPI aiCamera*       aiGetCamera(aiObject* obj);
abciAPI void            aiCameraGetData(aiCameraSample* sample, aiCameraData *outData);

abciAPI aiPoints*       aiGetPoints(aiObject* obj);
abciAPI void            aiPointsGetSummary(aiPoints *schema, aiPointsSummary *summary);
abciAPI void            aiPointsGetDataPointer(aiPointsSample* sample, aiPointsData *outData);
abciAPI void            aiPointsCopyData(aiPointsSample* sample, aiPointsData *outData);

abciAPI int             aiSchemaGetNumProperties(aiSchemaBase* schema);
abciAPI aiProperty*     aiSchemaGetPropertyByIndex(aiSchemaBase* schema, int i);
abciAPI aiProperty*     aiSchemaGetPropertyByName(aiSchemaBase* schema, const char *name);
abciAPI const char*     aiPropertyGetNameS(aiProperty* prop);
abciAPI aiPropertyType  aiPropertyGetType(aiProperty* prop);
abciAPI int             aiPropertyGetTimeSamplingIndex(aiProperty* prop);
abciAPI void            aiPropertyGetDataPointer(aiProperty* prop, const abcSampleSelector *ss, aiPropertyData *data);
abciAPI void            aiPropertyCopyData(aiProperty* prop, const abcSampleSelector *ss, aiPropertyData *data);
