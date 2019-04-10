#pragma once

#include <cstdint>

class aeContext;
#ifdef abciImpl
    class aeObject;
    class aeSchema;
#else
    using aeObject = void; // force make upper-castable
    using aeSchema = void; // 
#endif
class aeXform;    // : aeSchema
class aePoints;   // : aeSchema
class aePolyMesh; // : aeSchema
class aeCamera;   // : aeSchema
class aeProperty;


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

enum class aeTopology
{
    Points,
    Lines,
    Triangles,
    Quads,
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
    float frame_rate = 30.0f;    // frame rate on Alembic. relevant only if time_sampling_type is uniform
    aeXFromType xform_type = aeXFromType::TRS;
    bool swap_handedness = true; // swap rhs <-> lhs
    bool swap_faces = false; // swap triangle indices
    float scale_factor = 1.0f;
};


struct aeXformData
{
    bool visibility = true;

    abcV3 translation = { 0.0f, 0.0f, 0.0f };
    abcV4 rotation = { 0.0f, 0.0f, 0.0f, 1.0f }; // quaternion
    abcV3 scale = { 1.0f, 1.0f, 1.0f };
    bool inherits = true;
};

struct aeSubmeshData
{
    const int   *indices = nullptr;
    int         index_count = 0;
    aeTopology  topology = aeTopology::Triangles;
};

struct aePolyMeshData
{
    bool visibility = true;

    const int   *faces = nullptr;           // can be null. if null, assume all faces are triangles
    const int   *indices = nullptr;
    int         face_count = 0;
    int         index_count = 0;

    const abcV3 *points = nullptr;
    const abcV3 *velocities = nullptr;      // can be null. if not null, must be same size of positions
    int         point_count = 0;

    const abcV3 *normals = nullptr;         // can be null
    const int   *normal_indices = nullptr;  // can be null. if null, normal_count must be same as point_count or index_count
    int         normal_count = 0;           // if 0, assume same as position_count
    int         normal_index_count = 0;

    const abcV2 *uv0 = nullptr;             // can be null
    const int   *uv0_indices = nullptr;     // can be null. if null, uv0_count must be same as point_count or index_count
    int         uv0_count = 0;              // if 0, assume same as position_count
    int         uv0_index_count = 0;

    const abcV2 *uv1 = nullptr;             // can be null
    const int   *uv1_indices = nullptr;     // can be null. if null, uv1_count must be same as point_count or index_count
    int         uv1_count = 0;              // if 0, assume same as position_count
    int         uv1_index_count = 0;

    const abcV4 *colors = nullptr;          // can be null
    const int   *colors_indices = nullptr;  // can be null. if null, colors_count must be same as point_count or index_count
    int         colors_count = 0;           // if 0, assume same as position_count
    int         colors_index_count = 0;

    const aeSubmeshData *submeshes = nullptr;
    int submesh_count = 0;
};

struct aeFaceSetData
{
    const int *faces = nullptr;
    int face_count = 0;
};

struct aePointsData
{
    bool visibility = true;

    const abcV3 *positions = nullptr;
    const abcV3 *velocities = nullptr;  // can be null
    const uint64_t *ids = nullptr;      // can be null
    int count = 0;
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
abciAPI aeCamera*   aeNewCamera(aeObject *obj, const char *name, int tsi = 1);

abciAPI int         aeGetNumChildren(aeObject *obj);
abciAPI aeObject*   aeGetChild(aeObject *obj, int i);
abciAPI aeObject*   aeGetParent(aeObject *obj);
abciAPI aeXform*    aeAsXform(aeObject *obj);
abciAPI aePoints*   aeAsPoints(aeObject *obj);
abciAPI aePolyMesh* aeAsPolyMesh(aeObject *obj);
abciAPI aeCamera*   aeAsCamera(aeObject *obj);

abciAPI int         aeGetNumSamples(aeSchema *obj);
abciAPI void        aeSetFromPrevious(aeSchema *obj);
abciAPI void        aeMarkForceInvisible(aeSchema *obj);

abciAPI void        aeXformWriteSample(aeXform *obj, const aeXformData *data);
abciAPI void        aeCameraWriteSample(aeCamera *obj, const CameraData *data);
abciAPI void        aePointsWriteSample(aePoints *obj, const aePointsData *data);

abciAPI int         aePolyMeshAddFaceSet(aePolyMesh *obj, const char *name);
abciAPI void        aePolyMeshWriteSample(aePolyMesh *obj, const aePolyMeshData *data);
abciAPI void        aePolyMeshWriteFaceSetSample(aePolyMesh *obj, int fsi, const aeFaceSetData *data);

abciAPI aeProperty* aeNewProperty(aeSchema *parent, const char *name, aePropertyType type);
abciAPI void        aePropertyWriteArraySample(aeProperty *prop, const void *data, int num_data);
abciAPI void        aePropertyWriteScalarSample(aeProperty *prop, const void *data);

abciAPI int         aeGenerateRemapIndices(int *dst, abcV3 *points, aeWeights4 *weights, int vertex_count);
