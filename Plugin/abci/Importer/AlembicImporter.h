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

enum class aiNormalsMode
{
    ReadFromFile,
    ComputeIfMissing,
    AlwaysCompute,
    Ignore
};

enum class aiTangentsMode
{
    None,
    Smooth,
    Split
};

enum class aiTimeSamplingType
{
    Uniform,
    Cyclic,
    Acyclic,
};

enum class aiPropertyType
{
    Unknown,

    // scalar types
    Bool,
    Int,
    UInt,
    Float,
    Float2,
    Float3,
    Float4,
    Float4x4,

    // array types
    BoolArray,
    IntArray,
    UIntArray,
    FloatArray,
    Float2Array,
    Float3Array,
    Float4Array,
    Float4x4Array,

    ScalarTypeBegin  = Bool,
    ScalarTypeEnd    = Float4x4,

    ArrayTypeBegin   = BoolArray,
    ArrayTypeEnd     = Float4x4Array,
};

struct aiConfig
{
    bool swapHandedness = true;
    bool swapFaceWinding = false;
    aiNormalsMode normalsMode = aiNormalsMode::ComputeIfMissing;
    aiTangentsMode tangentsMode = aiTangentsMode::None;
    bool cacheTangentsSplits = true;
    float aspectRatio = -1.0f;
    bool forceUpdate = false;
    bool cacheSamples = false;
    bool shareVertices = false;
    bool treatVertexExtraDataAsStatic = false;
    bool interpolateSamples = true;
    bool turnQuadEdges = false;
    float vertexMotionScale = 1.0f;
};

struct aiTimeSamplingData
{
    aiTimeSamplingType type = aiTimeSamplingType::Uniform;
    float startTime = 0.0f;
    float endTime = 0.0f;
    float interval = 1.0f / 30.0f;  // relevant only if type is Uniform or Cyclic
    int numTimes = 0;               // relevant only if type is Acyclic
    double *times = nullptr;        // relevant only if type is Acyclic
};

struct aiXFormData
{
    abcV3 translation = { 0.0f, 0.0f, 0.0f };
    abcV4 rotation = { 0.0f, 0.0f, 0.0f, 1.0f }; // quaternion
    abcV3 scale = { 1.0f, 1.0f, 1.0f };
    bool inherits = false;
};

struct aiCameraData
{
    float nearClippingPlane = 0.3f;
    float farClippingPlane = 1000.0f;
    float fieldOfView = 60.0f;      // in degree. vertical one
    float aspectRatio = 16.0f / 9.0f;

    float focusDistance = 5.0f;     // in cm
    float focalLength = 0.0f;       // in mm
    float aperture = 2.4f;          // in cm. vertical one
};

struct aiMeshSummary
{
    int32_t topologyVariance = 0;
    int32_t peakVertexCount = 0;
    int32_t peakIndexCount = 0;
    int32_t peakTriangulatedIndexCount = 0;
    int32_t peakSubmeshCount = 0;
};

struct aiMeshSampleSummary
{
    int32_t splitCount = 0;
    bool hasNormals = false;
    bool hasUVs = false;
    bool hasTangents = false;
    bool hasVelocities = false;
};

struct aiPolyMeshData
{
    abcV3 *positions = nullptr;
    abcV3 *velocities = nullptr;
    abcV2 *interpolatedVelocitiesXY = nullptr;
    abcV2 *interpolatedVelocitiesZ = nullptr;
    abcV3 *normals = nullptr;
    abcV2 *uvs = nullptr;
    abcV4 *tangents = nullptr;

    int *indices = nullptr;
    int *normalIndices = nullptr;
    int *uvIndices = nullptr;
    int *faces = nullptr;

    int positionCount = 0;
    int normalCount = 0;
    int uvCount = 0;

    int indexCount = 0;
    int normalIndexCount = 0;
    int uvIndexCount = 0;
    int faceCount = 0;

    int triangulatedIndexCount = 0;

    abcV3 center = { 0.0f, 0.0f, 0.0f };
    abcV3 size = { 0.0f, 0.0f, 0.0f };
};

struct aiSubmeshSummary
{
    int32_t index = 0;
    int32_t splitIndex = 0;
    int32_t splitSubmeshIndex = 0;
    int32_t facesetIndex = -1;
    int32_t triangleCount = 0;
};

struct aiSubmeshData
{
    int32_t *indices = nullptr;

};

struct aiFacesets
{
    int32_t count = 0;
    int32_t *faceCounts = nullptr;
    int32_t *faceIndices = nullptr;
};


struct aiPointsSummary
{
    bool hasVelocity = false;
    bool positionIsConstant = false;
    bool idIsConstant = false;
    int32_t peakCount = 0;
    uint64_t minID = 0;
    uint64_t maxID = 0;
    abcV3 boundsCenter = { 0.0f, 0.0f, 0.0f };
    abcV3 boundsExtents = { 0.0f, 0.0f, 0.0f };
};

struct aiPointsData
{
    abcV3       *positions = nullptr;
    abcV3       *velocities = nullptr;
    uint64_t    *ids = nullptr;
    int32_t     count = 0;

    abcV3       center = { 0.0f, 0.0f, 0.0f };
    abcV3       size = { 0.0f, 0.0f, 0.0f };
};

struct aiPropertyData
{
    void *data = nullptr;
    int size = 0;
    aiPropertyType type = aiPropertyType::Unknown;

    aiPropertyData() {}
    aiPropertyData(void *d, int s, aiPropertyType t) : data(d), size(s), type(t) {}
};


using aiNodeEnumerator = void (abciSTDCall*)(aiObject *node, void *userData);
using aiConfigCallback =  void (abciSTDCall*)(void *csObj, aiConfig *config);
using aiSampleCallback = void (abciSTDCall*)(void *csObj, aiSampleBase *sample, bool topologyChanged);

abciAPI abcSampleSelector aiTimeToSampleSelector(float time);
abciAPI abcSampleSelector aiIndexToSampleSelector(int64_t index);

abciAPI void            aiEnableFileLog(bool on, const char *path);

abciAPI void            aiCleanup();
abciAPI void            clearContextsWithPath(const char *path);
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
abciAPI void            aiPolyMeshCopyData(aiPolyMeshSample* sample, aiPolyMeshData* data, int triangulate = false, int always_expand_indices = false);
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
abciAPI void            aiPointsSetSort(aiPoints* schema, bool v);
abciAPI void            aiPointsSetSortBasePosition(aiPoints* schema, abcV3 v);
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
