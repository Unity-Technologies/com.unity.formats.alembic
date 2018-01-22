#pragma once

#include <cstdint>

class aeContext;
#ifdef abciImpl
    class aeObject;
#else
    typedef void aeObject; // force make upper-castable
#endif
class aeXForm;    // : aeObject
class aePoints;   // : aeObject
class aePolyMesh; // : aeObject
class aeCamera;   // : aeObject
class aeProperty;

struct aeXFormData;
struct aePointsData;
struct aePolyMeshData;
struct aeCameraData;


enum class aeArchiveType
{
    HDF5,
    Ogawa,
};

enum class aeTimeSamplingType
{
    Uniform,
    Cyclic,
    Acyclic,
};

enum class aeXFromType
{
    Matrix,
    TRS,
};

enum class aePropertyType
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

    ScalarTypeBegin = Bool,
    ScalarTypeEnd = Float4x4,

    ArrayTypeBegin = BoolArray,
    ArrayTypeEnd = Float4x4Array,
};

struct aeConfig
{
    aeArchiveType archiveType = aeArchiveType::Ogawa;
    aeTimeSamplingType timeSamplingType = aeTimeSamplingType::Uniform;
    float startTime = 0.0f;    // start time on Alembic.
    float frameRate = 30.0f;    // frame rate on Alembic. relevant only if timeSamplingType is uniform
    aeXFromType xformType = aeXFromType::TRS;
    bool swapHandedness = true; // swap rhs <-> lhs
    bool swapFaces = false; // swap triangle indices
    float scale = 1.0f;
};


struct aeXFormData
{
    abcV3 translation = { 0.0f, 0.0f, 0.0f };
    abcV4 rotation = { 0.0f, 0.0f, 0.0f, 1.0f }; // quaternion
    abcV3 scale = { 1.0f, 1.0f, 1.0f };
    bool inherits = true;
};

struct aePolyMeshData
{
    const abcV3 *positions = nullptr;
    const abcV3 *velocities = nullptr;  // can be null. if not null, must be same size of positions
    const abcV3 *normals = nullptr;     // can be null
    const abcV2 *uvs = nullptr;         // can be null

    const int *indices = nullptr;
    const int *normalIndices = nullptr; // if null, assume same as indices
    const int *uvIndices = nullptr;     // if null, assume same as indices
    const int *faces = nullptr;         // if null, assume all faces are triangles

    int positionCount = 0;
    int normalCount = 0;        // if 0, assume same as positionCount
    int uvCount = 0;            // if 0, assume same as positionCount

    int indexCount = 0;
    int normalIndexCount = 0;   // if 0, assume same as indexCount
    int uvIndexCount = 0;       // if 0, assume same as indexCount
    int faceCount = 0;          // only relevant if faces is not null
};

struct aePointsData
{
    const abcV3 *positions = nullptr;
    const abcV3 *velocities = nullptr;  // can be null
    const uint64_t *ids = nullptr;      // can be null
    int count = 0;
};

struct aeCameraData
{
    float nearClippingPlane = 0.3f;
    float farClippingPlane = 1000.0f;
    float fieldOfView = 60.0f;      // in degree. vertical one. relevant only if focalLength==0.0
    float aspectRatio = 16.0f / 9.0f;

    float focusDistance = 5.0f;    // in cm
    float focalLength = 0.0f;      // in mm. if 0.0f, automatically computed by aperture and fieldOfView. alembic's default value is 35.0
    float aperture = 2.4f;         // in cm. vertical one
};

struct aeWeights4
{
    float weight[4];
    int   boneIndex[4];

    bool operator==(const aeWeights4& v) { return memcmp(this, &v, sizeof(*this)) == 0; }
};



abciAPI aeContext*  aeCreateContext();
abciAPI void        aeDestroyContext(aeContext* ctx);

abciAPI void        aeSetConfig(aeContext* ctx, const aeConfig *conf);
abciAPI bool        aeOpenArchive(aeContext* ctx, const char *path);
abciAPI aeObject*   aeGetTopObject(aeContext* ctx);
abciAPI int         aeAddTimeSampling(aeContext* ctx, float start_time);
// relevant only if timeSamplingType is acyclic. if tsi==-1, add time to all time samplings.
abciAPI void        aeAddTime(aeContext* ctx, float time, int tsi = -1);

abciAPI void        aeDeleteObject(aeObject *obj);
abciAPI aeXForm*    aeNewXForm(aeObject *parent, const char *name, int tsi = 1);
abciAPI aePoints*   aeNewPoints(aeObject *parent, const char *name, int tsi = 1);
abciAPI aePolyMesh* aeNewPolyMesh(aeObject *parent, const char *name, int tsi = 1);
abciAPI aeCamera*   aeNewCamera(aeObject *parent, const char *name, int tsi = 1);

abciAPI int         aeGetNumChildren(aeObject *obj);
abciAPI aeObject*   aeGetChild(aeObject *obj, int i);
abciAPI aeObject*   aeGetParent(aeObject *obj);
abciAPI aeXForm*    aeAsXForm(aeObject *obj);
abciAPI aePoints*   aeAsPoints(aeObject *obj);
abciAPI aePolyMesh* aeAsPolyMesh(aeObject *obj);
abciAPI aeCamera*   aeAsCamera(aeObject *obj);

abciAPI int         aeGetNumSamples(aeObject *obj);
abciAPI void        aeSetFromPrevious(aeObject *obj);

abciAPI void        aeXFormWriteSample(aeXForm *obj, const aeXFormData *data);
abciAPI void        aePointsWriteSample(aePoints *obj, const aePointsData *data);
abciAPI void        aePolyMeshWriteSample(aePolyMesh *obj, const aePolyMeshData *data);
abciAPI void        aeCameraWriteSample(aeCamera *obj, const aeCameraData *data);

abciAPI aeProperty* aeNewProperty(aeObject *parent, const char *name, aePropertyType type);
abciAPI void        aePropertyWriteArraySample(aeProperty *prop, const void *data, int num_data);
abciAPI void        aePropertyWriteScalarSample(aeProperty *prop, const void *data);

abciAPI int         aeGenerateRemapIndices(int *dst, abcV3 *points, aeWeights4 *weights, int vertex_count);
