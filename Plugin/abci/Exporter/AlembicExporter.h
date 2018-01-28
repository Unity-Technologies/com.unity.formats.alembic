#pragma once

#include <cstdint>

class aeContext;
#ifdef abciImpl
    class aeObject;
#else
    using aeObject = void; // force make upper-castable
#endif
class aeXform;    // : aeObject
class aePoints;   // : aeObject
class aePolyMesh; // : aeObject
class aeCamera;   // : aeObject
class aeProperty;

struct aeXformData;
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
    aeArchiveType archive_type = aeArchiveType::Ogawa;
    aeTimeSamplingType time_sampling_type = aeTimeSamplingType::Uniform;
    float start_time = 0.0f;    // start time on Alembic.
    float frame_rate = 30.0f;    // frame rate on Alembic. relevant only if timeSamplingType is uniform
    aeXFromType xform_type = aeXFromType::TRS;
    bool swap_handedness = true; // swap rhs <-> lhs
    bool swap_faces = false; // swap triangle indices
    float scale = 1.0f;
};


struct aeXformData
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
    const int *normal_indices = nullptr; // if null, assume same as indices
    const int *uv_indices = nullptr;     // if null, assume same as indices
    const int *faces = nullptr;          // if null, assume all faces are triangles

    int position_count = 0;
    int normal_count = 0;        // if 0, assume same as position_count
    int uv_count = 0;            // if 0, assume same as position_count

    int index_count = 0;
    int normal_index_count = 0;
    int uv_index_count = 0;
    int face_count = 0;
};

struct aeFaceSetData
{
    const int *faces = nullptr;
    int face_count = 0;
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
    float near_clipping_plane = 0.3f;
    float far_clipping_plane = 1000.0f;
    float field_of_view = 60.0f;      // in degree. vertical one. relevant only if focalLength==0.0
    float aspect_ratio = 16.0f / 9.0f;

    float focus_distance = 5.0f;    // in cm
    float focal_length = 0.0f;      // in mm. if 0.0f, automatically computed by aperture and fieldOfView. alembic's default value is 35.0
    float aperture = 2.4f;          // in cm. vertical one
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
abciAPI void        aeMarkFrameBegin(aeContext* ctx);
abciAPI void        aeMarkFrameEnd(aeContext* ctx);

abciAPI void        aeDeleteObject(aeObject *obj);
abciAPI aeXform*    aeNewXform(aeObject *parent, const char *name, int tsi = 1);
abciAPI aePoints*   aeNewPoints(aeObject *parent, const char *name, int tsi = 1);
abciAPI aePolyMesh* aeNewPolyMesh(aeObject *parent, const char *name, int tsi = 1);
abciAPI aeCamera*   aeNewCamera(aeObject *parent, const char *name, int tsi = 1);

abciAPI int         aeGetNumChildren(aeObject *obj);
abciAPI aeObject*   aeGetChild(aeObject *obj, int i);
abciAPI aeObject*   aeGetParent(aeObject *obj);
abciAPI aeXform*    aeAsXform(aeObject *obj);
abciAPI aePoints*   aeAsPoints(aeObject *obj);
abciAPI aePolyMesh* aeAsPolyMesh(aeObject *obj);
abciAPI aeCamera*   aeAsCamera(aeObject *obj);

abciAPI int         aeGetNumSamples(aeObject *obj);
abciAPI void        aeSetFromPrevious(aeObject *obj);

abciAPI void        aeXformWriteSample(aeXform *obj, const aeXformData *data);
abciAPI void        aePointsWriteSample(aePoints *obj, const aePointsData *data);
abciAPI void        aePolyMeshWriteSample(aePolyMesh *obj, const aePolyMeshData *data);
abciAPI int         aePolyMeshAddFaceSet(aePolyMesh *obj, const char *name);
abciAPI void        aePolyMeshWriteFaceSetSample(aePolyMesh *obj, int fsi, const aeFaceSetData *data);
abciAPI void        aeCameraWriteSample(aeCamera *obj, const aeCameraData *data);

abciAPI aeProperty* aeNewProperty(aeObject *parent, const char *name, aePropertyType type);
abciAPI void        aePropertyWriteArraySample(aeProperty *prop, const void *data, int num_data);
abciAPI void        aePropertyWriteScalarSample(aeProperty *prop, const void *data);

abciAPI int         aeGenerateRemapIndices(int *dst, abcV3 *points, aeWeights4 *weights, int vertex_count);
